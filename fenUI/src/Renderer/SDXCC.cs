using FenUISharp.Logging;
using FenUISharp.Mathematics;
using FenUISharp.Objects;
using SharpGen.Runtime;
using SkiaSharp;
using Vortice.Direct3D11;
using Vortice.Direct3D12;
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
        
        // Cached resources - only recreate when necessary
        private ID3D12Resource? backBuffer;
        private GRD3DTextureResourceInfo? resourceInfo;
        private GRBackendTexture? backendTexture;
        private uint currentBackBufferIndex = uint.MaxValue; // Track buffer changes
        
        public SKSurface? Surface { get; protected set; }
        public GRContext? grContext { get; protected set; }

        public static SKSamplingOptions SamplingOptions { get => new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear); }

        public Action? OnRebuildAdditionals { get; set; }
        public Action? OnDisposeAdditionals { get; set; }

        protected int width, height;
        private bool resourcesNeedRecreation = true;
        private readonly object resourceLock = new object();

        public SkiaDirectCompositionContext(FWindow window, Action<SKCanvas> drawAction)
        {
            this.window = new(window);
            this.DrawAction = drawAction;

            FLogger.Log<SkiaDirectCompositionContext>("Creating SkiaDirectCompositionContext...");

            // Assigning initial size
            width = (int)window.Shape.ClientSize.x;
            height = (int)window.Shape.ClientSize.y;

            // Creating DirectCompositionContext and getting adapter
            FLogger.Log<SkiaDirectCompositionContext>("Creating DirectCompositionContext for SDXCC...");
            DirectCompositionContext = new DirectCompositionContext(
                window.hWnd,
                (int)window.Shape.ClientSize.x,
                (int)window.Shape.ClientSize.y,
                Vortice.DXGI.Format.B8G8R8A8_UNorm
            );

            FLogger.Log<SkiaDirectCompositionContext>("Getting adapter...");
            using var adapter = DirectCompositionContext.GetHardwareAdapter();

            // Creating backend context for Skia D3D
            FLogger.Log<SkiaDirectCompositionContext>("Creating D3D Backend Context...");
            using var backendContext = new GRD3DBackendContext()
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

            Window.Callbacks.OnWindowResize += ResizeWithStretch;

            FLogger.Log<SkiaDirectCompositionContext>("Done creating SkiaDirectCompositionContext!");
            FLogger.Log<SkiaDirectCompositionContext>("");
        }

        private void EnsureResourcesCreated()
        {
            lock (resourceLock)
            {
                if (!resourcesNeedRecreation && Surface != null)
                {
                    // Check if back buffer index changed
                    var currentIndex = DirectCompositionContext?.SwapChain?.CurrentBackBufferIndex ?? 0;
                    if (currentIndex == currentBackBufferIndex)
                        return; // Resources are still valid
                }

                CreateTexturesAndSurface();
                resourcesNeedRecreation = false;
            }
        }

        private void CreateTexturesAndSurface()
        {
            try
            {
                // Wait for GPU to finish with current resources
                DirectCompositionContext?.WaitForGpu();

                // Dispose old resources
                DisposeSkiaResources();

                // Getting back buffer index
                var backBufferIndex = DirectCompositionContext?.SwapChain?.CurrentBackBufferIndex
                    ?? throw new NullReferenceException("Current Back Buffer Index could not be acquired.");

                currentBackBufferIndex = backBufferIndex;

                // Grabbing new back buffer
                backBuffer = DirectCompositionContext.SwapChain.GetBuffer<ID3D12Resource>(backBufferIndex);

                // Creating resource info for the backend texture
                resourceInfo = new GRD3DTextureResourceInfo
                {
                    Resource = backBuffer.NativePointer,
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

                // Make sure to redraw the content on the new surface
                Window.Redraw();

                if (Surface == null)
                    throw new Exception("Failed to create Skia surface");
            }
            catch (Exception ex)
            {
                FLogger.Error($"Failed to create textures and surface: {ex.Message}");
                DisposeSkiaResources();
                throw;
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
        }

        internal void Draw()
        {
            try
            {
                // Ensure resources are created/updated only when needed
                EnsureResourcesCreated();

                if (Surface?.Canvas == null)
                {
                    FLogger.Warn("Surface or Canvas is null, skipping draw");
                    return;
                }

                // Invoking the draw action
                DrawAction?.Invoke(Surface.Canvas);

                // Flush drawing commands efficiently
                Surface.Canvas.Flush();
                Surface.Flush();

                // Submit to GPU and present
                DirectCompositionContext?.Present(null, PresentFlags.None);
            }
            catch (Exception ex)
            {
                FLogger.Error($"Error during draw: {ex.Message}");
                // Mark resources for recreation on next frame
                resourcesNeedRecreation = true;
            }
        }

        internal void OnResize(Vector2 size)
        {
            OnResizeSynced(size);
        }

        public void ResizeWithStretch(Vector2 size)
        {
            // Not needed, resizing is done directly now
            return;

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

        private void OnResizeSynced(Vector2 size)
        {
            lock (resourceLock)
            {
                size *= Window.Shape.WindowDPIScale;

                if (width == size.x && height == size.y) return;
                FLogger.Log<SkiaDirectCompositionContext>($"Resizing SDXCC: {size}");

                // Setting new sizes
                width = (int)size.x;
                height = (int)size.y;

                // Updating direct composition context
                FLogger.Log<SkiaDirectCompositionContext>($"Resizing SDXCC: {width}, {height}");
                DirectCompositionContext?.Resize(width, height, DisposeRenderTargets);

                // Mark resources for recreation
                resourcesNeedRecreation = true;

                // Recreate skia resources
                EnsureResourcesCreated();

                // Commiting changes to DirectComposition device
                FLogger.Log<SkiaDirectCompositionContext>($"Commiting changes to DirectComposition device");

                // Reset transformation and effects
                DirectCompositionContext?.RootVisual?.SetTransform(null);
                DirectCompositionContext?.RootVisual?.SetEffect(null);

                // Committing to the dcomp device
                DirectCompositionContext?.RootVisual?.SetContent(DirectCompositionContext.SwapChain);
                DirectCompositionContext?.DCompDevice?.Commit();
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

        public SKImage? CaptureWindowRegion(SKRect region, float quality)
        {
            lock (resourceLock)
            {
                if (Surface == null)
                    return null;

                try
                {
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

            if (grContext == null)
                throw new InvalidOperationException("GRContext is not initialized.");

            // Check if device is in a valid state
            var device = DirectCompositionContext?.Device;
            if (device == null)
                throw new InvalidOperationException("D3D12 Device is null");

            // Check device removed reason
            var reason = device.DeviceRemovedReason;
            if (reason != 0) // S_OK
                throw new InvalidOperationException("Device removed: " + device.DeviceRemovedReason);

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
                ResourceState = (int)ResourceStates.Present,
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
            }
        }
    }
}