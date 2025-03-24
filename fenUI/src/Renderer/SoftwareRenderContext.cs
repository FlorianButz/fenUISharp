using System.Runtime.InteropServices;
using OpenTK.Graphics.ES30;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SkiaSharp;

namespace FenUISharp
{
    public class SoftwareRenderContext : FRenderContext
    {
        private IntPtr _hdcMemory = IntPtr.Zero;    // Memory DC
        private IntPtr _hBitmap = IntPtr.Zero;      // Handle to our DIB section
        private IntPtr _ppvBits = IntPtr.Zero;      // Pointer to pixel bits

        public SoftwareRenderContext(Window windowRoot) : base(windowRoot)
        {
            Surface = CreateSurface();
        }

        protected override SKSurface CreateSurface()
        {
            Surface?.Dispose();

            if (_hdcMemory != IntPtr.Zero)
            {
                DeleteDC(_hdcMemory);
                _hdcMemory = IntPtr.Zero;
            }

            if (_hBitmap != IntPtr.Zero)
            {
                DeleteObject(_hBitmap);
                _hBitmap = IntPtr.Zero;
            }

            // Get a screen DC to create a compatible memory DC and DIB.
            IntPtr hdcScreen = GetDC(IntPtr.Zero);
            if (hdcScreen == IntPtr.Zero)
                Console.WriteLine("GetDC failed: " + Marshal.GetLastWin32Error());

            _hdcMemory = CreateCompatibleDC(hdcScreen);
            if (_hdcMemory == IntPtr.Zero)
                Console.WriteLine("CreateCompatibleDC failed: " + Marshal.GetLastWin32Error());

            BITMAPINFO bmi = new BITMAPINFO();
            bmi.bmiHeader.biSize = (uint)Marshal.SizeOf(typeof(BITMAPINFOHEADER));
            bmi.bmiHeader.biWidth = (int)WindowRoot.WindowSize.x;
            bmi.bmiHeader.biHeight = -(int)WindowRoot.WindowSize.y; // negative for top-down DIB
            bmi.bmiHeader.biPlanes = 1;
            bmi.bmiHeader.biBitCount = 32;
            bmi.bmiHeader.biCompression = 0; // BI_RGB (uncompressed)
            bmi.bmiHeader.biSizeImage = (uint)((int)WindowRoot.WindowSize.x * (int)WindowRoot.WindowSize.y * 4);
            bmi.bmiHeader.biXPelsPerMeter = 0;
            bmi.bmiHeader.biYPelsPerMeter = 0;
            bmi.bmiHeader.biClrUsed = 0;
            bmi.bmiHeader.biClrImportant = 0;

            _hBitmap = CreateDIBSection(hdcScreen, ref bmi, 0, out _ppvBits, IntPtr.Zero, 0);
            SelectObject(_hdcMemory, _hBitmap);

            ReleaseDC(IntPtr.Zero, hdcScreen);

            var imageInfo = new SKImageInfo((int)WindowRoot.WindowSize.x, (int)WindowRoot.WindowSize.y,
                                              SKColorType.Bgra8888, SKAlphaType.Premul);
            return SKSurface.Create(imageInfo, _ppvBits, imageInfo.RowBytes);
        }

        public override SKSurface BeginDraw()
        {
            Surface = CreateSurface();
            Surface.Canvas.Clear(SKColors.Transparent);
            
            return Surface;
        }

        public override SKSurface CreateAdditional()
        {
            return SKSurface.Create(new SKImageInfo((int)WindowRoot.WindowSize.x, (int)WindowRoot.WindowSize.y,
                                                      SKColorType.Bgra8888));
        }

        public override void EndDraw()
        {
            Surface.Canvas.Flush();
            Surface.Flush();

            UpdateWindow();
        }

        public override void UpdateWindow()
        {
            base.UpdateWindow();

            // Get the window's device context.
            IntPtr hdcWindow = GetDC(WindowRoot.hWnd);
            // BitBlt the memory DC onto the window DC.
            BitBlt(hdcWindow, 0, 0, (int)WindowRoot.WindowSize.x, (int)WindowRoot.WindowSize.y,
                   _hdcMemory, 0, 0, SRCCOPY);

            ReleaseDC(WindowRoot.hWnd, hdcWindow);
            DeleteObject(_hdcMemory);
        }

        public override void Dispose()
        {
            base.Dispose();

            if (_hdcMemory != IntPtr.Zero)
            {
                DeleteDC(_hdcMemory);
                _hdcMemory = IntPtr.Zero;
            }

            if (_hBitmap != IntPtr.Zero)
            {
                DeleteObject(_hBitmap);
                _hBitmap = IntPtr.Zero;
            }
        }

        public override void OnResize(Vector2 newSize)
        {
            CreateSurface();
        }

        public override void OnWindowPropertyChanged()
        {
            CreateSurface();
        }
    }
}