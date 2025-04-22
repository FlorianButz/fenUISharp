using System.Runtime.InteropServices;
using FenUISharp.Mathematics;
using SkiaSharp;

namespace FenUISharp.WinFeatures
{
    public class DesktopCapture
    {
        public static DesktopCapture? instance;

        public SKImage? previousCapture;
        public SKImage? lastCapture;

        public Action<SKImage>? onCapture;
        public float CaptureQuality { get; set; } = 0.01f;
        public int CaptureInterval { get; set; } = 2500;

        public int timeSinceLastCapture = 0;

        public DesktopCapture()
        {
            instance = this;
        }

        static IntPtr CaptureDesktop(out int width, out int height, out IntPtr ppBits)
        {
            // Get the device context for the entire screen.
            IntPtr hdcScreen = GetDC(IntPtr.Zero);
            // Create a memory DC compatible with the screen.
            IntPtr hdcMem = CreateCompatibleDC(hdcScreen);

            width = GetSystemMetrics(SM_CXSCREEN);
            height = GetSystemMetrics(SM_CYSCREEN);

            // Set up the BITMAPINFO header.
            BITMAPINFO bmi = new BITMAPINFO();
            bmi.bmiHeader.biSize = (uint)Marshal.SizeOf(typeof(BITMAPINFOHEADER));
            bmi.bmiHeader.biWidth = width;
            // Use a negative height to indicate a top-down DIB.
            bmi.bmiHeader.biHeight = -height;
            bmi.bmiHeader.biPlanes = 1;
            bmi.bmiHeader.biBitCount = 32;
            bmi.bmiHeader.biCompression = 0; // BI_RGB
            bmi.bmiHeader.biSizeImage = (uint)(width * height * 4);

            // Create a DIB section (a bitmap we can directly access).
            IntPtr pBits;
            IntPtr hBitmap = CreateDIBSection(hdcScreen, ref bmi, DIB_RGB_COLORS, out pBits, IntPtr.Zero, 0);
            if (hBitmap == IntPtr.Zero)
                throw new Exception("Failed to create DIB section.");

            // Select the DIB into our memory DC.
            IntPtr hOld = SelectObject(hdcMem, hBitmap);

            // Copy the screen into our DIB.
            if (!BitBlt(hdcMem, 0, 0, width, height, hdcScreen, 0, 0, SRCCOPY))
                throw new Exception("BitBlt failed.");

            // Clean up our DCs.
            SelectObject(hdcMem, hOld);
            DeleteDC(hdcMem);
            ReleaseDC(IntPtr.Zero, hdcScreen);

            ppBits = pBits;
            return hBitmap;
        }

        static SKImage GetSKImageFromCapture(IntPtr pBits, int width, int height)
        {
            // Specify the image info.
            SKImageInfo info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);

            // Create an SKBitmap that uses the DIB pixel buffer.
            SKBitmap bitmap = new SKBitmap();
            bool success = bitmap.InstallPixels(info, pBits, info.RowBytes, releaseProc: null, context: IntPtr.Zero);
            if (!success)
                throw new Exception("Failed to install pixels into SKBitmap.");

            // Make an SKImage copy (SKImage.FromBitmap copies the pixel data).
            SKImage image = SKImage.FromBitmap(bitmap);
            bitmap.Dispose();
            return image;
        }

        public void RequestCaptureDesktop()
        {
            Task.Run(() =>
            {
                while (lastCapture == null)
                {
                    int width, height;
                    IntPtr pBits;
                    IntPtr hBmp = CaptureDesktop(out width, out height, out pBits);

                    try
                    {
                        // Create new image
                        var capture = GetSKImageFromCapture(pBits, width, height);
                        lastCapture = RMath.CreateLowResImage(capture, CaptureQuality, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
                        capture.Dispose();

                        // Invoke callback
                        onCapture?.Invoke(lastCapture);
                    }
                    finally
                    {
                        if (hBmp != IntPtr.Zero)
                            DeleteObject(hBmp);
                    }

                    Thread.Sleep(25);
                }
            });
        }

        public SKImage? GetLastCapture()
        {
            return lastCapture;
        }
        
        private const int SM_CXSCREEN = 0;
        private const int SM_CYSCREEN = 1;
        private const uint DIB_RGB_COLORS = 0;

        private const int SRCCOPY = 0x00CC0020;
        private const uint WDA_NONE = 0x00000000;
        private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;
        private const int ULW_ALPHA = 0x00000002;


        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("user32.dll")]
        private static extern bool SetWindowDisplayAffinity(IntPtr hwnd, uint dwAffinity);
        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool DeleteDC(IntPtr hdc);
        
        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight,
                                    IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern IntPtr CreateDIBSection(IntPtr hdc, [In] ref BITMAPINFO pbmi,
             uint iUsage, out IntPtr ppvBits, IntPtr hSection, uint dwOffset);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern IntPtr CreateCompatibleDC(IntPtr hdc);
             
        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAPINFOHEADER
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
        private struct BITMAPINFO
        {
            public BITMAPINFOHEADER bmiHeader;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public RGBQUAD[] bmiColors;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RGBQUAD
        {
            public byte rgbBlue;
            public byte rgbGreen;
            public byte rgbRed;
            public byte rgbReserved;
        }
    }
}