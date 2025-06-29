using System.Runtime.InteropServices;
using FenUISharp.Mathematics;
using FenUISharp.Objects;
using SkiaSharp;

namespace FenUISharp
{

    public abstract class FRenderContext : IDisposable
    {
        public Window WindowRoot { get; set; }
        public SKSurface? Surface { get; protected set; }

        public bool HasAlphaChannel { get; set; } = false;

        public SKSamplingOptions SamplingOptions { get; protected set; } = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);

        public IntPtr _hdcMemory { get; protected set; } = IntPtr.Zero;    // Memory DC
        public IntPtr _hBitmap { get; protected set; } = IntPtr.Zero;      // Handle to our DIB section
        protected IntPtr _ppvBits = IntPtr.Zero;      // Pointer to pixel bits

        protected bool _recreateSurfaceFlag = false;

        public void RecreateSurface() => _recreateSurfaceFlag = true;

        public FRenderContext(Window windowRoot)
        {
            WindowRoot = windowRoot;
        }

        public abstract SKSurface BeginDraw();
        public abstract void EndDraw();
        public abstract SKSurface CreateAdditional(SKImageInfo imageInfo);
        protected abstract SKSurface CreateSurface();

        public abstract void OnResize(Vector2 newSize);
        public abstract void OnEndResize();
        public abstract void OnWindowPropertyChanged(); // Used for refreshrate change, etc..

        public virtual void UpdateWindow()
        {
            WindowRoot.UpdateWindowFrame();
        }

        public SKImage? CaptureWindowRegion(SKRect region, float quality = 0.5f)
        {
            if (Surface == null)
                return null;

            Compositor.Dump(Surface.Snapshot(), "rcontext_buffer_surf_whole");

            var snapshot = Surface.Snapshot(new SKRectI((int)region.Left, (int)region.Top, (int)region.Right, (int)region.Bottom));
            var scaled = RMath.CreateLowResImage(snapshot, RMath.Clamp(quality, 0.01f, 1f), WindowRoot.RenderContext.SamplingOptions);
            snapshot?.Dispose();

            Compositor.Dump(scaled, "rcontext_buffer_cropped_scaled");

            return scaled;
        }

        public virtual void Dispose()
        {
            Surface?.Dispose();
        }

        public static SKImage HBitmapToSKImage(IntPtr hBitmap)
        {
            // Retrieve the BITMAP information from the HBITMAP
            BITMAP bmp;
            int result = GetObject(hBitmap, Marshal.SizeOf(typeof(BITMAP)), out bmp);
            if (result == 0)
                throw new Exception("Failed to get bitmap info.");

            // Set up the BITMAPINFO header for a top-down DIB (negative height)
            BITMAPINFO bmi = new BITMAPINFO();
            bmi.bmiHeader.biSize = (uint)Marshal.SizeOf(typeof(BITMAPINFOHEADER));
            bmi.bmiHeader.biWidth = bmp.bmWidth;
            bmi.bmiHeader.biHeight = -bmp.bmHeight; // Negative for top-down
            bmi.bmiHeader.biPlanes = 1;
            bmi.bmiHeader.biBitCount = (ushort)bmp.bmBitsPixel;
            bmi.bmiHeader.biCompression = 0; // BI_RGB (no compression)
            bmi.bmiHeader.biSizeImage = (uint)(bmp.bmWidthBytes * bmp.bmHeight);

            // Allocate a buffer for the pixel data.
            int imageSize = bmp.bmWidthBytes * bmp.bmHeight;
            byte[] pixelData = new byte[imageSize];

            // Get a device context to use with GetDIBits.
            IntPtr hdc = GetDC(IntPtr.Zero);
            try
            {
                // Retrieve the pixel data from the HBITMAP
                int scanLines = GetDIBits(hdc, hBitmap, 0, (uint)bmp.bmHeight, pixelData, ref bmi, 0);
                if (scanLines == 0)
                    throw new Exception("GetDIBits failed.");
            }
            finally
            {
                ReleaseDC(IntPtr.Zero, hdc);
            }

            // Create a SkiaSharp image info.
            // Adjust SKColorType as needed; here we assume 32bpp BGRA.
            var imageInfo = new SKImageInfo(bmp.bmWidth, bmp.bmHeight, SKColorType.Bgra8888, SKAlphaType.Premul);

            // Create an SKImage from the pixel data.
            unsafe
            {
                fixed (byte* p = pixelData)
                {
                    // Note: bmp.bmWidthBytes is used as the row bytes parameter.
                    var image = SKImage.FromPixels(imageInfo, (IntPtr)p, bmp.bmWidthBytes);
                    return image;
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BITMAP
        {
            public int bmType;
            public int bmWidth;
            public int bmHeight;
            public int bmWidthBytes;
            public ushort bmPlanes;
            public ushort bmBitsPixel;
            public IntPtr bmBits;
        }

        protected const uint PFD_DRAW_TO_WINDOW = 0x00000004;
        protected const uint PFD_SUPPORT_OPENGL = 0x00000020;
        protected const uint PFD_DOUBLEBUFFER = 0x00000001;
        protected const byte PFD_TYPE_RGBA = 0;
        protected const int SRCCOPY = 0x00CC0020;

        [DllImport("gdi32.dll", SetLastError = true)]
        static extern int GetObject(IntPtr h, int nCount, out BITMAP lpObject);

        [DllImport("gdi32.dll", SetLastError = true)]
        static extern int GetDIBits(IntPtr hdc, IntPtr hbmp, uint start, uint cLines,
            [Out] byte[] lpvBits, ref BITMAPINFO lpbi, uint usage);

        [DllImport("gdi32.dll", SetLastError = true)]
        protected static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        protected static extern bool SetPixelFormat(IntPtr hdc, int iPixelFormat, ref PIXELFORMATDESCRIPTOR pfd);

        [DllImport("opengl32.dll")]
        protected static extern IntPtr wglCreateContext(IntPtr hdc);

        [DllImport("opengl32.dll")]
        protected static extern bool wglMakeCurrent(IntPtr hdc, IntPtr hglrc);

        [DllImport("opengl32.dll")]
        protected static extern bool wglDeleteContext(IntPtr hglrc);

        [DllImport("gdi32.dll")]
        protected static extern int ChoosePixelFormat(IntPtr hdc, ref PIXELFORMATDESCRIPTOR pfd);

        [DllImport("user32.dll")]
        protected static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        protected static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll", SetLastError = true)]
        protected static extern IntPtr CreateDIBSection(IntPtr hdc, [In] ref BITMAPINFO pbmi,
             uint iUsage, out IntPtr ppvBits, IntPtr hSection, uint dwOffset);

        [DllImport("gdi32.dll", SetLastError = true)]
        protected static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll", SetLastError = true)]
        protected static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll", SetLastError = true)]
        protected static extern bool DeleteObject(IntPtr hObject);

        [DllImport("user32.dll")]
        protected static extern IntPtr BeginPaint(IntPtr hWnd, out PAINTSTRUCT lpPaint);

        [DllImport("user32.dll")]
        protected static extern bool EndPaint(IntPtr hWnd, [In] ref PAINTSTRUCT lpPaint);

        [DllImport("gdi32.dll")]
        protected static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight,
            IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

        [DllImport("dwmapi.dll")]
        public static extern void DwmFlush();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst,
            ref POINT pptDst, ref SIZE psize, IntPtr hdcSrc,
            ref POINT pptSrc, uint crKey, ref BLENDFUNCTION pblend, uint dwFlags);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PIXELFORMATDESCRIPTOR
    {
        public ushort nSize;
        public ushort nVersion;
        public uint dwFlags;
        public byte iPixelType;
        public byte cColorBits;
        public byte cRedBits;
        public byte cRedShift;
        public byte cGreenBits;
        public byte cGreenShift;
        public byte cBlueBits;
        public byte cBlueShift;
        public byte cAlphaBits;
        public byte cAlphaShift;
        public byte cAccumBits;
        public byte cAccumRedBits;
        public byte cAccumGreenBits;
        public byte cAccumBlueBits;
        public byte cAccumAlphaBits;
        public byte cDepthBits;
        public byte cStencilBits;
        public byte cAuxBuffers;
        public sbyte iLayerType;
        public byte bReserved;
        public uint dwLayerMask;
        public uint dwVisibleMask;
        public uint dwDamageMask;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BITMAPINFOHEADER
    {
        public uint biSize;
        public int biWidth;
        public int biHeight;
        public ushort biPlanes;
        public ushort biBitCount;
        public uint biCompression;
        public uint biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public uint biClrUsed;
        public uint biClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RGBQUAD
    {
        public byte rgbBlue;
        public byte rgbGreen;
        public byte rgbRed;
        public byte rgbReserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BITMAPINFO
    {
        public BITMAPINFOHEADER bmiHeader;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public RGBQUAD[] bmiColors;
    }

    public enum LayeredWindowFlags : uint
    {
        ULW_ALPHA = 0x00000002
    }

}