using System.Runtime.InteropServices;
using FenUISharp.Logging;
using FenUISharp.Mathematics;
using FenUISharp.Objects;
using SkiaSharp;
using Vortice.Direct3D12;
using Vortice.DirectComposition;
using Vortice.DXGI;

namespace FenUISharp
{
    public class WindowRenderResources : IDisposable
    {
        public FWindow Window { get; }
        public Action<SKCanvas>? DrawAction { get; set; }

        public IDCompositionTarget? DCompTarget { get; private set; }
        public IDCompositionVisual? RootVisual { get; private set; }
        public IDXGISwapChain3? SwapChain { get; private set; }

        private IDCompositionDevice _dcompDevice = null!;
        private ID3D12CommandQueue? _commandQueue;
        private ID3D12CommandAllocator? _commandAllocator;
        private ID3D12GraphicsCommandList? _commandList;

        public GRContext? grContext { get; private set; }
        private GRD3DBackendContext? _backendContext;

        private ID3D12Fence? _fence;
        private AutoResetEvent? _fenceEvent;
        private ulong _fenceValue;

        private ID3D12Resource? _renderTarget;
        private GRD3DTextureResourceInfo? _resourceInfo;
        private GRBackendTexture? _backendTexture;
        private SKSurface? _surface;

        private ID3D12Resource? _lastBackBuffer;

        public SKSurface? Surface => _surface;

        private int _width;
        private int _height;
        private bool _resourcesNeedRecreation = true;
        private bool _deviceLost;
        private readonly object _resourceLock = new();

        private int _resizeOverflowPadding = 350;
        private bool _wasResizing;
        private Vector2 _currentBufferSizeIncludingPad;

        internal enum ResizingMethod { Stretch, PerFrame, SmartSmoothing }
        internal ResizingMethod Method { get; set; } = ResizingMethod.SmartSmoothing;

        public Action? OnRebuildAdditionals { get; set; }
        public Action? OnDisposeAdditionals { get; set; }
        public Action<SKSurface>? OnFrameDone { get; set; }

        public WindowRenderResources(FWindow window, Action<SKCanvas> drawAction)
        {
            Window = window;
            DrawAction = drawAction;

            var dxcc = DirectCompositionContext.Instance;
            var clientSize = window.Shape.ClientSize;
            _width = (int)clientSize.x;
            _height = (int)clientSize.y;

            FLogger.Log<WindowRenderResources>($"Creating WindowRenderResources for window {window.hWnd}...");

            CreateCommandQueue();
            _backendContext = CreateBackendContext();
            grContext = SkiaDirectCompositionContext.CreateGrContext(_commandQueue);
            CreateSwapChainAndDComp();
            CreateCommandObjects();
            InitFence();

            Window.Callbacks.OnWindowResize += OnWindowResize;
            Window.Callbacks.OnWindowEndResize += OnWindowEndResize;
            Window.Callbacks.DPIChanged += WindowDPIChanged;

            FLogger.Log<WindowRenderResources>("Done creating WindowRenderResources!");
        }

        private void CreateCommandQueue()
        {
            var dxcc = DirectCompositionContext.Instance;
            var commandQueueDesc = new CommandQueueDescription(CommandListType.Direct);
            _commandQueue = dxcc.Device.CreateCommandQueue(commandQueueDesc);
        }

        private GRD3DBackendContext CreateBackendContext()
        {
            var dxcc = DirectCompositionContext.Instance;
            var adapter = dxcc.Adapter ?? throw new InvalidOperationException("Adapter is null");

            return new GRD3DBackendContext()
            {
                Device = dxcc.Device.NativePointer,
                Adapter = adapter.NativePointer,
                ProtectedContext = false,
                Queue = _commandQueue?.NativePointer
                    ?? throw new NullReferenceException("Native Pointer of Command Queue is null.")
            };
        }

        private void CreateSwapChainAndDComp()
        {
            var dxcc = DirectCompositionContext.Instance;
            var hWnd = Window.hWnd;

            FLogger.Log<WindowRenderResources>("Creating per-window DirectComposition device...");
            _dcompDevice = dxcc.CreateDCompDevice();

            FLogger.Log<WindowRenderResources>("Creating DComposition target for window...");
            var hr = _dcompDevice.CreateTargetForHwnd(hWnd, true, out IDCompositionTarget target);
            if (hr.Failure || target == null)
                throw new InvalidOperationException($"Failed to create DirectComposition target. HRESULT: {hr}");
            DCompTarget = target;

            FLogger.Log<WindowRenderResources>("Creating DComposition visual...");
            hr = _dcompDevice.CreateVisual(out IDCompositionVisual visual);
            if (hr.Failure || visual == null)
                throw new InvalidOperationException($"Failed to create DirectComposition visual. HRESULT: {hr}");
            RootVisual = visual;

            DCompTarget.SetRoot(RootVisual);

            FLogger.Log<WindowRenderResources>($"Creating swap chain with size {_width}x{_height}...");
            SwapChainDescription1 swapChainDesc = new()
            {
                Width = (uint)_width,
                Height = (uint)_height,
                Format = dxcc.ColorFormat,
                BufferCount = 2,
                BufferUsage = Usage.RenderTargetOutput,
                SampleDescription = new SampleDescription(1, 0),
                Scaling = Scaling.Stretch,
                SwapEffect = SwapEffect.FlipDiscard,
                AlphaMode = AlphaMode.Premultiplied
            };

            var tempSwap = dxcc.Factory.CreateSwapChainForComposition(_commandQueue, swapChainDesc);
            SwapChain = tempSwap.QueryInterface<IDXGISwapChain3>();

            RootVisual?.SetContent(SwapChain);
            _dcompDevice.Commit();
        }

        private void CreateCommandObjects()
        {
            var dxcc = DirectCompositionContext.Instance;
            _commandAllocator = dxcc.Device.CreateCommandAllocator(CommandListType.Direct);
            _commandList = dxcc.Device.CreateCommandList<ID3D12GraphicsCommandList>(
                0, CommandListType.Direct, _commandAllocator, null
            );
            _commandList.Close();
        }

        private void InitFence()
        {
            var dxcc = DirectCompositionContext.Instance;
            _fence = dxcc.Device.CreateFence(0, FenceFlags.None);
            _fenceValue = 1;
            _fenceEvent = new AutoResetEvent(false);
        }

        private void WindowDPIChanged()
        {
            FLogger.Log<WindowRenderResources>($"New window DPI: {Window.Shape.WindowDPIScale}");
            PerformBufferResize(Window.Shape.Size);
            Window.Redraw();
        }

        private void OnWindowResize(Vector2 size)
        {
            if (!_wasResizing) OnResizeStart(size);
            _wasResizing = true;

            lock (_resourceLock)
            {
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
                    if (
                        size.x >= (_currentBufferSizeIncludingPad.x - 25) ||
                        size.y >= (_currentBufferSizeIncludingPad.y - 25)
                        )
                    {
                        _currentBufferSizeIncludingPad = size + new Vector2(_resizeOverflowPadding, _resizeOverflowPadding);
                        PerformBufferResize(_currentBufferSizeIncludingPad);
                        FLogger.Log<WindowRenderResources>("Resize including padding");
                    }
                }
            }
        }

        private void OnResizeStart(Vector2 size)
        {
            if (Method == ResizingMethod.SmartSmoothing)
                _currentBufferSizeIncludingPad = size;
        }

        private void OnWindowEndResize(Vector2 size)
        {
            _wasResizing = false;
            FLogger.Log<WindowRenderResources>("Resize using correct dimensions");
            PerformBufferResize(size);
        }

        private void ResizeWithStretch(Vector2 size)
        {
            if (_dcompDevice == null || RootVisual == null || !Window.Procedure._isSizeMoving) return;

            int newWidth = (int)size.x;
            int newHeight = (int)size.y;

            float scaleX = (float)newWidth / (float)_width;
            float scaleY = (float)newHeight / (float)_height;

            var transform = _dcompDevice.CreateScaleTransform();
            transform?.SetScaleX(scaleX);
            transform?.SetScaleY(scaleY);
            RootVisual?.SetTransform(transform);

            _dcompDevice.Commit();
        }

        private void PerformBufferResize(Vector2 size)
        {
            lock (_resourceLock)
            {
                int newW = (int)size.x;
                int newH = (int)size.y;
                if (_width == newW && _height == newH) return;
                FLogger.Log<WindowRenderResources>($"Resizing: {size}");

                _width = newW;
                _height = newH;

                WaitForGpu();

                FLogger.Log<WindowRenderResources>($"Resizing DX resources: {_width}, {_height}");

                var dxcc = DirectCompositionContext.Instance;

                _lastBackBuffer?.Dispose();
                _lastBackBuffer = null;

                DisposeRenderTarget();

                WaitForGpu();

                var hr = SwapChain?.ResizeBuffers(
                    2,
                    (uint)_width,
                    (uint)_height,
                    dxcc.ColorFormat,
                    SwapChainFlags.None
                );

                _resourcesNeedRecreation = true;
                EnsureResourcesCreated();

                OnDisposeAdditionals?.Invoke();
                OnRebuildAdditionals?.Invoke();

                RootVisual?.SetContent(SwapChain);
                _dcompDevice.Commit();

                FLogger.Log<WindowRenderResources>($"Resize completed");
            }
        }

        private void EnsureResourcesCreated()
        {
            lock (_resourceLock)
            {
                if (!_resourcesNeedRecreation && _surface != null && SkiaDirectCompositionContext.IsDeviceValid())
                    return;

                _resourcesNeedRecreation = false;
                CreateTextureAndSurface();
            }
        }

        private void CreateTextureAndSurface()
        {
            var dxcc = DirectCompositionContext.Instance;

            FLogger.Log<WindowRenderResources>("Recreating skia surface...");

            try
            {
                if (!SkiaDirectCompositionContext.IsDeviceValid())
                    return;

                WaitForGpu();

                _surface?.Dispose();
                _surface = null;
                _backendTexture?.Dispose();
                _backendTexture = null;
                _resourceInfo?.Dispose();
                _resourceInfo = null;
                _renderTarget?.Dispose();
                _renderTarget = null;

                var resourceDescription = ResourceDescription.Texture2D(
                    Format.B8G8R8A8_UNorm,
                    (uint)_width,
                    (uint)_height,
                    1, 1, 1, 0,
                    ResourceFlags.AllowRenderTarget,
                    TextureLayout.Unknown
                );

                var clearValue = new ClearValue
                {
                    Format = Format.B8G8R8A8_UNorm,
                    Color = new Vortice.Mathematics.Color4(0, 0, 0, 0)
                };

                _renderTarget = dxcc.Device.CreateCommittedResource(
                    new HeapProperties(HeapType.Default),
                    HeapFlags.None,
                    resourceDescription,
                    ResourceStates.RenderTarget,
                    clearValue
                );

                if (_renderTarget == null)
                    throw new Exception("Failed to create persistent render target");

                _resourceInfo = new GRD3DTextureResourceInfo
                {
                    Resource = _renderTarget.NativePointer,
                    ResourceState = (int)ResourceStates.RenderTarget,
                    Format = (uint)Format.B8G8R8A8_UNorm,
                    SampleCount = 1,
                    LevelCount = 1,
                    SampleQualityPattern = 0,
                    Protected = false
                };

                _backendTexture = new GRBackendTexture(_width, _height, _resourceInfo);

                _surface = SKSurface.Create(grContext, _backendTexture, GRSurfaceOrigin.TopLeft, SKColorType.Bgra8888);

                if (_surface == null)
                    throw new Exception("Failed to create Skia surface");

                _surface.Canvas.Clear(SKColors.Transparent);
                _surface.Canvas.Flush();

                Window.Redraw();
            }
            catch (Exception ex)
            {
                FLogger.Error($"Failed to create texture and surface: {ex.Message}");
                DisposeRenderTarget();
                _resourcesNeedRecreation = true;
                throw;
            }
        }

        public void Draw()
        {
            if (_deviceLost || _isDisposed) return;

            if (!Monitor.TryEnter(_resourceLock, 0))
                return;

            try
            {
                if (!SkiaDirectCompositionContext.IsDeviceValid() || _resourcesNeedRecreation)
                {
                    EnsureResourcesCreated();
                    if (_resourcesNeedRecreation)
                        return;
                }

                WaitForGpu();

                if (_surface?.Canvas == null)
                    return;

                var canvas = _surface.Canvas;
                canvas.Save();

                try
                {
                    DrawAction?.Invoke(canvas);
                    OnFrameDone?.Invoke(_surface);
                }
                finally
                {
                    canvas.Restore();
                }

                canvas.Flush();
                _surface.Flush();
                grContext?.Submit(false);

                _lastBackBuffer?.Dispose();
                var backBuffer = SwapChain?.GetBuffer<ID3D12Resource>(SwapChain.CurrentBackBufferIndex)
                    ?? throw new NullReferenceException("SwapChain null");
                _lastBackBuffer = backBuffer;

                _commandAllocator?.Reset();
                _commandList?.Reset(_commandAllocator, null);

                var barriers = new ResourceBarrier[]
                {
                    ResourceBarrier.BarrierTransition(
                        _renderTarget,
                        ResourceStates.RenderTarget,
                        ResourceStates.CopySource),
                    ResourceBarrier.BarrierTransition(
                        backBuffer,
                        ResourceStates.Present,
                        ResourceStates.CopyDest)
                };

                _commandList?.ResourceBarrier(barriers);
                _commandList?.CopyResource(backBuffer, _renderTarget);

                var backBarriers = new ResourceBarrier[]
                {
                    ResourceBarrier.BarrierTransition(
                        _renderTarget,
                        ResourceStates.CopySource,
                        ResourceStates.RenderTarget)
                };

                _commandList?.ResourceBarrier(backBarriers);
                _commandList?.Close();
                _commandQueue?.ExecuteCommandLists(new ID3D12CommandList[] { _commandList });

                _commandQueue?.Signal(_fence, ++_fenceValue);

                SwapChain.Present(1, PresentFlags.None);
                _dcompDevice.Commit();
            }
            catch (Exception ex)
            {
                FLogger.Error($"Exception in WindowRenderResources.Draw: {ex.Message}");
            }
            finally
            {
                Monitor.Exit(_resourceLock);
            }
        }

        public void WaitForGpu()
        {
            _commandQueue?.Signal(_fence, ++_fenceValue);

            if (_fence?.CompletedValue < _fenceValue)
            {
                _fence?.SetEventOnCompletion(_fenceValue, _fenceEvent);
                if (!_fenceEvent?.WaitOne(TimeSpan.FromSeconds(1)) ?? false)
                {
                    var reason = DirectCompositionContext.Instance.Device?.DeviceRemovedReason;
                    FLogger.Error($"GPU TIMEOUT! DeviceRemovedReason: {reason}");
                }
            }
        }

        public SKImage? CaptureWindowRegion(SKRect region, float quality)
        {
            lock (_resourceLock)
            {
                if (_surface == null || !SkiaDirectCompositionContext.IsDeviceValid())
                    return null;

                try
                {
                    WaitForGpu();

                    grContext?.Submit(true);

                    _surface.Canvas.Flush();
                    _surface.Flush();

                    using var snapshot = _surface.Snapshot(new SKRectI(
                        (int)region.Left, (int)region.Top,
                        (int)region.Right, (int)region.Bottom));
                    var scaled = RMath.CreateLowResImage(snapshot, RMath.Clamp(quality, 0.01f, 1f), SkiaDirectCompositionContext.SamplingOptions);

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
                return new FAdditionalSurface(SKSurface.Create(info), null, null, null, info, this);
            }

            if (grContext == null || !SkiaDirectCompositionContext.IsDeviceValid())
                throw new InvalidOperationException("GRContext is not initialized or device is invalid.");

            var dxcc = DirectCompositionContext.Instance;
            var device = dxcc.Device;
            if (device == null)
                throw new InvalidOperationException("D3D12 Device is null");

            var texDesc = new ResourceDescription
            {
                Dimension = ResourceDimension.Texture2D,
                Alignment = 0,
                Width = (ulong)info.Width,
                Height = (uint)info.Height,
                DepthOrArraySize = 1,
                MipLevels = 1,
                Format = Format.B8G8R8A8_UNorm,
                SampleDescription = new SampleDescription(1, 0),
                Layout = TextureLayout.Unknown,
                Flags = ResourceFlags.AllowRenderTarget
            };

            var clearValue = new ClearValue
            {
                Format = Format.B8G8R8A8_UNorm,
                Color = new Vortice.Mathematics.Color4(0, 0, 0, 0)
            };

            var texture = device.CreateCommittedResource(
                new HeapProperties(HeapType.Default),
                HeapFlags.None,
                texDesc,
                ResourceStates.RenderTarget,
                clearValue
            );

            var resourceInfo = new GRD3DTextureResourceInfo
            {
                Resource = texture?.NativePointer ?? throw new NullReferenceException("Texture is null"),
                ResourceState = (int)ResourceStates.RenderTarget,
                Format = (int)Format.B8G8R8A8_UNorm,
                SampleCount = 1,
                LevelCount = 1,
                SampleQualityPattern = 0,
                Protected = false
            };

            var backendTex = new GRBackendTexture(info.Width, info.Height, resourceInfo);

            var surface = SKSurface.Create(grContext, backendTex, GRSurfaceOrigin.TopLeft, info.ColorType);

            return new FAdditionalSurface(surface, texture, resourceInfo, backendTex, info, this);
        }

        private void DisposeRenderTarget()
        {
            FLogger.Log<WindowRenderResources>("Disposing render target...");

            _surface?.Canvas?.Flush();
            _surface?.Flush();
            grContext?.Flush();
            grContext?.Submit(true);

            _surface?.Dispose();
            _surface = null;
            _backendTexture?.Dispose();
            _backendTexture = null;
            _resourceInfo?.Dispose();
            _resourceInfo = null;
            _renderTarget?.Dispose();
            _renderTarget = null;

            _resourcesNeedRecreation = true;
        }

        private bool _isDisposed;

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            FLogger.Log<WindowRenderResources>($"Disposing WindowRenderResources for {Window.hWnd}...");

            // Use a timeout to avoid deadlock if the logic thread is still rendering
            if (!Monitor.TryEnter(_resourceLock, TimeSpan.FromSeconds(2)))
            {
                FLogger.Error("Timeout waiting for resource lock during WindowRenderResources.Dispose");
            }

            try
            {
                // Notify all FAdditionalSurface objects to clean up their resources
                OnDisposeAdditionals?.Invoke();
                OnDisposeAdditionals = null;
                OnRebuildAdditionals = null;

                if (SkiaDirectCompositionContext.IsDeviceValid())
                {
                    try { WaitForGpu(); } catch { }
                }

                try { DisposeRenderTarget(); }
                catch { FLogger.Error("DisposeRenderTarget failed, continuing cleanup..."); }

                try { _lastBackBuffer?.Dispose(); } catch { }
                _lastBackBuffer = null;

                if (grContext != null)
                {
                    if (SkiaDirectCompositionContext.IsDeviceValid())
                    {
                        try { grContext.Dispose(); }
                        catch { SkiaDirectCompositionContext.DisposeGrContextSafely(grContext, _backendContext); }
                    }
                    else
                    {
                        SkiaDirectCompositionContext.DisposeGrContextSafely(grContext, _backendContext);
                    }
                    grContext = null;
                }

                try { _commandAllocator?.Dispose(); } catch { }
                _commandAllocator = null;
                try { _commandList?.Dispose(); } catch { }
                _commandList = null;

                try { _fenceEvent?.Set(); } catch { }
                try { _fenceEvent?.Dispose(); } catch { }
                _fenceEvent = null;
                try { _fence?.Dispose(); } catch { }
                _fence = null;

                // Clear DComp references before disposal so the composition tree
                // properly releases its references to child objects
                try
                {
                    if (DCompTarget != null)
                        DCompTarget.SetRoot(null);
                    if (RootVisual != null)
                        RootVisual.SetContent(null);
                    if (_dcompDevice != null)
                        _dcompDevice.Commit();
                }
                catch { }

                try { SwapChain?.Dispose(); } catch { }
                SwapChain = null;

                try { RootVisual?.Dispose(); } catch { }
                RootVisual = null;
                try { DCompTarget?.Dispose(); } catch { }
                DCompTarget = null;

                try { _dcompDevice?.Dispose(); } catch { }
                _dcompDevice = null!;

                try { _commandQueue?.Dispose(); } catch { }
                _commandQueue = null;

                try { _backendContext?.Dispose(); } catch { }
                _backendContext = null;

                DrawAction = null;

                // Unsubscribe from window callbacks to prevent lingering references
                try
                {
                    Window.Callbacks.OnWindowResize -= OnWindowResize;
                    Window.Callbacks.OnWindowEndResize -= OnWindowEndResize;
                    Window.Callbacks.DPIChanged -= WindowDPIChanged;
                }
                catch { }

                FLogger.Log<WindowRenderResources>($"Done disposing WindowRenderResources for {Window.hWnd}!");
            }
            finally
            {
                if (Monitor.IsEntered(_resourceLock))
                    Monitor.Exit(_resourceLock);
            }
        }
    }
}
