using System.Diagnostics;
using System.Numerics;
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

        public static IntPtr hWnd { get; private set; } // Window Handle

        // Private variables

        private static IntPtr _hdcMemory = IntPtr.Zero; // Memory DC
        private static IntPtr _hBitmap = IntPtr.Zero;     // Handle to our DIB section
        private static IntPtr _ppvBits = IntPtr.Zero;     // Pointer to pixel bits

        private static IntPtr _mouseHookID = IntPtr.Zero;
        private static IntPtr _keyboardHookID = IntPtr.Zero;

        private static Win32Helper.LowLevelMouseProc _mouseProc = MouseHookCallback;
        private static Win32Helper.LowLevelKeyboardProc _keyboardProc = KeyboardHookCallback;
        private readonly Win32Helper.WndProcDelegate _wndProcDelegate;

        // Rendering / SkiaSharp

        private static SKSurface? _surface;
        private static SKCanvas? _canvas;

        public static SKRect bounds { get => new SKRect(0, 0, WindowWidth, WindowHeight); }

        public static List<FUIComponent> uiComponents = new List<FUIComponent>();

        public static double globalTime = 0;

        // Events

        public static Action<int, int>? onMouseMove;

        public static Action? onMouseLeftDown;
        public static Action? onMouseLeftUp;
        public static Action? onMouseMiddleDown;
        public static Action? onMouseMiddleUp;
        public static Action? onMouseRightDown;
        public static Action? onMouseRightUp;

        public static Action<int>? onKeyPressed; // Filters for only when the user presses and ignores repeated inputs after that
        public static Action<int>? onKeyTyped; // Triggers on raw key input
        public static Action<int>? onKeyReleased;

        public static Action<int>? onMouseScroll;
        public static Action? onTrayIconRightClicked;

        public static Action? onWindowCreated;
        public static Action? onWindowUpdate;

        public static Action<string>? onFileDropped;
        public static Action? onFileWantDrop;

        // Other

        public static Vector2 MousePosition { get; private set; } = new Vector2(0, 0);

        public static SKSamplingOptions samplingOptions { get; private set; } = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);
        private static bool alreadyCreated = false;


        public static float DeltaTime { get; private set; }

        public FWindow(string windowTitle, string windowClass)
        {
            if (alreadyCreated == true) throw new Exception("Another FWindow has already been created.");
            alreadyCreated = true;

            _wndProcDelegate = WndProc;

            DragDropRegistration.OleInitialize(IntPtr.Zero);

            WindowTitle = windowTitle;
            WindowClass = windowClass;

            hWnd = CreateWin32Window();

            Win32Helper.DragAcceptFiles(hWnd, true);

            SetAlwaysOnTop();
            RemoveTaskbarIcon();

            onWindowCreated?.Invoke();

            Thread.CurrentThread.Name = "Win32 Window";

            Win32Helper.SetWindowDisplayAffinity(hWnd, Win32Helper.WDA_EXCLUDEFROMCAPTURE);
        }

        private void OnWindowUpdate_BeforeFrameRender()
        {
            onWindowUpdate?.Invoke();
        }

        private Thread _renderThread;
        private volatile bool _isRunning = true;

        public void Begin()
        {
            // Keep a reference to prevent garbage collection
            IDropTarget _dropTarget = new FDropTarget();

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

            RegisterHook();

            _renderThread = new Thread(RenderLoop)
            {
                IsBackground = true
            };
            _renderThread.Name = "Render Loop";
            _renderThread.Start();

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
            }
        }

        private void RenderLoop()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            double frameInterval = 1000.0 / WindowRefreshRate; // e.g., 16.67 ms for 60 FPS
            double nextFrameTime = 0;
            double previousFrameTime = 0;

            while (_isRunning)
            {
                double currentTime = stopwatch.Elapsed.TotalMilliseconds;

                // Check if it's time to render the next frame
                if (currentTime >= nextFrameTime)
                {
                    // Calculate DeltaTime (time since the last frame)
                    DeltaTime = (float)(currentTime - previousFrameTime) / 1000.0f; // Convert to seconds
                    previousFrameTime = currentTime;

                    // Render the frame
                    OnWindowUpdate_BeforeFrameRender();
                    RenderFrame();

                    // Notify the main thread to update the window
                    Win32Helper.PostMessageA(hWnd, (int)Win32Helper.WindowMessages.WM_RENDER, IntPtr.Zero, IntPtr.Zero);

                    // Schedule the next frame
                    nextFrameTime = currentTime + frameInterval;
                }

                // Sleep briefly to avoid busy-waiting
                Thread.Sleep(1);
            }
        }

        private readonly object _renderLock = new object();

        private void RenderFrame()
        {
            lock (_renderLock)
            {
                if (_surface == null)
                {
                    Console.WriteLine("Surface is null!");
                    return;
                }

                _canvas = _surface.Canvas;
                _canvas.Clear(new SKColor(0, 0, 0, 0));

                foreach (var component in uiComponents)
                {
                    if (component.enabled && component.transform.parent == null)
                        component.DrawToScreen(_canvas);
                }

                _canvas.Flush();
            }
        }

        private void UpdateWindow()
        {
            lock (_renderLock)
            {
                Win32Helper.POINT ptSrc = new Win32Helper.POINT { x = 0, y = 0 };
                Win32Helper.POINT ptDst = new Win32Helper.POINT { x = 0, y = 0 };
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
                    ref ptDst,
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
        }

        public void Cleanup()
        {
            Console.WriteLine("Cleaning up...");
            _isRunning = false;

            if (_renderThread != null && _renderThread.IsAlive)
            {
                _renderThread.Join();
            }

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
                hWnd = FWindow.hWnd,
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
                lpfnWndProc = _wndProcDelegate,
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

        private void RegisterHook()
        {
            IntPtr moduleHandle = Win32Helper.GetModuleHandle(null);

            RegMouseHook();
            _keyboardHookID = Win32Helper.SetWindowsHookEx((int)Win32Helper.WindowsHooks.WH_KEYBOARD_LL, _keyboardProc, moduleHandle, 0);
        }

        void RegMouseHook()
        {
            IntPtr moduleHandle = Win32Helper.GetModuleHandle(null);
            _mouseHookID = Win32Helper.SetWindowsHookEx((int)Win32Helper.WindowsHooks.WH_MOUSE_LL, _mouseProc, moduleHandle, 0);
        }

        private static IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                Win32Helper.MSLLHOOKSTRUCT mouseInfo = Marshal.PtrToStructure<Win32Helper.MSLLHOOKSTRUCT>(lParam);

                // Mouse coordinates
                int mouseX = mouseInfo.pt.x;
                int mouseY = mouseInfo.pt.y;

                // Mouse wheel scrolling
                if (wParam == (IntPtr)Win32Helper.WindowsHooks.WM_MOUSEWHEEL)
                {
                    short scrollDelta = (short)((mouseInfo.mouseData >> 16) & 0xFFFF);
                    onMouseScroll?.Invoke(scrollDelta);
                }

                if (wParam == (IntPtr)Win32Helper.WindowMessages.WM_MOUSEMOVE)
                {
                    MousePosition = new Vector2(mouseX, mouseY);

                    onMouseMove?.Invoke(mouseX, mouseY);
                }
            }

            return Win32Helper.CallNextHookEx(_mouseHookID, nCode, wParam, lParam);
        }

        private static List<int> _pressedKeys = new List<int>();

        private static IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                Win32Helper.KBDLLHOOKSTRUCT keyInfo = Marshal.PtrToStructure<Win32Helper.KBDLLHOOKSTRUCT>(lParam);

                if (wParam == (IntPtr)Win32Helper.WindowsHooks.WM_KEYDOWN)
                {
                    // Console.WriteLine($"Key Down: {keyInfo.vkCode}");
                    // Console.WriteLine($"Key Down: {KeyInfo.GetKeyName(keyInfo.vkCode)}");

                    if (!_pressedKeys.Contains(keyInfo.vkCode))
                        onKeyPressed?.Invoke(keyInfo.vkCode);
                    _pressedKeys.Add(keyInfo.vkCode);

                    onKeyTyped?.Invoke(keyInfo.vkCode);
                }
                else if (wParam == (IntPtr)Win32Helper.WindowsHooks.WM_KEYUP)
                {
                    // Console.WriteLine($"Key Up: {keyInfo.vkCode}");
                    // Console.WriteLine($"Key Up: {KeyInfo.GetKeyName(keyInfo.vkCode)}");
                    onKeyReleased?.Invoke(keyInfo.vkCode);

                    if (_pressedKeys.Contains(keyInfo.vkCode))
                        _pressedKeys.Remove(keyInfo.vkCode);
                }
            }

            return Win32Helper.CallNextHookEx(_keyboardHookID, nCode, wParam, lParam);
        }

        public void UnregisterHook()
        {
            if (_keyboardHookID != IntPtr.Zero)
            {
                Win32Helper.UnhookWindowsHookEx(_keyboardHookID);
                _keyboardHookID = IntPtr.Zero;
            }

            UnRegMouseHook();
        }

        void UnRegMouseHook()
        {
            if (_mouseHookID != IntPtr.Zero)
            {
                Win32Helper.UnhookWindowsHookEx(_mouseHookID);
                _mouseHookID = IntPtr.Zero;
            }
        }

        public static FMultiAccess<Win32Helper.Cursors> ActiveCursor { get; private set; } = new FMultiAccess<Win32Helper.Cursors>(Win32Helper.Cursors.IDC_ARROW);

        // Window Procedure
        private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case (int)Win32Helper.WindowMessages.WM_USER + 1:
                    if ((int)lParam == (int)Win32Helper.WindowMessages.WM_RBUTTONDOWN)
                    {
                        onTrayIconRightClicked?.Invoke();
                    }
                    return IntPtr.Zero;

                case (uint)Win32Helper.WindowMessages.WM_RENDER:
                    UpdateWindow();
                    return IntPtr.Zero;

                case (int)Win32Helper.WindowMessages.WM_PAINT:
                    return IntPtr.Zero;

                case (int)Win32Helper.WindowMessages.WM_SIZE:
                    Console.WriteLine($"Window resized: {wParam} {lParam}");
                    return IntPtr.Zero;

                case (int)Win32Helper.WindowMessages.WM_KEYDOWN:
                    // Console.WriteLine($"Key Pressed: {wParam}");
                    return IntPtr.Zero;

                case (int)Win32Helper.WindowMessages.WM_LBUTTONDOWN:
                    // Console.WriteLine("Left mouse button down.");
                    onMouseLeftDown?.Invoke();
                    return IntPtr.Zero;

                case (int)Win32Helper.WindowMessages.WM_RBUTTONDOWN:
                    // Console.WriteLine("Right mouse button down.");
                    onMouseRightDown?.Invoke();
                    return IntPtr.Zero;

                case (int)Win32Helper.WindowMessages.WM_MBUTTONDOWN:
                    // Console.WriteLine("Middle mouse button down.");
                    onMouseMiddleDown?.Invoke();
                    return IntPtr.Zero;

                case (int)Win32Helper.WindowMessages.WM_LBUTTONUP:
                    // Console.WriteLine("Left mouse button up.");
                    onMouseLeftUp?.Invoke();
                    return IntPtr.Zero;

                case (int)Win32Helper.WindowMessages.WM_RBUTTONUP:
                    // Console.WriteLine("Right mouse button up.");
                    onMouseRightUp?.Invoke();
                    return IntPtr.Zero;

                case (int)Win32Helper.WindowMessages.WM_MBUTTONUP:
                    // Console.WriteLine("Middle mouse button up.");
                    onMouseMiddleUp?.Invoke();
                    return IntPtr.Zero;

                // This should fix mouse lag - Edit: it doesn't.
                // case (int)Win32Helper.WindowMessages.WM_MOUSEHOVER:
                //     Console.WriteLine("Unregister");
                //     UnRegMouseHook();
                //     return IntPtr.Zero;

                // case (int)Win32Helper.WindowMessages.WM_MOUSELEAVE:
                //     Console.WriteLine("Register");
                //     RegMouseHook();
                //     return IntPtr.Zero;

                // // Use this for when the mouse is inside client area
                // case (int)Win32Helper.WindowMessages.WM_MOUSEMOVE:
                //     int x = lParam.ToInt32() & 0xFFFF;
                //     int y = (lParam.ToInt32() >> 16) & 0xFFFF;
                //     onMouseMove?.Invoke(x, y);
                //     return IntPtr.Zero;


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
                    Win32Helper.SetCursor(Win32Helper.LoadCursor(IntPtr.Zero, (int)ActiveCursor.Value));
                    return IntPtr.Zero;

                case (int)Win32Helper.WindowMessages.WM_CLOSE:
                    Cleanup();
                    Win32Helper.DestroyWindow(hWnd);
                    return IntPtr.Zero;

                case (int)Win32Helper.WindowMessages.WM_DESTROY:
                    Cleanup();
                    Win32Helper.PostQuitMessage(0);
                    return IntPtr.Zero;
            }
            return Win32Helper.DefWindowProcW(hWnd, msg, wParam, lParam);
        }

        public static void HideWindow()
        {
            Win32Helper.ShowWindow(hWnd, 0);
        }

        public static void ShowWindow()
        {
            Win32Helper.ShowWindow(hWnd, 5);
        }
    }
}