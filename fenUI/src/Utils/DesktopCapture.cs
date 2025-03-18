using System.Runtime.InteropServices;
using SkiaSharp;

namespace FenUISharp
{
    public class DesktopCapture
    {
        public static DesktopCapture instance;
        private Thread _captureThread;
        private bool _isRunning = false;

        public SKImage? previousCapture;
        public SKImage? lastCapture;

        public Action<SKImage> onCapture;
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
            IntPtr hdcScreen = Win32Helper.GetDC(IntPtr.Zero);
            // Create a memory DC compatible with the screen.
            IntPtr hdcMem = Win32Helper.CreateCompatibleDC(hdcScreen);

            width = Win32Helper.GetSystemMetrics(Win32Helper.SM_CXSCREEN);
            height = Win32Helper.GetSystemMetrics(Win32Helper.SM_CYSCREEN);

            // Set up the BITMAPINFO header.
            Win32Helper.BITMAPINFO bmi = new Win32Helper.BITMAPINFO();
            bmi.bmiHeader.biSize = (uint)Marshal.SizeOf(typeof(Win32Helper.BITMAPINFOHEADER));
            bmi.bmiHeader.biWidth = width;
            // Use a negative height to indicate a top-down DIB.
            bmi.bmiHeader.biHeight = -height;
            bmi.bmiHeader.biPlanes = 1;
            bmi.bmiHeader.biBitCount = 32;
            bmi.bmiHeader.biCompression = 0; // BI_RGB
            bmi.bmiHeader.biSizeImage = (uint)(width * height * 4);

            // Create a DIB section (a bitmap we can directly access).
            IntPtr pBits;
            IntPtr hBitmap = Win32Helper.CreateDIBSection(hdcScreen, ref bmi, Win32Helper.DIB_RGB_COLORS, out pBits, IntPtr.Zero, 0);
            if (hBitmap == IntPtr.Zero)
                throw new Exception("Failed to create DIB section.");

            // Select the DIB into our memory DC.
            IntPtr hOld = Win32Helper.SelectObject(hdcMem, hBitmap);

            // Copy the screen into our DIB.
            if (!Win32Helper.BitBlt(hdcMem, 0, 0, width, height, hdcScreen, 0, 0, Win32Helper.SRCCOPY))
                throw new Exception("BitBlt failed.");

            // Clean up our DCs.
            Win32Helper.SelectObject(hdcMem, hOld);
            Win32Helper.DeleteDC(hdcMem);
            Win32Helper.ReleaseDC(IntPtr.Zero, hdcScreen);

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

        private int requested = 0;

        public void Begin()
        {
            requested++;

            if (_isRunning) return;
            _isRunning = true;

            bool firstRun = true;

            _captureThread = new Thread(() =>
            {
                while (_isRunning)
                {
                    int width, height;
                    IntPtr pBits;
                    IntPtr hBmp = CaptureDesktop(out width, out height, out pBits);

                    try
                    {
                        // Dispose previous image
                        if(previousCapture != lastCapture)
                            previousCapture?.Dispose();
                        previousCapture = lastCapture;

                        // Create new image
                        var capture = GetSKImageFromCapture(pBits, width, height);
                        lastCapture = RMath.CreateLowResImage(capture, CaptureQuality);
                        capture.Dispose();

                        if(firstRun) previousCapture = lastCapture;

                        // Invoke callback
                        onCapture?.Invoke(lastCapture);
                    }
                    finally
                    {
                        if (hBmp != IntPtr.Zero)
                            Win32Helper.DeleteObject(hBmp);
                    }

                    for(int i = 0; i < CaptureInterval; i++){
                        timeSinceLastCapture = i;
                        Thread.Sleep(1);
                    }
                    firstRun = false;
                }
            });

            Win32Helper.SetWindowDisplayAffinity(Window.hWnd, Win32Helper.WDA_EXCLUDEFROMCAPTURE);

            _captureThread.IsBackground = true;
            _captureThread.Start();
        }

        public void Stop()
        {
            requested--;
            if(requested > 0) return;

            _isRunning = false;

            // Clean up resources
            previousCapture?.Dispose();
            previousCapture = null;

            lastCapture?.Dispose();
            lastCapture = null;

            Win32Helper.SetWindowDisplayAffinity(Window.hWnd, Win32Helper.WDA_NONE);
        }
    }
}