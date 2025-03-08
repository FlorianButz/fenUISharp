using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;
using FenUISharpTest1;
using SkiaSharp;

namespace FenUISharp
{
    public class FWindow
    {

        // Window Specifications

        public static double WindowRefreshRate { get; set; } = 60.0;

        public static string WindowTitle { get; private set; } = "FenUISharp Window";
        public static string WindowClass { get; private set; } = "fenUISharpWindow";

        public static int WindowWidth { get; private set; }
        public static int WindowHeight { get; private set; }

        public IntPtr hWnd { get; private set; } // Window Handle

        // Private variables

        private static IntPtr _hdcMemory = IntPtr.Zero; // Memory DC
        private static IntPtr _hBitmap = IntPtr.Zero;     // Handle to our DIB section
        private static IntPtr _ppvBits = IntPtr.Zero;     // Pointer to pixel bits

        // Rendering / SkiaSharp

        private static SKSurface? _surface;
        private static SKCanvas? _canvas;

        public static SKRect bounds { get => new SKRect(0, 0, WindowWidth, WindowHeight); }

        public static List<UIComponent> uiComponents = new List<UIComponent>();

        public static double globalTime = 0;

        // Events

        public static Action<int, int>? onMouseMove;
        public static Action? onMouseLeftClick;
        public static Action? onTrayIconRightClicked;

        public static Action? onWindowCreated;
        public static Action? onWindowUpdate; // Runs in a separate thread. DO NOT RENDER HERE

        public static Action<string>? onFileDropped;
        public static Action? onFileWantDrop;

        // Other

        private static bool alreadyCreated = false;

        public FWindow(string windowTitle, string windowClass)
        {
            if (alreadyCreated == true) throw new Exception("Another FWindow has already been created.");
            alreadyCreated = true;

            DragDropRegistration.OleInitialize(IntPtr.Zero);

            WindowTitle = windowTitle;
            WindowClass = windowClass;

            hWnd = CreateWin32Window();

            Win32Helper.DragAcceptFiles(hWnd, true);

            SetAlwaysOnTop();
            RemoveTaskbarIcon();

            onWindowCreated?.Invoke();
        }

        private void OnWindowUpdate()
        {
            onWindowUpdate?.Invoke();
        }

        public void Begin()
        {
            // Keep a reference to prevent garbage collection
            IDropTarget _dropTarget = new MyDropTarget();

            // Get COM interface pointer for the drop target
            IntPtr pDropTarget = Marshal.GetComInterfaceForObject(
                _dropTarget,
                typeof(IDropTarget)
            );

            // Register the window for drag-drop
            int hr = DragDropRegistration.RegisterDragDrop(hWnd, pDropTarget);
            if (hr != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            // Begin render stopwatch
            Stopwatch stopwatch = Stopwatch.StartNew();
            Stopwatch renderDuration = new Stopwatch();
            Stopwatch renderStepDuration = new Stopwatch();

            double lastFrameTime = 0;
            double frameTime = 1000.0 / WindowRefreshRate;

            renderStopwatch.Restart();

            // Message loop
            Win32Helper.MSG msg;
            while (true)
            {
                while (Win32Helper.PeekMessage(out msg, IntPtr.Zero, 0, 0, (int)Win32Helper.PeekMessageRemoveOptions.PM_REMOVE))
                {
                    if (msg.message == (int)Win32Helper.WindowMessages.WM_QUIT)
                        return;

                    Win32Helper.TranslateMessage(ref msg);
                    Win32Helper.DispatchMessage(ref msg);
                }

                double elapsed = stopwatch.Elapsed.TotalMilliseconds;
                if (elapsed - lastFrameTime >= frameTime)
                {
                    // Instead of lastFrameTime = elapsed, do:
                    lastFrameTime += frameTime;

                    OnWindowUpdate();

                    renderDuration.Restart();
                    Render(hWnd);

                    // Console.WriteLine("Frame Time: " + renderDuration.ElapsedMilliseconds + "ms");
                    // Console.WriteLine("Render Step Duration: " + renderStepDuration.ElapsedMilliseconds + "ms");
                    renderStepDuration.Restart();
                }

                // For finer control, you could spin-wait or do a more dynamic sleep here
                Thread.Sleep(1);
            }

        }

        private Stopwatch renderStopwatch = new Stopwatch();

        private void Render(IntPtr hWnd)
        {
            if (_surface == null)
                return;

            _canvas = _surface.Canvas;
            // Clear the surface with fully transparent pixels.
            _canvas.Clear(new SKColor(0, 0, 0, 0));

            globalTime = renderStopwatch.Elapsed.TotalMilliseconds / 1000.0;

            foreach (var component in uiComponents)
            {
                component.DrawToScreen(_canvas);
            }

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
            Console.WriteLine("Cleaning up...");

            if (_hdcMemory != IntPtr.Zero)
                Win32Helper.DeleteDC(_hdcMemory);
            if (_hBitmap != IntPtr.Zero)
                Win32Helper.DeleteObject(_hBitmap);

            Win32Helper.Shell_NotifyIconA((uint)Win32Helper.NIF.NIM_DELETE, ref _nid);

            DragDropRegistration.RevokeDragDrop(hWnd);
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

        public void SetWindowIcon(string iconPath)
        {
            IntPtr hIcon = Win32Helper.LoadImage(IntPtr.Zero, iconPath, (int)Win32Helper.IMAGE_ICON, 0, 0, Win32Helper.LR_LOADFROMFILE);
            if (hIcon != IntPtr.Zero)
            {
                Console.WriteLine("Loaded icon file.");
                Win32Helper.SendMessage(hWnd, (int)Win32Helper.WindowMessages.WM_SETICON, (IntPtr)Win32Helper.ICON_SMALL, hIcon);
                Win32Helper.SendMessage(hWnd, (int)Win32Helper.WindowMessages.WM_SETICON, (IntPtr)Win32Helper.ICON_BIG, hIcon);
            }
            else
                Console.WriteLine("Cannot load icon file.");
        }

        public void CreateTray(string iconPath, string tooltip)
        {
            AddTrayIcon(iconPath, tooltip);
        }

        private Win32Helper.NOTIFYICONDATAA _nid;

        private void AddTrayIcon(string iconPath, string tooltip)
        {
            if (_nid.hWnd == hWnd) throw new Exception("Another tray icon has already been added!");

            Win32Helper.NOTIFYICONDATAA nid = new Win32Helper.NOTIFYICONDATAA
            {
                cbSize = Marshal.SizeOf(typeof(Win32Helper.NOTIFYICONDATAA)),
                hWnd = this.hWnd,
                uID = 1,
                uFlags = (int)Win32Helper.NIF.NIF_MESSAGE | (int)Win32Helper.NIF.NIF_ICON | (int)Win32Helper.NIF.NIF_TIP,
                uCallbackMessage = (int)Win32Helper.WindowMessages.WM_USER + 1,
                szTip = tooltip
            };

            _nid = nid;

            IntPtr hIcon = Win32Helper.LoadImage(IntPtr.Zero, iconPath, (int)Win32Helper.IMAGE_ICON, 0, 0, Win32Helper.LR_LOADFROMFILE);
            nid.hIcon = hIcon;

            Win32Helper.Shell_NotifyIconA((uint)Win32Helper.NIF.NIM_ADD, ref nid);
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
        private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case (int)Win32Helper.WindowMessages.WM_USER + 1:
                    if ((int)lParam == (int)Win32Helper.WindowMessages.WM_RBUTTONUP)
                    {
                        onTrayIconRightClicked?.Invoke();
                    }
                    return IntPtr.Zero;

                case (int)Win32Helper.WindowMessages.WM_PAINT:
                    Console.WriteLine($"Window Repaint");
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
                    onMouseMove?.Invoke(x, y);
                    return IntPtr.Zero;

                case (int)Win32Helper.WindowMessages.WM_LBUTTONDOWN:
                    Console.WriteLine("Left mouse button clicked.");
                    onMouseLeftClick?.Invoke();
                    return IntPtr.Zero;

                case (int)Win32Helper.WindowMessages.WM_DROPFILES:
                {
                    IntPtr hDrop = wParam;
                    uint fileCount = DragDropRegistration.DragQueryFile(hDrop, 0xFFFFFFFF, null, 0);

                    List<string> droppedFiles = new List<string>();

                    for (uint i = 0; i < fileCount; i++)
                    {
                        // Get the required buffer size
                        uint charsRequired = DragDropRegistration.DragQueryFile(hDrop, i, null, 0);
                        if (charsRequired == 0)
                            continue;

                        StringBuilder buffer = new StringBuilder((int)charsRequired + 1);
                        DragDropRegistration.DragQueryFile(hDrop, i, buffer, (uint)buffer.Capacity);
                        droppedFiles.Add(buffer.ToString());
                    }

                    DragDropRegistration.DragFinish(hDrop); // Release the handle

                    // Invoke your event with the file paths
                    FWindow.onFileDropped?.Invoke(droppedFiles[0]);
                    Console.WriteLine("Dropped File: " + droppedFiles[0]);
                    return IntPtr.Zero;
                }

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