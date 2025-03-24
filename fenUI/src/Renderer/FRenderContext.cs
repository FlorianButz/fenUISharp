using System.Runtime.InteropServices;
using SkiaSharp;

namespace FenUISharp
{

    public abstract class FRenderContext : IDisposable
    {

        public Window WindowRoot { get; set; }
        public SKSurface Surface { get; protected set; }

        public bool HasAlphaChannel { get; set; } = false;

        public SKSamplingOptions SamplingOptions { get; protected set; } = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);

        public FRenderContext(Window windowRoot)
        {
            WindowRoot = windowRoot;
        }

        public abstract SKSurface BeginDraw();
        public abstract void EndDraw();
        public abstract SKSurface CreateAdditional();
        protected abstract SKSurface CreateSurface();

        public abstract void OnResize(Vector2 newSize);
        public abstract void OnWindowPropertyChanged(); // Used for refreshrate change, etc..

        public virtual void UpdateWindow()
        {
            WindowRoot.UpdateWindowFrame();
        }

        public SKImage? CaptureWindowRegion(SKRect region, float quality = 0.5f)
        {
            if (Surface == null)
                return null;

            var snapshot = Surface.Snapshot(new SKRectI((int)region.Left, (int)region.Top, (int)region.Right, (int)region.Bottom));
            var scaled = RMath.CreateLowResImage(snapshot, RMath.Clamp(quality, 0.05f, 1f), WindowRoot.RenderContext.SamplingOptions);
            snapshot.Dispose();

            return scaled;
        }

        public virtual void Dispose()
        {
            Surface?.Dispose();
        }

        protected const uint PFD_DRAW_TO_WINDOW = 0x00000004;
        protected const uint PFD_SUPPORT_OPENGL = 0x00000020;
        protected const uint PFD_DOUBLEBUFFER = 0x00000001;
        protected const byte PFD_TYPE_RGBA = 0;
        protected const int SRCCOPY = 0x00CC0020;

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