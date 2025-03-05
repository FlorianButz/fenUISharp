using SkiaSharp;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Timers;

namespace FenUISharpTest1
{
    class Program
    {
        // Window and style constants
        private const int CW_USEDEFAULT = unchecked((int)0x80000000);
        private const int SW_SHOWNORMAL = 1;
        private const int WM_DESTROY = 0x0002;
        private const int WM_PAINT = 0x000F;
        private const int WM_SIZE = 0x0005;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_DROPFILES = 0x0233;
        private const int WM_SETCURSOR = 0x0020;
        private const int WM_TIMER = 0x0113;
        private const int IDC_ARROW = 32512;

        public const uint PM_REMOVE = 0x0001;  // Removes messages after processing
        public const uint WM_QUIT = 0x0012;    // Quit message

        private const int WS_POPUP = unchecked((int)0x80000000);
        private const int WS_EX_LAYERED = 0x00080000;

        // For UpdateLayeredWindow
        private const uint ULW_ALPHA = 0x00000002;
        private const byte AC_SRC_OVER = 0x00;
        private const byte AC_SRC_ALPHA = 0x01;

        // Global variables for our layered window
        private static IntPtr _hdcMemory = IntPtr.Zero; // Memory DC
        private static IntPtr _hBitmap = IntPtr.Zero;     // Handle to our DIB section
        private static IntPtr _ppvBits = IntPtr.Zero;     // Pointer to pixel bits
        private static SKSurface _surface;
        private static SKCanvas _canvas;
        private static int _width = 800;
        private static int _height = 600;
        private static int posX = 0;
        private static bool dir = false;

        // Win32 structures and delegates
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct WNDCLASSEX
        {
            public uint cbSize;
            public uint style;
            public WndProcDelegate lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            public string lpszMenuName;
            public string lpszClassName;
            public IntPtr hIconSm;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public UIntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public POINT pt;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SIZE
        {
            public int cx;
            public int cy;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct BLENDFUNCTION
        {
            public byte BlendOp;
            public byte BlendFlags;
            public byte SourceConstantAlpha;
            public byte AlphaFormat;
        }

        // BITMAPINFO structures
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
        public struct BITMAPINFO
        {
            public BITMAPINFOHEADER bmiHeader;
            public uint bmiColors; // Not used for 32-bit DIBs
        }

        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        // Win32 API functions
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern ushort RegisterClassEx(ref WNDCLASSEX lpwcx);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr CreateWindowEx(
            int dwExStyle, string lpClassName, string lpWindowName, int dwStyle,
            int x, int y, int nWidth, int nHeight,
            IntPtr hWndParent, IntPtr hMenu,
            IntPtr hInstance, IntPtr lpParam);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        static extern bool UpdateWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern void PostQuitMessage(int nExitCode);

        [DllImport("user32.dll")]
        private static extern bool GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        [DllImport("user32.dll")]
        private static extern bool TranslateMessage([In] ref MSG lpMsg);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PeekMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

        [DllImport("user32.dll")]
        private static extern IntPtr DispatchMessage([In] ref MSG lpmsg);

        [DllImport("user32.dll")]
        private static extern IntPtr LoadCursor(IntPtr hInstance, int lpCursorName);

        [DllImport("user32.dll")]
        private static extern IntPtr SetCursor(IntPtr hCursor);

        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst,
            ref POINT pptDst, ref SIZE psize, IntPtr hdcSrc,
            ref POINT pptSrc, uint crKey, ref BLENDFUNCTION pblend, uint dwFlags);

        [DllImport("shell32.dll")]
        private static extern void DragAcceptFiles(IntPtr hWnd, bool accept);

        // GDI functions for our offscreen bitmap
        [DllImport("gdi32.dll", SetLastError = true)]
        static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll", SetLastError = true)]
        static extern IntPtr CreateDIBSection(IntPtr hdc, [In] ref BITMAPINFO pbmi,
            uint iUsage, out IntPtr ppvBits, IntPtr hSection, uint dwOffset);

        [DllImport("gdi32.dll", SetLastError = true)]
        static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll", SetLastError = true)]
        static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll", SetLastError = true)]
        static extern bool DeleteObject(IntPtr hObject);

        [DllImport("dwmapi.dll")]
        static extern void DwmFlush();

        [DllImport("user32.dll")]
        private static extern UIntPtr SetTimer(IntPtr hWnd, UIntPtr nIDEvent, uint uElapse, IntPtr lpTimerFunc);

        const int TIMER_ID = 1;
        const int FRAME_INTERVAL = 16; // ~60 FPS

        // Our window procedure
        private static IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case WM_PAINT:
                    return IntPtr.Zero;

                case WM_SIZE:
                    Console.WriteLine($"Window resized: {wParam} {lParam}");
                    return IntPtr.Zero;

                case WM_KEYDOWN:
                    Console.WriteLine($"Key Pressed: {wParam}");
                    return IntPtr.Zero;

                case WM_MOUSEMOVE:
                    int x = lParam.ToInt32() & 0xFFFF;
                    int y = (lParam.ToInt32() >> 16) & 0xFFFF;
                    Console.WriteLine($"Mouse moved: X={x}, Y={y}");
                    return IntPtr.Zero;

                case WM_LBUTTONDOWN:
                    Console.WriteLine("Left mouse button clicked.");
                    return IntPtr.Zero;

                case WM_DROPFILES:
                    Console.WriteLine("File dropped!");
                    return IntPtr.Zero;

                case WM_SETCURSOR:
                    SetCursor(LoadCursor(IntPtr.Zero, IDC_ARROW));
                    return IntPtr.Zero;

                case WM_DESTROY:
                    PostQuitMessage(0);
                    return IntPtr.Zero;
            }
            return DefWindowProc(hWnd, msg, wParam, lParam);
        }

        // Get screen width and height
        [DllImport("user32.dll")]
        static extern int GetSystemMetrics(int nIndex);

        private static IntPtr CreateWin32Window()
        {
            string className = "MySkiaWin32LayeredWindow";
            WNDCLASSEX wndClass = new WNDCLASSEX
            {
                cbSize = (uint)Marshal.SizeOf(typeof(WNDCLASSEX)),
                style = 0x0020, // CS_OWNDC
                lpfnWndProc = WndProc,
                hInstance = Marshal.GetHINSTANCE(typeof(Program).Module),
                lpszClassName = className
            };

            RegisterClassEx(ref wndClass);

            _width = GetSystemMetrics(0);  // SM_CXSCREEN
            _height = GetSystemMetrics(1); // SM_CYSCREEN

            // Create a borderless popup window with the layered style.
            IntPtr hWnd = CreateWindowEx(
                WS_EX_LAYERED,
                className,
                "SkiaSharp + Layered Window",
                WS_POPUP,
                0, 0, // Position at (0,0)
                _width, _height, // Use screen size
                IntPtr.Zero, IntPtr.Zero, wndClass.hInstance, IntPtr.Zero);

            SetAlwaysOnTop(hWnd, true);

            return hWnd;
        }

        private const int HWND_TOPMOST = -1;
        private const int HWND_NOTOPMOST = -2;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_SHOWWINDOW = 0x0040;

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        public static void SetAlwaysOnTop(IntPtr hWnd, bool enable)
        {
            IntPtr topmostFlag = enable ? (IntPtr)HWND_TOPMOST : (IntPtr)HWND_NOTOPMOST;
            SetWindowPos(hWnd, topmostFlag, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_SHOWWINDOW);
        }

        // Set up a memory DC and a 32-bit DIB section for per-pixel alpha rendering.
        private static void SetupLayeredDC(IntPtr hWnd)
        {
            IntPtr hdcScreen = GetDC(IntPtr.Zero);
            _hdcMemory = CreateCompatibleDC(hdcScreen);

            BITMAPINFO bmi = new BITMAPINFO();
            bmi.bmiHeader.biSize = (uint)Marshal.SizeOf(typeof(BITMAPINFOHEADER));
            bmi.bmiHeader.biWidth = _width;
            bmi.bmiHeader.biHeight = -_height; // negative to create a top-down DIB
            bmi.bmiHeader.biPlanes = 1;
            bmi.bmiHeader.biBitCount = 32;
            bmi.bmiHeader.biCompression = 0; // BI_RGB
            bmi.bmiHeader.biSizeImage = (uint)(_width * _height * 4);
            bmi.bmiHeader.biXPelsPerMeter = 0;
            bmi.bmiHeader.biYPelsPerMeter = 0;
            bmi.bmiHeader.biClrUsed = 0;
            bmi.bmiHeader.biClrImportant = 0;

            _hBitmap = CreateDIBSection(hdcScreen, ref bmi, 0, out _ppvBits, IntPtr.Zero, 0);
            SelectObject(_hdcMemory, _hBitmap);

            ReleaseDC(IntPtr.Zero, hdcScreen);

            // Create a SkiaSharp surface backed by the DIB's pixel memory.
            var imageInfo = new SKImageInfo(_width, _height, SKColorType.Bgra8888, SKAlphaType.Premul);
            _surface = SKSurface.Create(imageInfo, _ppvBits, imageInfo.RowBytes);
        }

        // Render using SkiaSharp and update the layered window with the resulting bitmap.
        private static void Render(IntPtr hWnd)
        {
            if (_surface == null)
                return;

            // Update animation position
            if (dir)
                posX += 5;
            else
                posX -= 5;

            if (posX >= 100)
                dir = false;
            else if (posX <= 0)
                dir = true;

            _canvas = _surface.Canvas;
            // Clear the surface with fully transparent pixels.
            _canvas.Clear(new SKColor(0, 0, 0, 0));

            // Draw a rectangle with opaque color.
            _canvas.DrawRect(SKRect.Create(posX, 0, 400, 100), new SKPaint() { Color = SKColors.Red });

            _canvas.Flush();

            // Set destination point to (0,0) to eliminate margin
            POINT ptSrc = new POINT { x = 0, y = 0 };
            POINT ptDst = new POINT { x = 0, y = 0 }; // Fix: Position at top-left corner
            SIZE size = new SIZE { cx = _width, cy = _height };

            BLENDFUNCTION blend = new BLENDFUNCTION
            {
                BlendOp = AC_SRC_OVER,
                SourceConstantAlpha = 255,
                AlphaFormat = AC_SRC_ALPHA
            };

            IntPtr hdcScreen = GetDC(IntPtr.Zero);
            UpdateLayeredWindow(
                hWnd,
                hdcScreen,
                ref ptDst,  // Now (0,0)
                ref size,
                _hdcMemory,
                ref ptSrc,
                0,
                ref blend,
                ULW_ALPHA
            );
            ReleaseDC(IntPtr.Zero, hdcScreen);

            DwmFlush();
        }

        static IntPtr hWnd;

        static void Main()
        {
            hWnd = CreateWin32Window();
            ShowWindow(hWnd, SW_SHOWNORMAL);
            UpdateWindow(hWnd);

            // Initialize our offscreen DC and SkiaSharp surface.
            SetupLayeredDC(hWnd);
            DragAcceptFiles(hWnd, true);

            Stopwatch stopwatch = Stopwatch.StartNew();
            double lastFrameTime = 0;
            const double frameTime = 1000.0 / 120.0; // 60 FPS

            // Message loop
            MSG msg;
            while (true)
            {
                while (PeekMessage(out msg, IntPtr.Zero, 0, 0, PM_REMOVE))
                {
                    if (msg.message == WM_QUIT)
                        return; // Exit loop properly

                    TranslateMessage(ref msg);
                    DispatchMessage(ref msg);
                }

                // Frame timing
                double elapsed = stopwatch.Elapsed.TotalMilliseconds;
                if (elapsed - lastFrameTime >= frameTime)
                {
                    lastFrameTime = elapsed;
                    Render(hWnd); // Call your SkiaSharp render function
                }

                // Sleep a bit to avoid 100% CPU usage
                Thread.Sleep(1);
            }

            // Cleanup (if needed)
            if (_hdcMemory != IntPtr.Zero)
                DeleteDC(_hdcMemory);
            if (_hBitmap != IntPtr.Zero)
                DeleteObject(_hBitmap);
        }
    }
}
