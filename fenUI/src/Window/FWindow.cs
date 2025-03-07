using System.Diagnostics;
using System.Runtime.InteropServices;
using FenUISharpTest1;
using SkiaSharp;

namespace FenUISharp
{
    public class FWindow
    {

        // Window Specifications

        public double WindowRefreshRate { get; set; } = 5.0;

        public string WindowTitle { get; private set; }
        public string WindowClass { get; private set; }

        public int WindowWidth { get; private set; }
        public int WindowHeight { get; private set; }

        public IntPtr hWnd { get; private set; } // Window Handle

        // Private variables
        private static IntPtr _hdcMemory = IntPtr.Zero; // Memory DC
        private static IntPtr _hBitmap = IntPtr.Zero;     // Handle to our DIB section
        private static IntPtr _ppvBits = IntPtr.Zero;     // Pointer to pixel bits

        // Rendering / SkiaSharp
        private static SKSurface _surface;
        private static SKCanvas _canvas;

        public FWindow(string windowTitle, string windowClass)
        {
            WindowTitle = windowTitle;
            WindowClass = windowClass;

            hWnd = CreateWin32Window();
            Win32Helper.DragAcceptFiles(hWnd, true);

            SetAlwaysOnTop();
            RemoveTaskbarIcon();
        }

        public void Begin()
        {
            // Begin render stopwatch
            Stopwatch stopwatch = Stopwatch.StartNew();

            double lastFrameTime = 0;
            double frameTime = 1000.0 / WindowRefreshRate;

            // Message loop
            Win32Helper.MSG msg;
            while (true)
            {
                while (Win32Helper.PeekMessage(out msg, IntPtr.Zero, 0, 0, (int)Win32Helper.PeekMessageRemoveOptions.PM_REMOVE))
                {
                    if (msg.message == (int)Win32Helper.WindowMessages.WM_QUIT)
                        return; // Exit loop properly

                    Win32Helper.TranslateMessage(ref msg);
                    Win32Helper.DispatchMessage(ref msg);
                }

                // Frame timing
                double elapsed = stopwatch.Elapsed.TotalMilliseconds;
                if (elapsed - lastFrameTime >= frameTime)
                {
                    lastFrameTime = elapsed;
                    Render(hWnd);
                }

                // Sleep a bit to avoid too much CPU usage
                Thread.Sleep(1);
            }
        }

        private void Render(IntPtr hWnd)
        {
            if (_surface == null)
                return;

            _canvas = _surface.Canvas;
            // Clear the surface with fully transparent pixels.
            _canvas.Clear(new SKColor(0, 0, 0, 0));

            // Draw stuff
            var rect = SKRect.Create(0, 0, 100, 100);

string skslCode = @"
        uniform vec2 u_resolution;
        uniform vec4 u_color;
        
        half4 main(float2 fragCoord) {
            vec2 p = (fragCoord / u_resolution) * 2.0 - 1.0; // Normalize -1 to 1
            float r = 1.0;
            float n = 3; // Squircle roundness

            float d = pow(abs(p.x), n) + pow(abs(p.y), n);
            if (d > pow(r, n)) {
                return half4(0.0); // Transparent outside
            }
            
            return u_color; // Inside squircle
        }
    ";

    // Compile the shader
    var effect = SKRuntimeEffect.CreateShader(skslCode, out var error);
    if (effect == null) throw new Exception("Shader compile error: " + error);

    // Set uniform values
    var uniforms = new SKRuntimeEffectUniforms(effect);
    uniforms["u_resolution"] = new SKPoint(rect.Width, rect.Height);
    uniforms["u_color"] = SKColors.White;

    // Create the shader
    var shader = effect.ToShader(uniforms);

    // Paint object with the shader
    using var paint = new SKPaint { Shader = shader };
    paint.IsAntialias = true;

    // Draw on canvas
    _canvas.DrawRect(rect, paint);

            _canvas.Flush();

            // Set destination point to (0,0) to eliminate margin
            Win32Helper.POINT ptSrc = new Win32Helper.POINT { x = 0, y = 0 };
            Win32Helper.POINT ptDst = new Win32Helper.POINT { x = 0, y = 0 }; // Fix: Position at top-left corner
            Win32Helper.SIZE size = new Win32Helper.SIZE { cx = WindowWidth, cy = WindowHeight };

            Win32Helper.BLENDFUNCTION blend = new Win32Helper.BLENDFUNCTION
            {
                BlendOp = (int)Win32Helper.AlphaBlendOptions.AC_SRC_OVER,
                SourceConstantAlpha = 255,
                AlphaFormat = (int)Win32Helper.AlphaBlendOptions.AC_SRC_ALPHA
            };

            IntPtr hdcScreen = Win32Helper.GetDC(IntPtr.Zero);
            Win32Helper.UpdateLayeredWindow(
                hWnd,
                hdcScreen,
                ref ptDst,  // Now (0,0)
                ref size,
                _hdcMemory,
                ref ptSrc,
                0,
                ref blend,
                (int)Win32Helper.LayeredWindowFlags.ULW_ALPHA
            );
            Win32Helper.ReleaseDC(IntPtr.Zero, hdcScreen);

            Win32Helper.DwmFlush();
        }

        public void Cleanup()
        {
            if (_hdcMemory != IntPtr.Zero)
                Win32Helper.DeleteDC(_hdcMemory);
            if (_hBitmap != IntPtr.Zero)
                Win32Helper.DeleteObject(_hBitmap);
        }

        public void CreateSurface()
        {
            SetupLayeredDC(hWnd);
        }

        // Set up a memory DC and a 32-bit DIB section for per-pixel alpha rendering.
        private void SetupLayeredDC(IntPtr hWnd)
        {
            IntPtr hdcScreen = Win32Helper.GetDC(IntPtr.Zero);
            _hdcMemory = Win32Helper.CreateCompatibleDC(hdcScreen);

            Win32Helper.BITMAPINFO bmi = new Win32Helper.BITMAPINFO();
            bmi.bmiHeader.biSize = (uint)Marshal.SizeOf(typeof(Win32Helper.BITMAPINFOHEADER));
            bmi.bmiHeader.biWidth = WindowWidth;
            bmi.bmiHeader.biHeight = -WindowHeight; // negative to create a top-down DIB
            bmi.bmiHeader.biPlanes = 1;
            bmi.bmiHeader.biBitCount = 32;
            bmi.bmiHeader.biCompression = 0; // BI_RGB
            bmi.bmiHeader.biSizeImage = (uint)(WindowWidth * WindowHeight * 4);
            bmi.bmiHeader.biXPelsPerMeter = 0;
            bmi.bmiHeader.biYPelsPerMeter = 0;
            bmi.bmiHeader.biClrUsed = 0;
            bmi.bmiHeader.biClrImportant = 0;

            _hBitmap = Win32Helper.CreateDIBSection(hdcScreen, ref bmi, 0, out _ppvBits, IntPtr.Zero, 0);
            Win32Helper.SelectObject(_hdcMemory, _hBitmap);

            Win32Helper.ReleaseDC(IntPtr.Zero, hdcScreen);

            // Create a SkiaSharp surface backed by the DIB's pixel memory.
            var imageInfo = new SKImageInfo(WindowWidth, WindowHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
            _surface = SKSurface.Create(imageInfo, _ppvBits, imageInfo.RowBytes);
        }

        public void Show()
        {
            Win32Helper.ShowWindow(hWnd, (int)Win32Helper.ShowWindowCommands.SW_SHOWNORMAL);
        }

        private IntPtr CreateWin32Window()
        {
            string className = WindowClass;
            Win32Helper.WNDCLASSEX wndClass = new Win32Helper.WNDCLASSEX
            {
                cbSize = (uint)Marshal.SizeOf(typeof(Win32Helper.WNDCLASSEX)),
                style = 0x0020, // CS_OWNDC
                lpfnWndProc = WndProc,
                hInstance = Marshal.GetHINSTANCE(typeof(Program).Module),
                lpszClassName = className
            };

            Win32Helper.RegisterClassEx(ref wndClass);

            WindowWidth = Win32Helper.GetSystemMetrics(0);  // SM_CXSCREEN
            WindowHeight = Win32Helper.GetSystemMetrics(1); // SM_CYSCREEN

            // Create a borderless popup window with the layered style.
            var hWnd = Win32Helper.CreateWindowEx(
                (int)Win32Helper.WindowStyles.WS_EX_LAYERED,
                className,
                WindowTitle,
                (int)Win32Helper.WindowStyles.WS_POPUP,
                0, 0,
                WindowWidth, WindowHeight,
                IntPtr.Zero, IntPtr.Zero, wndClass.hInstance, IntPtr.Zero);

            return hWnd;
        }

        private void SetAlwaysOnTop()
        {
            IntPtr topmostFlag = (IntPtr)Win32Helper.HWND_TOPMOST;
            Win32Helper.SetWindowPos(hWnd, topmostFlag, 0, 0, 0, 0,
                (int)Win32Helper.SetWindowPosFlags.SWP_NOSIZE |
                (int)Win32Helper.SetWindowPosFlags.SWP_NOMOVE |
                (int)Win32Helper.SetWindowPosFlags.SWP_SHOWWINDOW);
        }

        private void RemoveTaskbarIcon()
        {
            int exStyle = Win32Helper.GetWindowLong(hWnd, (int)Win32Helper.WindowLongs.GWL_EXSTYLE);
            Win32Helper.SetWindowLong(hWnd, (int)Win32Helper.WindowLongs.GWL_EXSTYLE, exStyle | (int)Win32Helper.WindowStyles.WS_EX_TOOLWINDOW);
        }

        // Window Procedure
        private static IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case (int)Win32Helper.WindowMessages.WM_PAINT:
                    return IntPtr.Zero;

                case (int)Win32Helper.WindowMessages.WM_SIZE:
                    Console.WriteLine($"Window resized: {wParam} {lParam}");
                    return IntPtr.Zero;

                case (int)Win32Helper.WindowMessages.WM_KEYDOWN:
                    Console.WriteLine($"Key Pressed: {wParam}");
                    return IntPtr.Zero;

                case (int)Win32Helper.WindowMessages.WM_MOUSEMOVE:
                    int x = lParam.ToInt32() & 0xFFFF;
                    int y = (lParam.ToInt32() >> 16) & 0xFFFF;
                    Console.WriteLine($"Mouse moved: X={x}, Y={y}");
                    return IntPtr.Zero;

                case (int)Win32Helper.WindowMessages.WM_LBUTTONDOWN:
                    Console.WriteLine("Left mouse button clicked.");
                    return IntPtr.Zero;

                case (int)Win32Helper.WindowMessages.WM_DROPFILES:
                    Console.WriteLine("File dropped!");
                    return IntPtr.Zero;

                case (int)Win32Helper.WindowMessages.WM_SETCURSOR:
                    Win32Helper.SetCursor(Win32Helper.LoadCursor(IntPtr.Zero, Win32Helper.IDC_ARROW));
                    return IntPtr.Zero;

                case (int)Win32Helper.WindowMessages.WM_DESTROY:
                    Win32Helper.PostQuitMessage(0);
                    return IntPtr.Zero;
            }
            return Win32Helper.DefWindowProcW(hWnd, msg, wParam, lParam);
        }
    }
}