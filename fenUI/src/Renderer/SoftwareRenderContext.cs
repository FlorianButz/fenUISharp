using System.Runtime.InteropServices;
using FenUISharp.Mathematics;
using OpenTK.Graphics.ES30;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SkiaSharp;

namespace FenUISharp
{
    public class SoftwareRenderContext : FRenderContext
    {
        public SoftwareRenderContext(Window windowRoot) : base(windowRoot)
        {
            // Surface = CreateSurface();
        }

        protected override SKSurface CreateSurface()
        {
            Surface?.Dispose();
            DisposeHDC();

            // Get a screen DC to create a compatible memory DC and DIB.
            IntPtr hdcScreen = GetDC(IntPtr.Zero);
            if (hdcScreen == IntPtr.Zero)
                Console.WriteLine("GetDC failed: " + Marshal.GetLastWin32Error());

            _hdcMemory = CreateCompatibleDC(hdcScreen);
            if (_hdcMemory == IntPtr.Zero)
                Console.WriteLine("CreateCompatibleDC failed: " + Marshal.GetLastWin32Error());

            int Width = RMath.Clamp((int)WindowRoot.WindowSize.x, 1, int.MaxValue);
            int Height = RMath.Clamp((int)WindowRoot.WindowSize.y, 1, int.MaxValue);

            BITMAPINFO bmi = new BITMAPINFO();
            bmi.bmiHeader.biSize = (uint)Marshal.SizeOf(typeof(BITMAPINFOHEADER));
            bmi.bmiHeader.biWidth = (int)Width;
            bmi.bmiHeader.biHeight = -(int)Height; // negative for top-down DIB
            bmi.bmiHeader.biPlanes = 1;
            bmi.bmiHeader.biBitCount = 32;
            bmi.bmiHeader.biCompression = 0; // BI_RGB (uncompressed)
            bmi.bmiHeader.biSizeImage = (uint)(Width * Height * 4);
            bmi.bmiHeader.biXPelsPerMeter = 0;
            bmi.bmiHeader.biYPelsPerMeter = 0;
            bmi.bmiHeader.biClrUsed = 0;
            bmi.bmiHeader.biClrImportant = 0;
            _hBitmap = CreateDIBSection(
                _hdcMemory,
                ref bmi,
                0,
                out _ppvBits,
                IntPtr.Zero,
                0
            );
            SelectObject(_hdcMemory, _hBitmap);

            ReleaseDC(IntPtr.Zero, hdcScreen);

            var imageInfo = new SKImageInfo(Width, Height, SKColorType.Bgra8888, SKAlphaType.Premul);
            return SKSurface.Create(imageInfo, _ppvBits, imageInfo.RowBytes);
        }

        public override SKSurface BeginDraw()
        {
            if (Surface == null || _surfaceDirty)
            {
                Surface = CreateSurface();
                _surfaceDirty = false;
            }

            // Surface.Canvas.Clear(new SKColor(0, 0, 0, 0));
            return Surface;
        }

        public override SKSurface CreateAdditional(SKImageInfo info)
        {
            return SKSurface.Create(info);
        }

        public override void EndDraw()
        {
            UpdateWindow();
        }

        bool _surfaceDirty = false;

        public override void UpdateWindow()
        {
            base.UpdateWindow();

            int Width = RMath.Clamp((int)WindowRoot.WindowSize.x, 1, int.MaxValue);
            int Height = RMath.Clamp((int)WindowRoot.WindowSize.y, 1, int.MaxValue);

            // Get the window's device context.
            IntPtr hdcWindow = GetDC(WindowRoot.hWnd);
            
            // BitBlt the memory DC onto the window DC.
            BitBlt(hdcWindow, 0, 0, Width, Height,
                   _hdcMemory, 0, 0, SRCCOPY);

            ReleaseDC(WindowRoot.hWnd, hdcWindow);
        }

        public override void Dispose()
        {
            base.Dispose();
            DisposeHDC();
        }

        void DisposeHDC()
        {
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
        }

        public override void OnWindowPropertyChanged()
        {
            _surfaceDirty = true;
        }

        public override void OnEndResize()
        {
            _surfaceDirty = true;
        }
    }
}