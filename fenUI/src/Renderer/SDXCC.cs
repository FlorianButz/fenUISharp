using FenUISharp.Logging;
using FenUISharp.Mathematics;
using FenUISharp.Objects;
using SharpGen.Runtime;
using SkiaSharp;
using Vortice.Direct3D11;
using Vortice.Direct3D12;
using Vortice.Direct3D12.Debug;
using Vortice.DirectComposition;
using Vortice.DXGI;

namespace FenUISharp
{
    public class SkiaDirectCompositionContext : IDisposable
    {
        private WeakReference<FWindow> window { get; set; }
        public FWindow Window { get => window.TryGetTarget(out var target) ? target : throw new Exception("Window not set."); }

        public Action<SKCanvas>? DrawAction { get; private set; }

        public DirectCompositionContext? DirectCompositionContext { get; protected set; }

        private ID3D12Resource? backBuffer;
        private GRD3DTextureResourceInfo? resourceInfo;
        private GRBackendTexture? backendTexture;
        private ID3D12Resource? persistentRenderTarget;
        private IDXGIAdapter1? adapter;
        private GRD3DBackendContext? backendContext;
        private uint currentBackBufferIndex = uint.MaxValue;

        public SKSurface? Surface { get; protected set; }
        public GRContext? grContext { get; protected set; }

        public static SKSamplingOptions SamplingOptions { get => new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear); }

        public Action? OnRebuildAdditionals { get; set; }
        public Action? OnDisposeAdditionals { get; set; }
        public Action<SKSurface>? OnFrameDone { get; set; }

        protected int width, height;
        private bool resourcesNeedRecreation = true;
        private readonly object resourceLock = new object();

        // Device recovery tracking
        private bool deviceLost = false;
        private int consecutiveDrawErrors = 0;
        private const int MAX_CONSECUTIVE_ERRORS = 3;

        // This is used to resize buffer while the user is still resizing
        private int resizeOverflowPadding = 350;

        // In order to trigger OnStartResize
        private bool _wasResizing;

        internal enum ResizingMethod { Stretch, PerFrame, SmartSmoothing }
        internal ResizingMethod Method { get; set; } = ResizingMethod.SmartSmoothing;

        public SkiaDirectCompositionContext(FWindow window, Action<SKCanvas> drawAction)
        {
            this.window = new(window);
            this.DrawAction = drawAction;

            if (FenUI.debugEnabled)
            {
                if (D3D12.D3D12GetDebugInterface<ID3D12Debug>(out var debug).Success)
                    debug?.EnableDebugLayer();
            }

            FLogger.Log<SkiaDirectCompositionContext>("Creating SkiaDirectCompositionContext...");

            width = (int)window.Shape.ClientSize.x;
            height = (int)window.Shape.ClientSize.y;

            InitializeDeviceAndContext();

            Window.Callbacks.OnWindowResize += OnResize;
            Window.Callbacks.OnWindowEndResize += OnResizeEnd;

            FLogger.Log<SkiaDirectCompositionContext>("Done creating SkiaDirectCompositionContext!");
        }

        private void InitializeDeviceAndContext()
        {
            try
            {
                // Creating DirectCompositionContext and getting adapter
                FLogger.Log<SkiaDirectCompositionContext>("Creating DirectCompositionContext for SDXCC...");
                DirectCompositionContext = new DirectCompositionContext(
                    Window.hWnd,
                    width,
                    height,
                    Vortice.DXGI.Format.B8G8R8A8_UNorm
                );

                FLogger.Log<SkiaDirectCompositionContext>("Getting adapter...");
                adapter = DirectCompositionContext.GetHardwareAdapter();

                // Creating backend context for Skia D3D
                FLogger.Log<SkiaDirectCompositionContext>("Creating D3D Backend Context...");
                backendContext = new GRD3DBackendContext()
                {
                    Device = DirectCompositionContext.Device.NativePointer,
                    Adapter = adapter.NativePointer,
                    ProtectedContext = false,
                    Queue = DirectCompositionContext.CommandQueue?.NativePointer
                        ?? throw new NullReferenceException("Native Pointer of Command Queue is null.")
                };

                // Creating Skia D3D GRContext
                FLogger.Log<SkiaDirectCompositionContext>("Creating Skia D3D GRContext...");
                grContext = GRContext.CreateDirect3D(backendContext);
                if (grContext == null)
                    throw new Exception("Failed to create Skia D3D GRContext");

                // Initial resource creation
                EnsureResourcesCreated();

                // Reset error tracking
                deviceLost = false;
                consecutiveDrawErrors = 0;
            }
            catch (Exception ex)
            {
                deviceLost = true;
                FLogger.Error($"Failed to initialize device and context: {ex.Message}");
                throw new InvalidOperationException(DirectCompositionContext?.Device.DeviceRemovedReason.Description);
            }
        }

        private bool IsDeviceValid()
        {
            if (DirectCompositionContext?.Device == null)
                return false;

            var reason = DirectCompositionContext.Device.DeviceRemovedReason;
            return reason == 0; // S_OK means device is valid
        }

        private void RecoverFromDeviceRemoval()
        {
            if (deviceLost)
                return; // Already in recovery

            deviceLost = true;
            FLogger.Warn("Device removed, attempting recovery...");

            lock (resourceLock)
            {
                try
                {
                    // First, dispose all GPU resources in the correct order
                    DisposeSkiaResources();

                    // Dispose GRContext while we still have valid D3D12 objects
                    grContext?.Dispose();
                    grContext = null;

                    // Now dispose D3D objects
                    adapter?.Dispose();
                    adapter = null;

                    DirectCompositionContext?.Dispose();
                    DirectCompositionContext = null;

                    // Clear any cached state
                    consecutiveDrawErrors = 0;
                    resourcesNeedRecreation = true;
                    currentBackBufferIndex = uint.MaxValue;

                    // Wait for system to stabilize
                    System.Threading.Thread.Sleep(200);

                    // Force garbage collection to clean up any lingering references
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();

                    // Recreate everything from scratch
                    InitializeDeviceAndContext();

                    deviceLost = false;
                    FLogger.Log<SkiaDirectCompositionContext>("Device recovery successful");
                }
                catch (Exception ex)
                {
                    FLogger.Error($"Device recovery failed: {ex.Message}");

                    // If recovery fails completely, mark as permanently failed
                    deviceLost = true;

                    // Try to clean up what we can
                    try
                    {
                        grContext?.Dispose();
                        grContext = null;
                        DirectCompositionContext?.Dispose();
                        DirectCompositionContext = null;
                        adapter?.Dispose();
                        adapter = null;
                    }
                    catch { /* Ignore cleanup errors during failed recovery */ }

                    throw new InvalidOperationException($"Device recovery failed: {ex.Message}", ex);
                }
            }
        }

        private void EnsureResourcesCreated()
        {
            lock (resourceLock)
            {
                if (!resourcesNeedRecreation && Surface != null && IsDeviceValid())
                    return; // Resources are still valid

                resourcesNeedRecreation = false;
                CreateTexturesAndSurface();
            }
        }

        private void CreateTexturesAndSurface()
        {
            FLogger.Log<SkiaDirectCompositionContext>("Recreating skia surface...");

            try
            {
                // Check device validity first
                if (!IsDeviceValid())
                    // Skip this frame
                    return;

                // Wait for GPU to finish with current resources
                DirectCompositionContext?.WaitForGpu();

                // Dispose old resources
                DisposeSkiaResources();

                // Create persistent render target with proper heap properties
                var resourceDescription = ResourceDescription.Texture2D(
                    Format.B8G8R8A8_UNorm,
                    (uint)width,
                    (uint)height,
                    1,
                    1,
                    1,
                    0,
                    ResourceFlags.AllowRenderTarget,
                    Vortice.Direct3D12.TextureLayout.Unknown
                );

                // Use a clear value for better performance
                var clearValue = new ClearValue
                {
                    Format = Format.B8G8R8A8_UNorm,
                    Color = new Vortice.Mathematics.Color4(0, 0, 0, 0)
                };

                persistentRenderTarget = DirectCompositionContext?.Device.CreateCommittedResource(
                    new HeapProperties(HeapType.Default),
                    HeapFlags.None,
                    resourceDescription,
                    ResourceStates.RenderTarget,
                    clearValue
                );

                if (persistentRenderTarget == null)
                    throw new Exception("Failed to create persistent render target");

                // Creating resource info for the backend texture
                resourceInfo = new GRD3DTextureResourceInfo
                {
                    Resource = persistentRenderTarget.NativePointer,
                    ResourceState = (int)ResourceStates.RenderTarget,
                    Format = (uint)Format.B8G8R8A8_UNorm,
                    SampleCount = 1,
                    LevelCount = 1,
                    SampleQualityPattern = 0,
                    Protected = false
                };

                // Creating the backend texture for SkiaSharp to draw on
                backendTexture = new GRBackendTexture(
                    width,
                    height,
                    resourceInfo
                );

                Surface = SKSurface.Create(grContext, backendTexture, GRSurfaceOrigin.TopLeft, SKColorType.Bgra8888);

                if (Surface == null)
                    throw new Exception("Failed to create Skia surface");

                // Clear the surface initially
                Surface.Canvas.Clear(SKColors.Transparent);
                Surface.Canvas.Flush();

                // Make sure to redraw the content on the new surface
                Window.Redraw();
            }
            catch (Exception ex)
            {
                FLogger.Error($"Failed to create textures and surface: {ex.Message}");
                DisposeSkiaResources();
                resourcesNeedRecreation = true;
                throw;
            }
        }

        private void SwapFromPersistent((ID3D12GraphicsCommandList commandList, ID3D12Resource backBuffer) context)
        {
            try
            {
                if (persistentRenderTarget == null)
                    throw new InvalidOperationException("Persistent render target is null");

                // Make sure Skia has finished all operations on the persistent render target
                Surface?.Canvas?.Flush();
                Surface?.Flush();
                grContext?.Submit(true); // Force completion

                // The backBuffer comes in as RenderTarget state from Present()
                // We need to transition it to CopyDest
                // The persistentRenderTarget is currently in RenderTarget state, transition to CopySource

                var barriers = new ResourceBarrier[]
                {
                    ResourceBarrier.BarrierTransition(
                        persistentRenderTarget,
                        ResourceStates.RenderTarget,
                        ResourceStates.CopySource),
                    ResourceBarrier.BarrierTransition(
                        context.backBuffer,
                        ResourceStates.RenderTarget, // This is the state it comes in as
                        ResourceStates.CopyDest)
                };

                context.commandList.ResourceBarrier(barriers);

                // Copy the persistent render target to the back buffer
                context.commandList.CopyResource(context.backBuffer, persistentRenderTarget);

                // Transition back to proper states for next frame
                // BackBuffer will be transitioned to Present by the Present() caller
                // PersistentRenderTarget should go back to RenderTarget for Skia
                var backBarriers = new ResourceBarrier[]
                {
            ResourceBarrier.BarrierTransition(
                persistentRenderTarget,
                ResourceStates.CopySource,
                ResourceStates.RenderTarget)
                    // Don't transition backBuffer here - Present() will handle it
                };

                context.commandList.ResourceBarrier(backBarriers);
            }
            catch (Exception ex)
            {
                FLogger.Error($"Error in SwapFromPersistent: {ex.Message}");
                throw;
            }
        }

        internal void Draw()
        {
            // Don't draw if we're in an invalid state
            if (deviceLost || _isDisposed)
                return;

            try
            {
                if (Window.Procedure._isSizeMoving && Method == ResizingMethod.Stretch)
                    return;

                lock (resourceLock)
                {
                    // Check device validity first
                    if (!IsDeviceValid())
                    {
                        RecoverFromDeviceRemoval();
                        return;
                    }

                    // Ensure resources are created
                    EnsureResourcesCreated();

                    if (Surface?.Canvas == null)
                    {
                        FLogger.Warn("Surface or Canvas is null, skipping draw");
                        return;
                    }

                    // Wait for any previous frame to complete before starting new work
                    DirectCompositionContext?.WaitForGpu();

                    // Reset consecutive error count on successful preparation
                    consecutiveDrawErrors = 0;

                    // Clear and prepare canvas
                    Surface.Canvas.Save();

                    try
                    {
                        // Invoke the draw action
                        DrawAction?.Invoke(Surface.Canvas);

                        OnFrameDone?.Invoke(Surface);
                    }
                    finally
                    {
                        Surface.Canvas.Restore();
                    }

                    // Flush drawing commands efficiently
                    Surface.Canvas.Flush();
                    Surface.Flush();

                    // Submit to GRContext and ensure completion before copy
                    grContext?.Submit(true);

                    // Present with proper error handling
                    DirectCompositionContext?.Present(SwapFromPersistent, PresentFlags.None);
                }
            }
            catch (SharpGenException ex) when (ex.ResultCode == Vortice.DXGI.ResultCode.DeviceRemoved ||
                                               ex.ResultCode == Vortice.DXGI.ResultCode.DeviceReset ||
                                               ex.ResultCode == Vortice.DXGI.ResultCode.DeviceHung)
            {
                FLogger.Warn($"Device lost during draw: {ex.Message}");
                RecoverFromDeviceRemoval();
            }
            catch (Exception ex)
            {
                consecutiveDrawErrors++;
                FLogger.Error($"Error during draw (attempt {consecutiveDrawErrors}): {ex.Message}");

                // If we get too many consecutive errors, try device recovery
                if (consecutiveDrawErrors >= MAX_CONSECUTIVE_ERRORS)
                {
                    FLogger.Warn($"Too many consecutive draw errors ({consecutiveDrawErrors}), attempting device recovery");
                    RecoverFromDeviceRemoval();
                }
                else
                {
                    // Mark resources for recreation on next frame
                    resourcesNeedRecreation = true;
                }
            }
        }

        private Vector2 currentBufferSizeIncludingPad;

        internal void OnResize(Vector2 size)
        {
            if (!_wasResizing) OnResizeStart(size);
            _wasResizing = true;

            if (Method == ResizingMethod.PerFrame)
            {
                PerformBufferResize(size);
            }
            else if (Method == ResizingMethod.Stretch)
            {
                ResizeWithStretch(size);
                return;
            }
            else if (Method == ResizingMethod.SmartSmoothing)
            {
                // Checking if the current size is bigger than the overflow size
                // This is done in order to avoid resizing the buffers (which is damn expensive)
                // every frame and archive an insanely smooth window resize
                if (
                    size.x >= (currentBufferSizeIncludingPad.x - 25 /* Trigger resize a bit early to avoid visible lag */) ||
                    size.y >= (currentBufferSizeIncludingPad.y - 25 /* Trigger resize a bit early to avoid visible lag */)
                    )
                {
                    // Resize using the bigger buffer
                    currentBufferSizeIncludingPad = size + new Vector2(resizeOverflowPadding, resizeOverflowPadding);
                    PerformBufferResize(currentBufferSizeIncludingPad);

                    FLogger.Log<SkiaDirectCompositionContext>("Resize including padding");
                }
            }
        }

        internal void OnResizeStart(Vector2 size)
        {
            if (Method == ResizingMethod.SmartSmoothing)
                currentBufferSizeIncludingPad = size;
        }

        internal void OnResizeEnd(Vector2 size)
        {
            _wasResizing = false;

            FLogger.Log<SkiaDirectCompositionContext>("Resize using correct dimensions");

            // Pick correct buffer size
            PerformBufferResize(size);

            if (Method == ResizingMethod.Stretch)
            {
                DirectCompositionContext?.RootVisual?.SetTransform(null);
                DirectCompositionContext?.DCompDevice?.Commit();
            }
        }

        private void ResizeWithStretch(Vector2 size)
        {
            if (DirectCompositionContext == null || DirectCompositionContext.DCompDevice == null || !Window.Procedure._isSizeMoving) return;

            int newWidth = (int)size.x;
            int newHeight = (int)size.y;

            // Create a container visual
            var containerVisual = DirectCompositionContext.RootVisual;

            // Scale the content
            float scaleX = (float)newWidth / (float)DirectCompositionContext.SizeX;
            float scaleY = (float)newHeight / (float)DirectCompositionContext.SizeY;

            // Create transform
            var transform = DirectCompositionContext?.DCompDevice?.CreateScaleTransform();
            transform?.SetScaleX(scaleX);
            transform?.SetScaleY(scaleY);
            containerVisual?.SetTransform(transform);

            // Replace root visual temporarily
            DirectCompositionContext?.RootVisual?.SetContent(containerVisual);
            DirectCompositionContext?.DCompDevice?.Commit();
        }

        internal void PerformBufferResize(Vector2 size)
        {
            lock (resourceLock)
            {
                size *= Window.Shape.WindowDPIScale;

                if (width == size.x && height == size.y) return;
                FLogger.Log<SkiaDirectCompositionContext>($"Resizing SDXCC: {size}");

                // Setting new sizes
                width = (int)size.x;
                height = (int)size.y;

                // Wait for GPU before resizing
                DirectCompositionContext?.WaitForGpu();

                // Updating direct composition context
                FLogger.Log<SkiaDirectCompositionContext>($"Resizing SDXCC: {width}, {height}");
                DirectCompositionContext?.Resize(width, height, DisposeRenderTargets);

                // Recreate skia resources
                resourcesNeedRecreation = true;
                EnsureResourcesCreated();

                OnDisposeAdditionals?.Invoke();
                OnRebuildAdditionals?.Invoke();

                // Reset transformation and effects
                DirectCompositionContext?.RootVisual?.SetTransform(null);
                DirectCompositionContext?.RootVisual?.SetEffect(null);

                // Committing to the dcomp device
                DirectCompositionContext?.RootVisual?.SetContent(DirectCompositionContext.SwapChain);
                DirectCompositionContext?.DCompDevice?.Commit();

                FLogger.Log<SkiaDirectCompositionContext>($"Resize completed");
            }
        }

        public SKImage? CaptureWindowRegion(SKRect region, float quality)
        {
            lock (resourceLock)
            {
                if (Surface == null || !IsDeviceValid())
                    return null;

                try
                {
                    // Ensure all drawing is complete before capture
                    Surface.Canvas.Flush();
                    Surface.Flush();
                    grContext?.Submit(true);

                    Compositor.Dump(Surface.Snapshot(), "rcontext_buffer_surf_whole");

                    var snapshot = Surface.Snapshot(new SKRectI((int)region.Left, (int)region.Top, (int)region.Right, (int)region.Bottom));
                    var scaled = RMath.CreateLowResImage(snapshot, RMath.Clamp(quality, 0.01f, 1f), SamplingOptions);
                    snapshot?.Dispose();

                    Compositor.Dump(scaled, "rcontext_buffer_cropped_scaled");

                    return scaled;
                }
                catch (Exception ex)
                {
                    FLogger.Error($"Error capturing window region: {ex.Message}");
                    return null;
                }
            }
        }

        public FAdditionalSurface CreateAdditional(SKImageInfo info)
        {
            if (_isDisposed)
            {
                FLogger.Error("CreateAdditional was called but devices were already disposed.");
                return new(SKSurface.Create(info), null, null, null, info, null);
            }

            if (grContext == null || !IsDeviceValid())
                throw new InvalidOperationException("GRContext is not initialized or device is invalid.");

            // Check if device is in a valid state
            var device = DirectCompositionContext?.Device;
            if (device == null)
                throw new InvalidOperationException("D3D12 Device is null");

            // Create a D3D12 texture resource for offscreen rendering
            var texDesc = new ResourceDescription
            {
                Dimension = Vortice.Direct3D12.ResourceDimension.Texture2D,
                Alignment = 0,
                Width = (ulong)info.Width,
                Height = (uint)info.Height,
                DepthOrArraySize = 1,
                MipLevels = 1,
                Format = Format.B8G8R8A8_UNorm,
                SampleDescription = new SampleDescription(1, 0),
                Layout = Vortice.Direct3D12.TextureLayout.Unknown,
                Flags = ResourceFlags.AllowRenderTarget
            };

            var clearValue = new ClearValue
            {
                Format = Format.B8G8R8A8_UNorm,
                Color = new Vortice.Mathematics.Color4(0, 0, 0, 0)
            };

            // Create the D3D12 texture in the DEFAULT heap
            var texture = DirectCompositionContext?.Device.CreateCommittedResource(
                new HeapProperties(HeapType.Default),
                HeapFlags.None,
                texDesc,
                ResourceStates.RenderTarget,
                clearValue
            );

            // Wrap it for Skia
            var resourceInfo = new GRD3DTextureResourceInfo
            {
                Resource = texture?.NativePointer ?? throw new NullReferenceException("Texture is null"),
                ResourceState = (int)ResourceStates.RenderTarget, // Changed to RenderTarget
                Format = (int)Format.B8G8R8A8_UNorm,
                SampleCount = 1,
                LevelCount = 1,
                SampleQualityPattern = 0,
                Protected = false
            };

            var backendTex = new GRBackendTexture(info.Width, info.Height, resourceInfo);

            // Create an SKSurface
            var surface = SKSurface.Create(grContext, backendTex, GRSurfaceOrigin.TopLeft, info.ColorType);

            return new(surface, texture, resourceInfo, backendTex, info, this);
        }

        private bool _isDisposed;

        public void Dispose()
        {
            if (_isDisposed) return;

            FLogger.Log<SkiaDirectCompositionContext>("Disposing SkiaDirectCompositionContext...");

            _isDisposed = true;

            lock (resourceLock)
            {
                // Wait for GPU before disposing
                DirectCompositionContext?.WaitForGpu();

                // Disposing all resources
                DisposeSkiaResources();
                grContext?.Dispose();
                grContext = null!;

                // Setting draw action to null
                DrawAction = null!;

                // Disposing direct composition context
                DirectCompositionContext?.Dispose();
                DirectCompositionContext = null!;

                adapter?.Dispose();
            }
        }

        private void DisposeRenderTargets()
        {
            lock (resourceLock)
            {
                // Make sure Skia isn't holding any GPU resources
                Surface?.Canvas?.Flush();
                Surface?.Flush();
                grContext?.Flush();
                grContext?.Submit(true); // Force submission and wait

                DisposeSkiaResources();
                resourcesNeedRecreation = true;

                DirectCompositionContext?.WaitForGpu();
            }
        }

        private void DisposeSkiaResources()
        {
            // Dispose in reverse order of creation
            Surface?.Dispose();
            Surface = null;

            backendTexture?.Dispose();
            backendTexture = null;

            resourceInfo?.Dispose();
            resourceInfo = null;

            backBuffer?.Dispose();
            backBuffer = null;

            persistentRenderTarget?.Dispose();
            persistentRenderTarget = null;

            backendContext?.Dispose();
        }
    }
}