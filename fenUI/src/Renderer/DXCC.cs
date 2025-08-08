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
        public IDCompositionDevice DCompDevice { get; private set; }
        public IDCompositionTarget DCompTarget { get; private set; }
        public IDCompositionVisual RootVisual { get; private set; }

        // DirectX objects
        public IDXGISwapChain3? SwapChain { get; private set; }
        public ID3D12CommandQueue? CommandQueue { get; private set; }
        public ID3D12GraphicsCommandList CommandList { get; private set; }
        public ID3D12CommandAllocator CommandAllocator { get; private set; }

        public ColorSpaceType ColorSpace = ColorSpaceType.RgbFullG22NoneP709;
        public uint BufferCount { get; } = 2;

        public DirectCompositionContext(IntPtr windowHandle, int sx, int sy, Format colorFormat = Format.B8G8R8A8_UNorm, Format depthStencilFormat = Format.D32_Float)
        {
            try
            {
                FLogger.Log("Starting DirectCompositionContext initialization...");

                this.SizeX = sx;
                this.SizeY = sy;
                this.hWnd = windowHandle;
                this.ColorFormat = colorFormat;
                this.DepthStencilFormat = depthStencilFormat;

                FLogger.Log($"Window Handle: {windowHandle}, Size: {sx}x{sy}");

                // Step 1: Create DXGI Factory
                FLogger.Log("Creating DXGI Factory...");
                Factory = DXGI.CreateDXGIFactory1<IDXGIFactory2>();
                FLogger.Log("DXGI Factory created successfully");

                // Step 2: Get hardware adapter
                FLogger.Log("Getting hardware adapter...");
                using var adapter = GetHardwareAdapter();
                FLogger.Log($"Hardware adapter: {adapter.Description1.Description}");

                // Step 3: Create D3D12 Device
                FLogger.Log("Creating D3D12 Device...");
                FeatureLevel = FeatureLevel.Level_11_0;
                Device = D3D12CreateDevice<ID3D12Device>(adapter, FeatureLevel);
                FLogger.Log($"D3D12 Device created: {Device.NativePointer}");

                // Step 4: Create Command Queue
                FLogger.Log("Creating command queue...");
                var commandQueueDesc = new CommandQueueDescription(CommandListType.Direct);
                CommandQueue = Device.CreateCommandQueue(commandQueueDesc);
                FLogger.Log("Command queue created successfully");

                // Step 5: Create DirectComposition (this is likely where it's failing)
                FLogger.Log("Creating DirectComposition device...");
                CreateDirectCompositionDevice();
                FLogger.Log("DirectComposition device created successfully");

                // Step 6: Create SwapChain
                FLogger.Log("Creating swap chain...");
                CreateSwapchain();
                FLogger.Log("Swap chain created successfully");

                // Step 7: Create Command Objects
                FLogger.Log("Creating command objects...");
                CreateCommandObjects();
                FLogger.Log("Command objects created successfully");

                FLogger.Log("DirectCompositionContext initialization completed!");
            }
            catch (Exception ex)
            {
                FLogger.Log($"Error in DirectCompositionContext constructor: {ex}");

                // Clean up any partially created objects
                try { Dispose(); } catch { }

                throw;
            }
        }

        private void CreateDirectCompositionDevice()
        {
            FLogger.Log("Creating D3D11 device for DirectComposition interop...");

            using var adapter = GetHardwareAdapter();

            // Create D3D11 device for interop
            var creationFlags = Vortice.Direct3D11.DeviceCreationFlags.BgraSupport;

            // Optional: add debug flag if in debug mode
#if DEBUG
            creationFlags |= Vortice.Direct3D11.DeviceCreationFlags.Debug;
#endif

            Vortice.Direct3D11.ID3D11Device d3d11Device;
            Vortice.Direct3D11.ID3D11DeviceContext d3d11Context;

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

            FLogger.Log($"D3D11 device created with feature level: {featureLevel}");

            using var dxgiDevice = d3d11Device.QueryInterface<IDXGIDevice>();

            if (dxgiDevice == null)
                throw new InvalidOperationException("Failed to get IDXGIDevice from D3D11 device");

            FLogger.Log("Creating DirectComposition device from D3D11 DXGI device...");

            hr = DComp.DCompositionCreateDevice(dxgiDevice, out IDCompositionDevice dcompDevice);
            if (hr.Failure || dcompDevice == null)
                throw new InvalidOperationException($"Failed to create DirectComposition device. HRESULT: {hr}");

            DCompDevice = dcompDevice;

            FLogger.Log("Creating composition target for window...");
            hr = DCompDevice.CreateTargetForHwnd(hWnd, true, out IDCompositionTarget target);
            if (hr.Failure || target == null)
                throw new InvalidOperationException($"Failed to create DirectComposition target. HRESULT: {hr}");

            DCompTarget = target;

            FLogger.Log("Creating root visual...");
            hr = DCompDevice.CreateVisual(out IDCompositionVisual visual);
            if (hr.Failure || visual == null)
                throw new InvalidOperationException($"Failed to create DirectComposition visual. HRESULT: {hr}");

            RootVisual = visual;

            FLogger.Log("Setting root visual on target...");
            DCompTarget.SetRoot(RootVisual);

            // Dispose D3D11 context & device when no longer needed (you might want to keep them as class fields if needed for interop)
            d3d11Context.Dispose();
            d3d11Device.Dispose();
        }

        private void CreateSwapchain()
        {
            try
            {
                SwapChain?.Dispose();

                var width = (uint)SizeX;
                var height = (uint)SizeY;

                FLogger.Log($"Creating swap chain with size {width}x{height}");

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

                FLogger.Log("Creating swap chain for composition...");
                var tempSwap = Factory.CreateSwapChainForComposition(CommandQueue, swapChainDesc);
                SwapChain = tempSwap.QueryInterface<IDXGISwapChain3>();

                FLogger.Log("Setting swap chain content on root visual...");
                RootVisual.SetContent(SwapChain);
                DCompDevice.Commit();

                UpdateColorSpace();
            }
            catch (Exception ex)
            {
                FLogger.Log($"Exception in CreateSwapchain: {ex}");
                throw;
            }
        }

        private void CreateCommandObjects()
        {
            try
            {
                FLogger.Log("Creating command allocator...");
                CommandAllocator = Device.CreateCommandAllocator(CommandListType.Direct);

                FLogger.Log("Creating command list...");
                CommandList = Device.CreateCommandList<ID3D12GraphicsCommandList>(
                    0, // nodeMask
                    CommandListType.Direct,
                    CommandAllocator,
                    null // no initial PSO
                );

                // Close the command list initially
                CommandList.Close();
                FLogger.Log("Command objects created successfully");
            }
            catch (Exception ex)
            {
                FLogger.Log($"Exception in CreateCommandObjects: {ex}");
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
                FLogger.Log($"Warning: UpdateColorSpace failed: {ex.Message}");
                // Non-critical, continue
            }
        }

        public IDXGIAdapter1 GetHardwareAdapter()
        {
            try
            {
                FLogger.Log("Enumerating adapters...");

                if (Factory.QueryInterfaceOrNull<IDXGIFactory6>() is IDXGIFactory6 factory6)
                {
                    FLogger.Log("Using IDXGIFactory6 for adapter enumeration");
                    for (uint adapterIndex = 0;
                        factory6.EnumAdapterByGpuPreference(adapterIndex, GpuPreference.HighPerformance, out IDXGIAdapter1? adapter).Success;
                        adapterIndex++)
                    {
                        if (adapter != null && (adapter.Description1.Flags & AdapterFlags.Software) == 0)
                        {
                            FLogger.Log($"Found hardware adapter: {adapter.Description1.Description}");
                            return adapter;
                        }
                        adapter?.Dispose();
                    }

                    factory6.Dispose();
                }

                FLogger.Log("Using IDXGIFactory2 for adapter enumeration");
                for (uint adapterIndex = 0;
                    Factory.EnumAdapters1(adapterIndex, out IDXGIAdapter1? adapter).Success;
                    adapterIndex++)
                {
                    if (adapter != null && (adapter.Description1.Flags & AdapterFlags.Software) == 0)
                    {
                        FLogger.Log($"Found hardware adapter: {adapter.Description1.Description}");
                        return adapter;
                    }
                    adapter?.Dispose();
                }

                throw new InvalidOperationException("No suitable D3D12 adapter found.");
            }
            catch (Exception ex)
            {
                FLogger.Log($"Exception in GetHardwareAdapter: {ex}");
                throw;
            }
        }

        public void Resize(int newWidth, int newHeight)
        {
            if (newWidth == SizeX && newHeight == SizeY)
                return;

            SizeX = newWidth;
            SizeY = newHeight;

            // Wait for GPU to finish
            WaitForGpu();

            // Resize swap chain
            SwapChain?.ResizeBuffers(
                BufferCount,
                (uint)newWidth,
                (uint)newHeight,
                ColorFormat,
                SwapChainFlags.None
            );
        }

        private void WaitForGpu()
        {
            // Simple synchronization - in production you'd want a fence
            CommandQueue?.Signal(null, 0);
        }

        public void Dispose()
        {
            try
            {
                FLogger.Log("Disposing DirectCompositionContext...");

                WaitForGpu();

                RootVisual?.Dispose();
                DCompTarget?.Dispose();
                DCompDevice?.Dispose();

                CommandList?.Dispose();
                CommandAllocator?.Dispose();
                CommandQueue?.Dispose();

                SwapChain?.Dispose();
                Device?.Dispose();
                Factory?.Dispose();

                FLogger.Log("DirectCompositionContext disposed");
            }
            catch (Exception ex)
            {
                FLogger.Log($"Exception during disposal: {ex}");
            }
        }

        public void Present(Action<ID3D12GraphicsCommandList>? drawAction = null, PresentFlags flags = PresentFlags.None)
        {
            try
            {
                // Reset command list
                CommandAllocator.Reset();
                CommandList.Reset(CommandAllocator, null);

                // Get current back buffer
                ID3D12Resource backBuffer = SwapChain.GetBuffer<ID3D12Resource>(SwapChain.CurrentBackBufferIndex);

                // Transition to render target state
                CommandList.ResourceBarrierTransition(
                    backBuffer,
                    ResourceStates.Present,
                    ResourceStates.RenderTarget
                );

                // Execute custom drawing commands
                drawAction?.Invoke(CommandList);

                // Transition back to present state
                CommandList.ResourceBarrierTransition(
                    backBuffer,
                    ResourceStates.RenderTarget,
                    ResourceStates.Present
                );

                // Execute command list
                CommandList.Close();
                CommandQueue.ExecuteCommandLists(new ID3D12CommandList[] { CommandList });

                // Present
                SwapChain.Present(1, flags);

                // Commit composition
                DCompDevice.Commit();

                // Clean up
                backBuffer.Dispose();
            }
            catch (Exception ex)
            {
                FLogger.Log($"Exception in Present: {ex}");
                throw;
            }
        }
    }
}