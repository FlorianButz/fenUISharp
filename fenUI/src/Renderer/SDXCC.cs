using System.Runtime.InteropServices;
using FenUISharp.Logging;
using SkiaSharp;
using Vortice.Direct3D12;

namespace FenUISharp
{
    public static class SkiaDirectCompositionContext
    {
        public static SKSamplingOptions SamplingOptions => new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);

        public static GRContext CreateGrContext(ID3D12CommandQueue commandQueue)
        {
            var dxcc = DirectCompositionContext.Instance;
            var adapter = dxcc.Adapter ?? throw new InvalidOperationException("Adapter is null");

            FLogger.Log("Creating D3D Backend Context...");
            var backendContext = new GRD3DBackendContext()
            {
                Device = dxcc.Device.NativePointer,
                Adapter = adapter.NativePointer,
                ProtectedContext = false,
                Queue = commandQueue.NativePointer
            };

            FLogger.Log("Creating Skia D3D GRContext...");
            var grContext = GRContext.CreateDirect3D(backendContext);
            if (grContext == null)
                throw new Exception("Failed to create Skia D3D GRContext");

            return grContext;
        }

        public static bool IsDeviceValid()
        {
            var dxcc = DirectCompositionContext.Instance;
            if (dxcc.Device == null)
                return false;
            return dxcc.Device.DeviceRemovedReason == 0;
        }

        public static void DisposeGrContextSafely(GRContext? grContext, GRD3DBackendContext? backendContext)
        {
            if (grContext == null) return;

            FLogger.Log("Disposing GRContext safely...");

            try
            {
                var devPtr = backendContext?.Device ?? IntPtr.Zero;
                var adaptPtr = backendContext?.Adapter ?? IntPtr.Zero;

                var flags = System.Reflection.BindingFlags.NonPublic |
                            System.Reflection.BindingFlags.Instance;
                var type = typeof(SkiaSharp.SKNativeObject);
                var handleField = type.GetField("_handle", flags)
                               ?? type.GetField("handle", flags);
                var ownsHandleField = type.GetField("_ownsHandle", flags);

                if (handleField != null)
                {
                    handleField.SetValue(grContext, IntPtr.Zero);
                    ownsHandleField?.SetValue(grContext, false);
                }

                grContext.Dispose();

                foreach (var ptr in new[] { devPtr, adaptPtr })
                {
                    if (ptr != IntPtr.Zero)
                    {
                        try { Marshal.Release(ptr); } catch { }
                    }
                }
            }
            catch
            {
                FLogger.Error("Manual GR release failed.");
            }
        }
    }
}
