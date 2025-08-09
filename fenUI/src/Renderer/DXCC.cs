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

                FLogger.Log<DirectCompositionContext>($"Window Handle: {windowHandle}, Size: {sx}x{sy}");

                FLogger.Log<DirectCompositionContext>("Creating DXGI Factory...");
                Factory = DXGI.CreateDXGIFactory1<IDXGIFactory2>();
                FLogger.Log<DirectCompositionContext>("DXGI Factory created successfully");

                FLogger.Log<DirectCompositionContext>("Getting hardware adapter...");
                using var adapter = GetHardwareAdapter();
                FLogger.Log<DirectCompositionContext>($"Hardware adapter: {adapter.Description1.Description}");

                FLogger.Log<DirectCompositionContext>("Creating D3D12 Device...");
                FeatureLevel = FeatureLevel.Level_11_0;
                Device = D3D12CreateDevice<ID3D12Device>(adapter, FeatureLevel);
                FLogger.Log<DirectCompositionContext>($"D3D12 Device created: {Device.NativePointer}");

                FLogger.Log<DirectCompositionContext>("Creating command queue...");
                var commandQueueDesc = new CommandQueueDescription(CommandListType.Direct);
                CommandQueue = Device.CreateCommandQueue(commandQueueDesc);
                FLogger.Log<DirectCompositionContext>("Command queue created successfully");

                FLogger.Log<DirectCompositionContext>("Creating DirectComposition device...");
                CreateDirectCompositionDevice();
                FLogger.Log<DirectCompositionContext>("DirectComposition device created successfully");

                FLogger.Log<DirectCompositionContext>("Creating swap chain...");
                CreateSwapchain();
                FLogger.Log<DirectCompositionContext>("Swap chain created successfully");

                FLogger.Log<DirectCompositionContext>("Creating command objects...");
                CreateCommandObjects();
                FLogger.Log<DirectCompositionContext>("Command objects created successfully");

                FLogger.Log<DirectCompositionContext>("Initiating Fence...");
                InitFence();
                FLogger.Log<DirectCompositionContext>("Fence created successfully");

                FLogger.Log<DirectCompositionContext>("DirectCompositionContext initialization completed!");
            }
            catch (Exception ex)
            {
                FLogger.Log<DirectCompositionContext>($"Error in DirectCompositionContext constructor: {ex}");

                // Clean up any partially created objects
                try { Dispose(); } catch { }

                throw;
            }
        }

        private void CreateDirectCompositionDevice()
        {
            FLogger.Log<DirectCompositionContext>("Creating D3D11 device for DirectComposition interop...");

            using var adapter = GetHardwareAdapter();

            // Create D3D11 device for interop
            var creationFlags = Vortice.Direct3D11.DeviceCreationFlags.BgraSupport;

            if (FenUI.debugEnabled)
                creationFlags |= Vortice.Direct3D11.DeviceCreationFlags.Debug;

            var featureLevels = new[] { Vortice.Direct3D.FeatureLevel.Level_11_0, Vortice.Direct3D.FeatureLevel.Level_10_1, Vortice.Direct3D.FeatureLevel.Level_10_0 };

            var hr = Vortice.Direct3D11.D3D11.D3D11CreateDevice(
                adapter,
                Vortice.Direct3D.DriverType.Unknown,
                creationFlags,
                featureLevels,
                out d3d11Device,
                out var featureLevel,
                out d3d11Context);

            if (hr.Failure)
                throw new InvalidOperationException($"Failed to create D3D11 device for DirectComposition interop: {hr}");

            FLogger.Log<DirectCompositionContext>($"D3D11 device created with feature level: {featureLevel}");

            using var dxgiDevice = d3d11Device.QueryInterface<IDXGIDevice>();

            if (dxgiDevice == null)
                throw new InvalidOperationException("Failed to get IDXGIDevice from D3D11 device");

            FLogger.Log<DirectCompositionContext>("Creating DirectComposition device from D3D11 DXGI device...");

            hr = DComp.DCompositionCreateDevice3(dxgiDevice, out IDCompositionDevice? dcompDevice);
            if (hr.Failure || dcompDevice == null)
                throw new InvalidOperationException($"Failed to create DirectComposition device. HRESULT: {hr}");

            DCompDevice = dcompDevice;
            DCompDevice3 = DCompDevice.QueryInterface<IDCompositionDevice3>();

            FLogger.Log<DirectCompositionContext>("Creating composition target for window...");
            hr = DCompDevice.CreateTargetForHwnd(hWnd, true, out IDCompositionTarget target);
            if (hr.Failure || target == null)
                throw new InvalidOperationException($"Failed to create DirectComposition target. HRESULT: {hr}");

            DCompTarget = target;

            FLogger.Log<DirectCompositionContext>("Creating root visual...");
            hr = DCompDevice.CreateVisual(out IDCompositionVisual visual);
            if (hr.Failure || visual == null)
                throw new InvalidOperationException($"Failed to create DirectComposition visual. HRESULT: {hr}");

            RootVisual = visual;

            FLogger.Log<DirectCompositionContext>("Setting root visual on target...");
            DCompTarget.SetRoot(RootVisual);

            // // Disposing DX11 device
            // FLogger.Log<DirectCompositionContext>($"Disposing D3D11 Device and Context...");
            // d3d11Context.Dispose();
            // d3d11Device.Dispose();

            FLogger.Log<DirectCompositionContext>($"Done creating DXCC");
            FLogger.Log<DirectCompositionContext>($"");
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

        public void Dispose()
        {
            try
            {
                WaitForGpu();

                // Dispoing DComp resources
                FLogger.Log<DirectCompositionContext>("Disposing DirectCompositionContext...");

                RootVisual?.Dispose();
                RootVisual = null!;
                DCompTarget?.Dispose();
                DCompTarget = null!;
                DCompDevice?.Dispose();
                DCompDevice = null!;

                FLogger.Log<DirectCompositionContext>("DirectCompositionContext disposed");

                FLogger.Log<DirectCompositionContext>("Disposing DirectX11...");

                d3d11Context?.Dispose();
                d3d11Device?.Dispose();

                FLogger.Log<DirectCompositionContext>("DirectX11 disposed");

                // Disposing DX resources
                FLogger.Log<DirectCompositionContext>("Disposing DirectX12 resources...");

                CommandList?.Dispose();
                CommandList = null!;
                CommandAllocator?.Dispose();
                CommandAllocator = null!;
                CommandQueue?.Dispose();
                CommandQueue = null!;
                SwapChain?.Dispose();
                SwapChain = null!;
                Device?.Dispose();
                Factory?.Dispose();
                Factory = null!;

                FLogger.Log<DirectCompositionContext>("DirectX12 disposed");

                // Disposing fence
                FLogger.Log<DirectCompositionContext>("Disposing fence...");

                Fence?.Dispose();
                Fence = null!;
                FenceEvent?.Dispose();
                FenceEvent = null!;

                FLogger.Log<DirectCompositionContext>("Fence disposed");

                FLogger.Log<DirectCompositionContext>("Done disposing DXCC");
                FLogger.Log<DirectCompositionContext>("");
            }
            catch (Exception ex)
            {
                FLogger.Log<DirectCompositionContext>($"Exception during disposal: {ex}");
            }
        }

        public void Present(Action<ID3D12GraphicsCommandList>? drawAction = null, PresentFlags flags = PresentFlags.None)
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
                drawAction?.Invoke(CommandList ?? throw new NullReferenceException("CommandList null"));

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
    }
}