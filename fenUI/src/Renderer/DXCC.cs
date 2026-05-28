
using System;
using FenUISharp.Logging;
using Vortice.Direct3D11;
using Vortice.Direct3D12;
using Vortice.Direct3D;
using Vortice.DirectComposition;
using Vortice.DXGI;
using SharpGen.Runtime;
using Vortice.Direct3D12.Debug;

namespace FenUISharp
{
    public class DirectCompositionContext : IDisposable
    {
        private static DirectCompositionContext? _instance;
        private static readonly object _initLock = new();
        public static DirectCompositionContext Instance => _instance ?? throw new InvalidOperationException("DirectCompositionContext not initialized. Call Initialize() first.");
        public static bool IsInitialized => _instance != null;

        public static void Initialize(Format colorFormat = Format.B8G8R8A8_UNorm, Format depthStencilFormat = Format.D32_Float)
        {
            if (_instance != null) return;
            lock (_initLock)
            {
                if (_instance != null) return;
                _instance = new DirectCompositionContext(colorFormat, depthStencilFormat);
            }
        }

        public IDXGIFactory2 Factory { get; private set; }
        public ID3D12Device Device { get; private set; }
        public FeatureLevel FeatureLevel { get; private set; }
        public Format ColorFormat { get; }
        public ColorSpaceType ColorSpace { get; set; } = ColorSpaceType.RgbFullG22NoneP709;
        public uint BufferCount { get; } = 2;

        internal IDXGIAdapter1? Adapter => _cachedAdapter;

        private IDXGIAdapter1? _cachedAdapter;
        private ID3D11Device? _d3d11Device;
        private ID3D11DeviceContext? _d3d11Context;
        private bool _disposed;

        private DirectCompositionContext(Format colorFormat, Format depthStencilFormat)
        {
            try
            {
                FLogger.Log<DirectCompositionContext>("Starting Global DirectCompositionContext initialization...");

                ColorFormat = colorFormat;

                if (FenUI.debugEnabled)
                {
                    if (D3D12.D3D12GetDebugInterface<ID3D12Debug>(out var debug).Success)
                        debug?.EnableDebugLayer();
                }

                FLogger.Log<DirectCompositionContext>("Creating DXGI Factory...");
                Factory = DXGI.CreateDXGIFactory1<IDXGIFactory2>();

                FLogger.Log<DirectCompositionContext>("Getting hardware adapter...");
                _cachedAdapter = GetHardwareAdapter();
                FLogger.Log<DirectCompositionContext>($"Hardware adapter: {_cachedAdapter.Description1.Description}");

                FLogger.Log<DirectCompositionContext>("Creating D3D12 Device...");
                FeatureLevel = FeatureLevel.Level_11_0;

                ID3D12Device? device = null;
                var attempts = 0;
                const int maxAttempts = 3;

                while (device == null && attempts < maxAttempts)
                {
                    try
                    {
                        device = D3D12.D3D12CreateDevice<ID3D12Device>(_cachedAdapter, FeatureLevel);
                    }
                    catch (SharpGenException ex) when (ex.ResultCode == Vortice.DXGI.ResultCode.InvalidCall)
                    {
                        FLogger.Log<DirectCompositionContext>($"Device creation attempt {attempts + 1} failed, retrying...");
                        System.Threading.Thread.Sleep(200 * (1 << attempts));
                        attempts++;
                    }
                }

                if (device == null)
                {
                    FLogger.Log<DirectCompositionContext>("Hardware adapter failed, trying WARP software adapter...");
                    try
                    {
                        var warpAdapter = GetWarpAdapter();
                        if (warpAdapter != null)
                        {
                            device = D3D12.D3D12CreateDevice<ID3D12Device>(warpAdapter, FeatureLevel);
                            if (device != null)
                            {
                                _cachedAdapter = warpAdapter;
                                FLogger.Log<DirectCompositionContext>("WARP adapter device created successfully");
                            }
                        }
                    }
                    catch (Exception warpEx)
                    {
                        FLogger.Log<DirectCompositionContext>($"WARP adapter also failed: {warpEx.Message}");
                    }
                }

                if (device == null)
                    throw new InvalidOperationException("Failed to create D3D12 device after multiple attempts");

                Device = device;
                FLogger.Log<DirectCompositionContext>($"D3D12 Device created: {Device.NativePointer}");

                CreateD3D11Device();

                FLogger.Log<DirectCompositionContext>("Global DirectCompositionContext initialization completed!");
            }
            catch (Exception ex)
            {
                FLogger.Log<DirectCompositionContext>($"Error in DirectCompositionContext constructor: {ex}");
                try { Dispose(); } catch { }
                throw;
            }
        }

        private void CreateD3D11Device()
        {
            FLogger.Log<DirectCompositionContext>("Creating D3D11 device for DirectComposition interop...");

            if (_cachedAdapter == null)
                throw new InvalidOperationException("Cached adapter is null");

            var creationFlags = DeviceCreationFlags.BgraSupport;

            if (FenUI.debugEnabled)
                creationFlags |= DeviceCreationFlags.Debug;

            var featureLevels = new[] {
                Vortice.Direct3D.FeatureLevel.Level_11_0,
                Vortice.Direct3D.FeatureLevel.Level_10_1,
                Vortice.Direct3D.FeatureLevel.Level_10_0
            };

            var attempts = 0;
            const int maxAttempts = 3;

            while (_d3d11Device == null && attempts < maxAttempts)
            {
                try
                {
                    var hr = D3D11.D3D11CreateDevice(
                        _cachedAdapter,
                        Vortice.Direct3D.DriverType.Unknown,
                        creationFlags,
                        featureLevels,
                        out _d3d11Device,
                        out var featureLevel,
                        out _d3d11Context);

                    if (hr.Success && _d3d11Device != null)
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

            if (_d3d11Device == null)
                throw new InvalidOperationException("Failed to create D3D11 device after multiple attempts");
        }

        public IDCompositionDevice CreateDCompDevice()
        {
            if (_d3d11Device == null)
                throw new InvalidOperationException("D3D11 device is null");

            using var dxgiDevice = _d3d11Device.QueryInterface<IDXGIDevice>();
            if (dxgiDevice == null)
                throw new InvalidOperationException("Failed to get IDXGIDevice from D3D11 device");

            FLogger.Log<DirectCompositionContext>("Creating DirectComposition device from D3D11 DXGI device...");

            var hr = DComp.DCompositionCreateDevice3(dxgiDevice, out IDCompositionDevice? dcompDevice);
            if (hr.Failure || dcompDevice == null)
                throw new InvalidOperationException($"Failed to create DirectComposition device. HRESULT: {hr}");

            return dcompDevice;
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
                            factory6.Dispose();
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

        private IDXGIAdapter1? GetWarpAdapter()
        {
            try
            {
                if (Factory.QueryInterfaceOrNull<IDXGIFactory4>() is IDXGIFactory4 factory4)
                {
                    var hr = factory4.EnumWarpAdapter(out IDXGIAdapter1? warpAdapter);
                    factory4.Dispose();
                    if (hr.Success && warpAdapter != null)
                    {
                        FLogger.Log<DirectCompositionContext>($"Found WARP adapter: {warpAdapter.Description1.Description}");
                        return warpAdapter;
                    }
                }
            }
            catch (Exception ex)
            {
                FLogger.Log<DirectCompositionContext>($"Failed to get WARP adapter: {ex.Message}");
            }
            return null;
        }

        internal void UpdateColorSpace()
        {
            try
            {
                if (!Factory.IsCurrent)
                {
                    Factory.Dispose();
                    Factory = DXGI.CreateDXGIFactory1<IDXGIFactory2>();
                }

                ColorSpace = ColorSpaceType.RgbFullG22NoneP709;
            }
            catch (Exception ex)
            {
                FLogger.Log<DirectCompositionContext>($"Warning: UpdateColorSpace failed: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                FLogger.Log<DirectCompositionContext>("Disposing Global DirectCompositionContext...");

                _d3d11Context?.ClearState();
                _d3d11Context?.Flush();

                _d3d11Context?.Dispose();
                _d3d11Context = null!;
                _d3d11Device?.Dispose();
                _d3d11Device = null!;

                Device?.Dispose();

                _cachedAdapter?.Dispose();
                _cachedAdapter = null!;

                Factory?.Dispose();
                Factory = null!;

                _instance = null;
                FLogger.Log<DirectCompositionContext>("Global DirectCompositionContext disposed successfully");
            }
            catch (Exception ex)
            {
                FLogger.Log<DirectCompositionContext>($"Exception during global disposal: {ex}");
            }
        }
    }
}
