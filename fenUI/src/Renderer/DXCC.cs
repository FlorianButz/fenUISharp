using System;
using SkiaSharp;
using Vortice.DXGI;
using Vortice.Direct3D12;
using Vortice.Direct3D;
using Vortice.Mathematics;
using static Vortice.Direct3D12.D3D12;
using Vortice.DirectComposition;
using System.Runtime.InteropServices;
using FenUISharp.Logging;
using Vortice.Direct3D12.Debug;
using SharpGen.Runtime;

namespace FenUISharp
{
    public class DirectCompositionContext : IDisposable
    {
        public int SizeX { get; protected set; }
        public int SizeY { get; protected set; }

        // Window handle
        public IntPtr hWnd { get; }

        // Formats
        public Format ColorFormat { get; }
        public Format DepthStencilFormat { get; }

        public IDXGIFactory2 Factory { get; private set; }
        public readonly ID3D12Device Device;
        public readonly FeatureLevel FeatureLevel;

        // DirectComposition objects
        public IDCompositionDevice? DCompDevice { get; private set; }
        public IDCompositionDevice3? DCompDevice3 { get; private set; }
        public IDCompositionTarget? DCompTarget { get; private set; }
        public IDCompositionVisual? RootVisual { get; private set; }

        // DirectX objects
        public IDXGISwapChain3? SwapChain { get; private set; }
        public ID3D12CommandQueue CommandQueue { get; private set; }
        public ID3D12GraphicsCommandList? CommandList { get; private set; }
        public ID3D12CommandAllocator? CommandAllocator { get; private set; }
        private ID3D12Resource? LastBackBuffer { get; set; }

        // DX11 objects
        Vortice.Direct3D11.ID3D11Device? d3d11Device;
        Vortice.Direct3D11.ID3D11DeviceContext? d3d11Context;

        private ID3D12Fence? Fence;
        private AutoResetEvent? FenceEvent;

        // With 4 fence increment per frame, refresh rate set to 60hz
        // Overflow can happen after ~207.125 days
        // Might be a problem later on, but keep for now
        private ulong _fenceValue;

        public ColorSpaceType ColorSpace = ColorSpaceType.RgbFullG22NoneP709;
        public uint BufferCount { get; } = 2;

        private IDXGIAdapter1? _cachedAdapter;

        public DirectCompositionContext(IntPtr windowHandle, int sx, int sy, Format colorFormat = Format.B8G8R8A8_UNorm, Format depthStencilFormat = Format.D32_Float)
        {
            try
            {
                FLogger.Log<DirectCompositionContext>("Starting DirectCompositionContext initialization...");

                this.SizeX = sx;
                this.SizeY = sy;
                this.hWnd = windowHandle;
                this.ColorFormat = colorFormat;
                this.DepthStencilFormat = depthStencilFormat;

                if (FenUI.debugEnabled)
                {
                    Vortice.Direct3D12.D3D12.D3D12GetDebugInterface(out ID3D12Debug3? debug);
                    if (debug != null)
                    {
                        debug.EnableDebugLayer();
                        debug.SetEnableGPUBasedValidation(false);
                        debug.Dispose();
                    }
                }

                FLogger.Log<DirectCompositionContext>("Creating DXGI Factory...");
                Factory = DXGI.CreateDXGIFactory1<IDXGIFactory2>();

                FLogger.Log<DirectCompositionContext>("Getting hardware adapter...");
                _cachedAdapter = GetHardwareAdapter();
                FLogger.Log<DirectCompositionContext>($"Hardware adapter: {_cachedAdapter.Description1.Description}");

                FLogger.Log<DirectCompositionContext>("Creating D3D12 Device...");
                FeatureLevel = FeatureLevel.Level_11_0;

                // Add retry logic for device creation
                ID3D12Device? device = null;
                var attempts = 0;
                const int maxAttempts = 3;

                while (device == null && attempts < maxAttempts)
                {
                    try
                    {
                        device = D3D12CreateDevice<ID3D12Device>(_cachedAdapter, FeatureLevel);
                    }
                    catch (SharpGenException ex) when (ex.ResultCode == Vortice.DXGI.ResultCode.InvalidCall && attempts < maxAttempts - 1)
                    {
                        FLogger.Log<DirectCompositionContext>($"Device creation attempt {attempts + 1} failed, retrying...");
                        System.Threading.Thread.Sleep(100); // Brief delay
                        attempts++;
                    }
                }

                if (device == null)
                    throw new InvalidOperationException("Failed to create D3D12 device after multiple attempts");

                Device = device;
                FLogger.Log<DirectCompositionContext>($"D3D12 Device created: {Device.NativePointer}");

                // Rest of initialization...
                var commandQueueDesc = new CommandQueueDescription(CommandListType.Direct);
                CommandQueue = Device.CreateCommandQueue(commandQueueDesc);

                CreateDirectCompositionDevice();
                CreateSwapchain();
                CreateCommandObjects();
                InitFence();

                FLogger.Log<DirectCompositionContext>("DirectCompositionContext initialization completed!");
            }
            catch (Exception ex)
            {
                FLogger.Log<DirectCompositionContext>($"Error in DirectCompositionContext constructor: {ex}");
                try { Dispose(); } catch { }
                throw;
            }
        }

        private void CreateDirectCompositionDevice()
        {
            FLogger.Log<DirectCompositionContext>("Creating D3D11 device for DirectComposition interop...");

            // Use the cached adapter instead of getting a new one
            if (_cachedAdapter == null)
                throw new InvalidOperationException("Cached adapter is null");

            var creationFlags = Vortice.Direct3D11.DeviceCreationFlags.BgraSupport;

            if (FenUI.debugEnabled)
                creationFlags |= Vortice.Direct3D11.DeviceCreationFlags.Debug;

            var featureLevels = new[] {
        Vortice.Direct3D.FeatureLevel.Level_11_0,
        Vortice.Direct3D.FeatureLevel.Level_10_1,
        Vortice.Direct3D.FeatureLevel.Level_10_0
    };

            // Add retry logic here too
            var attempts = 0;
            const int maxAttempts = 3;

            while (d3d11Device == null && attempts < maxAttempts)
            {
                try
                {
                    var hr = Vortice.Direct3D11.D3D11.D3D11CreateDevice(
                        _cachedAdapter,
                        Vortice.Direct3D.DriverType.Unknown,
                        creationFlags,
                        featureLevels,
                        out d3d11Device,
                        out var featureLevel,
                        out d3d11Context);

                    if (hr.Success && d3d11Device != null)
                    {
                        FLogger.Log<DirectCompositionContext>($"D3D11 device created with feature level: {featureLevel}");
                        break;
                    }
                }
                catch (Exception ex) when (attempts < maxAttempts - 1)
                {
                    FLogger.Log<DirectCompositionContext>($"D3D11 device creation attempt {attempts + 1} failed: {ex.Message}");
                    System.Threading.Thread.Sleep(50);
                }

                attempts++;
            }

            if (d3d11Device == null)
                throw new InvalidOperationException("Failed to create D3D11 device after multiple attempts");

            using var dxgiDevice = d3d11Device.QueryInterface<IDXGIDevice>();
            if (dxgiDevice == null)
                throw new InvalidOperationException("Failed to get IDXGIDevice from D3D11 device");

            FLogger.Log<DirectCompositionContext>("Creating DirectComposition device from D3D11 DXGI device...");

            var hr2 = DComp.DCompositionCreateDevice3(dxgiDevice, out IDCompositionDevice? dcompDevice);
            if (hr2.Failure || dcompDevice == null)
                throw new InvalidOperationException($"Failed to create DirectComposition device. HRESULT: {hr2}");

            DCompDevice = dcompDevice;
            DCompDevice3 = DCompDevice.QueryInterface<IDCompositionDevice3>();

            FLogger.Log<DirectCompositionContext>("Creating composition target for window...");
            hr2 = DCompDevice.CreateTargetForHwnd(hWnd, true, out IDCompositionTarget target);
            if (hr2.Failure || target == null)
                throw new InvalidOperationException($"Failed to create DirectComposition target. HRESULT: {hr2}");

            DCompTarget = target;

            FLogger.Log<DirectCompositionContext>("Creating root visual...");
            hr2 = DCompDevice.CreateVisual(out IDCompositionVisual visual);
            if (hr2.Failure || visual == null)
                throw new InvalidOperationException($"Failed to create DirectComposition visual. HRESULT: {hr2}");

            RootVisual = visual;

            FLogger.Log<DirectCompositionContext>("Setting root visual on target...");
            DCompTarget.SetRoot(RootVisual);

            FLogger.Log<DirectCompositionContext>("Done creating DXCC");
        }

        private void CreateSwapchain()
        {
            try
            {
                SwapChain?.Dispose();

                var width = (uint)SizeX;
                var height = (uint)SizeY;

                FLogger.Log<DirectCompositionContext>($"Creating swap chain with size {width}x{height}");

                SwapChainDescription1 swapChainDesc = new()
                {
                    Width = width,
                    Height = height,
                    Format = ColorFormat,
                    BufferCount = BufferCount,
                    BufferUsage = Usage.RenderTargetOutput,
                    SampleDescription = new SampleDescription(1, 0),
                    Scaling = Scaling.Stretch,
                    SwapEffect = SwapEffect.FlipDiscard,
                    AlphaMode = AlphaMode.Premultiplied
                };

                FLogger.Log<DirectCompositionContext>("Creating swap chain for composition...");
                var tempSwap = Factory.CreateSwapChainForComposition(CommandQueue, swapChainDesc);
                SwapChain = tempSwap.QueryInterface<IDXGISwapChain3>();

                FLogger.Log<DirectCompositionContext>("Setting swap chain content on root visual...");
                RootVisual?.SetContent(SwapChain);
                DCompDevice?.Commit();

                FLogger.Log<DirectCompositionContext>($"Upading color space...");
                UpdateColorSpace();

                FLogger.Log<DirectCompositionContext>($"Done creating SwapChain");
                FLogger.Log<DirectCompositionContext>($"");
            }
            catch (Exception ex)
            {
                FLogger.Log<DirectCompositionContext>($"Exception in CreateSwapchain: {ex}");
                throw;
            }
        }

        private void CreateCommandObjects()
        {
            try
            {
                FLogger.Log<DirectCompositionContext>("Creating command allocator...");
                CommandAllocator = Device.CreateCommandAllocator(CommandListType.Direct);

                FLogger.Log<DirectCompositionContext>("Creating command list...");
                CommandList = Device.CreateCommandList<ID3D12GraphicsCommandList>(
                    0, // nodeMask
                    CommandListType.Direct,
                    CommandAllocator,
                    null // no initial PSO
                );

                // Close the command list initially
                CommandList.Close();
                FLogger.Log<DirectCompositionContext>("Command objects created successfully");
                FLogger.Log<DirectCompositionContext>($"");
            }
            catch (Exception ex)
            {
                FLogger.Log<DirectCompositionContext>($"Exception in CreateCommandObjects: {ex}");
                throw;
            }
        }

        private void UpdateColorSpace()
        {
            try
            {
                if (!Factory.IsCurrent)
                {
                    Factory.Dispose();
                    Factory = DXGI.CreateDXGIFactory1<IDXGIFactory2>();
                }

                ColorSpace = ColorSpaceType.RgbFullG22NoneP709;

                using var swapChain3 = SwapChain?.QueryInterfaceOrNull<IDXGISwapChain3>();
                if (swapChain3 != null)
                {
                    var support = swapChain3.CheckColorSpaceSupport(ColorSpace);
                    if ((support & SwapChainColorSpaceSupportFlags.Present) != 0)
                    {
                        swapChain3.SetColorSpace1(ColorSpace);
                    }
                }
            }
            catch (Exception ex)
            {
                FLogger.Log<DirectCompositionContext>($"Warning: UpdateColorSpace failed: {ex.Message}");
                // Non-critical, continue
            }
        }

        public IDXGIAdapter1 GetHardwareAdapter()
        {
            try
            {
                FLogger.Log<DirectCompositionContext>("Enumerating adapters...");

                if (Factory.QueryInterfaceOrNull<IDXGIFactory6>() is IDXGIFactory6 factory6)
                {
                    FLogger.Log<DirectCompositionContext>("Using IDXGIFactory6 for adapter enumeration");
                    for (uint adapterIndex = 0;
                        factory6.EnumAdapterByGpuPreference(adapterIndex, GpuPreference.HighPerformance, out IDXGIAdapter1? adapter).Success;
                        adapterIndex++)
                    {
                        if (adapter != null && (adapter.Description1.Flags & AdapterFlags.Software) == 0)
                        {
                            FLogger.Log<DirectCompositionContext>($"Found hardware adapter: {adapter.Description1.Description}");
                            FLogger.Log<DirectCompositionContext>($"");
                            return adapter;
                        }
                        adapter?.Dispose();
                    }

                    factory6.Dispose();
                }

                FLogger.Log<DirectCompositionContext>("Using IDXGIFactory2 for adapter enumeration");
                for (uint adapterIndex = 0;
                    Factory.EnumAdapters1(adapterIndex, out IDXGIAdapter1? adapter).Success;
                    adapterIndex++)
                {
                    if (adapter != null && (adapter.Description1.Flags & AdapterFlags.Software) == 0)
                    {
                        FLogger.Log<DirectCompositionContext>($"Found hardware adapter: {adapter.Description1.Description}");
                        FLogger.Log<DirectCompositionContext>($"");
                        return adapter;
                    }
                    adapter?.Dispose();
                }

                throw new InvalidOperationException("No suitable D3D12 adapter found.");
            }
            catch (Exception ex)
            {
                FLogger.Log<DirectCompositionContext>($"Exception in GetHardwareAdapter: {ex}");
                throw;
            }
        }

        public void Resize(int newWidth, int newHeight, Action disposeTargets)
        {
            if (newWidth == SizeX && newHeight == SizeY)
                return;

            SizeX = newWidth;
            SizeY = newHeight;

            d3d11Context?.ClearState();
            d3d11Context?.Flush();

            // FLogger.Log<DirectCompositionContext>($"Setting root visual to null...");
            // RootVisual?.SetContent(null);
            // DCompDevice?.Commit();

            FLogger.Log<DirectCompositionContext>($"Disposing previous render targets...");
            LastBackBuffer?.Dispose();
            disposeTargets?.Invoke();

            // Wait for GPU to finish
            WaitForGpu();

            // Resize swap chain
            FLogger.Log<DirectCompositionContext>($"Resize SwapChain...");
            var hr = SwapChain?.ResizeBuffers(
                BufferCount,
                (uint)SizeX,
                (uint)SizeY,
                ColorFormat,
                SwapChainFlags.None
            );

            FLogger.Log($"ResizeBuffers HRESULT: 0x{hr?.Code:X8} ({hr?.Description})");

            var desc = SwapChain?.Description1;
            FLogger.Log($"SwapChain desc after ResizeBuffers: {desc?.Width}x{desc?.Height}");
        }

        private void InitFence()
        {
            Fence = Device.CreateFence(0, FenceFlags.None);
            _fenceValue = 1;
            FenceEvent = new AutoResetEvent(false);
        }

        internal void WaitForGpu()
        {
            var startTime = DateTime.Now;

            CommandQueue?.Signal(Fence, ++_fenceValue);

            if (Fence?.CompletedValue < _fenceValue)
            {
                // FLogger.Log<DirectCompositionContext>($"Waiting for fence {_fenceValue}, current: {Fence?.CompletedValue}");
                Fence?.SetEventOnCompletion(_fenceValue, FenceEvent);

                // Fence timeout
                if (!FenceEvent?.WaitOne(TimeSpan.FromSeconds(5)) ?? throw new InvalidOperationException("FenceEvent is null"))
                {
                    FLogger.Log<DirectCompositionContext>($"GPU TIMEOUT! Fence never completed");
                    return;
                }
            }

            var elapsed = DateTime.Now - startTime;
            // FLogger.Log<DirectCompositionContext>($"WaitForGpu took: {elapsed.TotalMilliseconds}ms");
        }

        public void Present(Action<(ID3D12GraphicsCommandList commandList, ID3D12Resource backBuffer)>? drawAction = null, PresentFlags flags = PresentFlags.None)
        {
            try
            {
                // Reset command list
                CommandAllocator?.Reset();
                CommandList?.Reset(CommandAllocator, null);

                LastBackBuffer?.Dispose();

                // Get current back buffer
                ID3D12Resource backBuffer = SwapChain?.GetBuffer<ID3D12Resource>(SwapChain.CurrentBackBufferIndex)
                    ?? throw new NullReferenceException("SwapChain null");
                LastBackBuffer = backBuffer;

                // Transition to render target state
                CommandList?.ResourceBarrierTransition(
                    backBuffer,
                    ResourceStates.Present,
                    ResourceStates.RenderTarget
                );

                // Execute custom drawing commands
                drawAction?.Invoke((CommandList ?? throw new NullReferenceException("CommandList null"), backBuffer));

                // Transition back to present state
                CommandList?.ResourceBarrierTransition(
                    backBuffer,
                    ResourceStates.RenderTarget,
                    ResourceStates.Present
                );

                // Execute command list
                CommandList?.Close();
                CommandQueue?.ExecuteCommandLists(new ID3D12CommandList[] { CommandList });

                // Present
                SwapChain.Present(1, flags);

                // Commit composition
                DCompDevice?.Commit();
            }
            catch (Exception ex)
            {
                FLogger.Log<DirectCompositionContext>($"Exception in Present: {ex}");
                throw;
            }
        }

        // Add to DirectCompositionContext class
        private bool _disposed = false;

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                _disposed = true;

                // Wait for GPU to complete all operations
                try
                {
                    WaitForGpu();
                }
                catch (Exception ex)
                {
                    FLogger.Log<DirectCompositionContext>($"Warning: WaitForGpu failed during dispose: {ex.Message}");
                }

                FLogger.Log<DirectCompositionContext>("Disposing DirectCompositionContext...");

                // Dispose in reverse order of creation

                // 1. Clear DirectComposition tree first
                try
                {
                    RootVisual?.SetContent(null);
                    DCompDevice?.Commit();
                }
                catch { /* Ignore errors during cleanup */ }

                // 2. Dispose DirectComposition objects
                RootVisual?.Dispose();
                RootVisual = null!;
                DCompTarget?.Dispose();
                DCompTarget = null!;
                DCompDevice3?.Dispose();
                DCompDevice3 = null!;
                DCompDevice?.Dispose();
                DCompDevice = null!;

                // 3. Dispose D3D11 objects (used by DirectComposition)
                d3d11Context?.ClearState();
                d3d11Context?.Flush();
                d3d11Context?.Dispose();
                d3d11Context = null!;
                d3d11Device?.Dispose();
                d3d11Device = null!;

                // 4. Dispose D3D12 resources
                LastBackBuffer?.Dispose();
                LastBackBuffer = null!;
                CommandList?.Dispose();
                CommandList = null!;
                CommandAllocator?.Dispose();
                CommandAllocator = null!;
                SwapChain?.Dispose();
                SwapChain = null!;
                CommandQueue?.Dispose();
                CommandQueue = null!;

                // 5. Dispose fence objects
                try
                {
                    FenceEvent?.Set(); // Wake up any waiting threads
                }
                catch { }
                FenceEvent?.Dispose();
                FenceEvent = null!;
                Fence?.Dispose();
                Fence = null!;

                // 6. Dispose device and factory last
                Device?.Dispose();
                _cachedAdapter?.Dispose(); // Don't forget the cached adapter!
                _cachedAdapter = null!;
                Factory?.Dispose();
                Factory = null!;

                FLogger.Log<DirectCompositionContext>("DirectCompositionContext disposed successfully");
            }
            catch (Exception ex)
            {
                FLogger.Log<DirectCompositionContext>($"Exception during disposal: {ex}");
            }
        }
    }
}