using System.Runtime.InteropServices;
using Vortice.DXGI;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using SkiaSharp;

namespace fenUI.Utils
{
    public class ScreenBuffer : IDisposable
    {
        private IDXGIFactory1? factory;
        private IDXGIAdapter1? adapter;
        private IDXGIOutput? output;
        private ID3D11Device? device;
        private ID3D11DeviceContext? context;
        private IDXGIOutputDuplication? duplication;
        private ID3D11Texture2D? stagingTexture;
        private uint screenWidth;
        private uint screenHeight;

        public bool Initialized { get; private set; }

        public SKImage? CachedCapture { get; private set; }

        public void Initialize(int screenIndex)
        {
            factory = DXGI.CreateDXGIFactory1<IDXGIFactory1>();
            factory.EnumAdapters1(0, out adapter);

            // Enumerate outputs to find the specified screen
            adapter.EnumOutputs((uint)screenIndex, out output);

            if (output == null)
                throw new ArgumentException($"Screen index {screenIndex} not found.");

            D3D11.D3D11CreateDevice(
                adapter,
                Vortice.Direct3D.DriverType.Unknown,
                DeviceCreationFlags.None,
                null,
                out device,
                out context
            );

            var output1 = output.QueryInterface<IDXGIOutput1>();
            duplication = output1.DuplicateOutput(device);

            var desc = duplication.Description;
            screenWidth = desc.ModeDescription.Width;
            screenHeight = desc.ModeDescription.Height;

            var texDesc = new Texture2DDescription
            {
                Width = screenWidth,
                Height = screenHeight,
                MipLevels = 1,
                ArraySize = 1,
                Format = desc.ModeDescription.Format, // Typically DXGI_FORMAT_B8G8R8A8_UNORM
                SampleDescription = new SampleDescription(1, 0),
                Usage = Vortice.Direct3D11.ResourceUsage.Staging,
                BindFlags = BindFlags.None,
                CPUAccessFlags = CpuAccessFlags.Read,
                MiscFlags = ResourceOptionFlags.None
            };

            stagingTexture = device.CreateTexture2D(texDesc);
            Initialized = true;
        }

        public unsafe SKImage? CaptureScreen()
        {
            if (duplication == null || stagingTexture == null || context == null)
                throw new InvalidOperationException("ScreenBuffer not initialized. Call Initialize() first.");

            try
            {
                duplication.AcquireNextFrame(100, out var frameInfo, out var desktopResource);

                if (frameInfo.LastPresentTime == 0)
                {
                    duplication.ReleaseFrame();
                    return null;
                }

                var texture = desktopResource.QueryInterface<ID3D11Texture2D>();
                context.CopyResource(stagingTexture, texture);
                var mapped = context.Map(stagingTexture, 0, MapMode.Read, Vortice.Direct3D11.MapFlags.None);

                int bytesPerPixel = 4;
                int stride = (int)screenWidth * bytesPerPixel;
                int bufferSize = stride * (int)screenHeight;

                byte[] data = new byte[bufferSize];
                Marshal.Copy(mapped.DataPointer, data, 0, bufferSize);

                context.Unmap(stagingTexture, 0);
                duplication.ReleaseFrame();

                var imageInfo = new SKImageInfo((int)screenWidth, (int)screenHeight, SKColorType.Bgra8888, SKAlphaType.Unpremul);
                using var bitmap = new SKBitmap(imageInfo);
                fixed (byte* ptr = data)
                {
                    bitmap.SetPixels((IntPtr)ptr);
                }

                var capture = SKImage.FromBitmap(bitmap);
                if (capture != null)
                    CachedCapture = capture;

                return CachedCapture;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error capturing screen: {ex.Message}");
                return null;
            }
        }

        public void Dispose()
        {
            stagingTexture?.Dispose();
            duplication?.Dispose();
            context?.Dispose();
            device?.Dispose();
            output?.Dispose();
            adapter?.Dispose();
            factory?.Dispose();
            Initialized = false;
        }
    }
}