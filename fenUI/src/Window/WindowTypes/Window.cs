using System.Diagnostics;
using System.Runtime.InteropServices;
using FenUISharp.Themes;
using FenUISharpTest1;
using SkiaSharp;

namespace FenUISharp
{
    public abstract class Window : IDisposable
    {

        #region Window Properties

        public string WindowTitle { get; protected set; }
        public string WindowClass { get; protected set; }

        public Vector2 WindowPosition { get; protected set; }
        public Vector2 WindowSize { get; protected set; }

        public Vector2 WindowMinSize { get; protected set; } = new Vector2(400, 300);
        public Vector2 WindowMaxSize { get; protected set; } = new Vector2(float.MaxValue, float.MaxValue);

        protected bool _allowResize = true;
        public bool AllowResizing { get => _allowResize; set => UpdateAllowResize(value); }

        protected int _refreshRate = 60;
        public int RefreshRate { get => _refreshRate; set { _refreshRate = value; RenderContext?.OnWindowPropertyChanged(); } }
        public double DeltaTime { get; private set; }

        public Vector2 ClientMousePosition
        {
            get
            {
                var mousePos = new POINT() { x = (int)GlobalHooks.MousePosition.x, y = (int)GlobalHooks.MousePosition.y };
                ScreenToClient(hWnd, ref mousePos);
                return new Vector2(mousePos.x, mousePos.y);
            }
        }

        protected bool _sysDarkMode = false;
        public bool SystemDarkMode { get => _sysDarkMode; set { _sysDarkMode = value; UpdateSysDarkmode(); } }

        public ThemeManager WindowThemeManager { get; private set; }

        public SKRect Bounds { get; private set; }

        public IntPtr hWnd { get; private set; }

        public DragDropHandler DropTarget { get; private set; }
        public FRenderContext RenderContext { get; private set; }

        #endregion

        #region Actions

        public Action<MouseInputCode> MouseAction { get; set; }
        public Action<MouseInputCode> MouseMove { get; set; }
        public Action<Vector2> OnWindowResize { get; set; }
        public Action<MouseInputCode> OnTrayIconClick { get; set; }

        public Action OnBeginRender { get; set; }
        public Action OnEndRender { get; set; }
        public Action OnUpdate { get; set; }

        #endregion

        #region Private

        private volatile List<UIComponent> uiComponents = new List<UIComponent>();

        private readonly WndProcDelegate _wndProcDelegate;
        protected bool _alwaysOnTop;
        protected bool _isDirty = false;
        protected bool _isResizing = false;

        private Thread _renderThread;

        #endregion

        #region Constructors

        public enum RenderContextType
        {
            Software,
            OpenGL
        }

        public Window(
            string title, string className, RenderContextType type,
            Vector2? windowSize = null, Vector2? windowPosition = null,
            bool alwaysOnTop = false, bool hideTaskbarIcon = false
        )
        {
            WindowTitle = title;
            WindowClass = className;

            WindowSize = (windowSize == null) ? new Vector2(400, 300) : windowSize.Value;
            WindowPosition = (windowPosition == null) ? new Vector2(0, 0) : windowPosition.Value;

            _wndProcDelegate = WindowsProcedure;

            // Create window and handle
            hWnd = CreateWin32Window(RegisterClass(), WindowSize, windowPosition);
            if (hWnd == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                throw new Exception($"Window creation failed: error {error}");
            }

            WindowFeatures.TryInitialize(); // Initialize all window features
            WindowThemeManager = new ThemeManager(Resources.GetTheme("default-dark"));

            // Pre initialize OLE DragDrop
            DragDropRegistration.Initialize();

            _alwaysOnTop = alwaysOnTop;
            SetAlwaysOnTop(_alwaysOnTop);

            SetTaskbarIconVisibility(hideTaskbarIcon);

            // Initialize FRenderContext
            CreateAndUpdateRenderContext(type);

            RecalcClientBounds();
        }

        #endregion

        public virtual void CreateAndUpdateRenderContext(RenderContextType type)
        {
            switch (type)
            {
                case RenderContextType.Software: RenderContext = new SoftwareRenderContext(this); break;
                case RenderContextType.OpenGL: RenderContext = new GLRenderContext(this); break;
            }
        }

        protected virtual void UpdateSysDarkmode()
        {
            const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
            const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19; // Windows 10 1809+

            int useDarkMode = _sysDarkMode ? 1 : 0;

            if (DwmSetWindowAttribute(hWnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDarkMode, sizeof(int)) != 0)
            {
                // If it fails, try the older one
                DwmSetWindowAttribute(hWnd, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref useDarkMode, sizeof(int));
            }

            if (ShouldSystemUseDarkMode() == 1) // Check if the system is in dark mode
            {
                AllowDarkModeForWindow(hWnd, true);
            }
        }

        public void BeginWindowLoop()
        {
            // Setup OLE DragDrop
            // Keep a reference to prevent garbage collection
            DropTarget = new DragDropHandler();

            // Get COM interface pointer for the drop target
            IntPtr pDropTarget = Marshal.GetComInterfaceForObject(
                DropTarget,
                typeof(IDropTarget)
            );

            // Register the window for drag-drop
            int hr = DragDropRegistration.RegisterDragDrop(hWnd, pDropTarget);
            if (hr != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            Marshal.Release(pDropTarget);

            // Start render loop and windows proc

            _isRunning = true;
            _renderThread = new Thread(() =>
            {
                Thread.CurrentThread.Priority = ThreadPriority.Highest;
                RenderLoop();
            });
            _renderThread.Start();

            MSG msg;
            while (_isRunning)
            {
                while (GetMessage(out msg, IntPtr.Zero, 0, 0))
                {
                    TranslateMessage(ref msg);
                    DispatchMessage(ref msg);

                    Thread.Sleep(1); // Prevent too high cpu usage
                }
            }
        }

        volatile bool _isRunning = false;

        private async void RenderLoop()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            double frameInterval = 1000.0 / RefreshRate;
            double nextFrameTime = 0;
            double previousFrameTime = 0;

            while (true) // temporary
            {
                if (!_isRunning)
                {
                    new List<UIComponent>(uiComponents).ForEach(x => x?.Dispose());
                    return;
                }

                double currentTime = stopwatch.Elapsed.TotalMilliseconds;

                if (currentTime >= nextFrameTime)
                {
                    DeltaTime = (float)(currentTime - previousFrameTime) / 1000.0f;
                    previousFrameTime = currentTime;

                    OnUpdate?.Invoke();

                    if (IsNextFrameRendering() || _isDirty)
                    {
                        OnBeginRender?.Invoke();
                        RenderFrame();
                        OnEndRender?.Invoke();

                        _isDirty = false;
                    }

                    nextFrameTime = currentTime + frameInterval;
                }

                // await Task.Delay(needsRender ? 1 : 16); // If not rendering, sleep longer (saves CPU); Edit: Not good, since the Update method inside the FUIComponents gets skipped then
                await Task.Delay(1);
            }
        }

        protected virtual void OnRenderFrame() { }

        private readonly object _renderLock = new object();

        private void RenderFrame()
        {
            if (_isResizing) return;

            lock (_renderLock)
            {
                var _canvas = RenderContext.BeginDraw().Canvas;
                OnRenderFrame();

                foreach (var component in uiComponents)
                {
                    if (component.enabled && component.transform.parent == null)
                        component.DrawToScreen(_canvas);
                }

                RenderContext.EndDraw();
            }
        }

        public void AddUIComponent(UIComponent component)
        {
            if (component == null) return;

            component.WindowRoot = this;
            uiComponents.Add(component);
        }

        public void RemoveUIComponent(UIComponent component)
        {
            if (component == null || !uiComponents.Contains(component)) return;

            // component.WindowRoot = null; // Don't do, could break some stuff
            uiComponents.Remove(component);
        }

        public void DestroyUIComponent(UIComponent component)
        {
            if (component == null) return;

            component.Dispose();
            uiComponents.Remove(component);
        }

        public bool IsNextFrameRendering()
        {
            return uiComponents.Any(x => x._isGloballyInvalidated && x.enabled && x.visible);
        }

        public List<UIComponent> GetUIComponents()
        {
            return uiComponents;
        }

        public void SetWindowVisibility(bool visible)
        {
            ShowWindow(hWnd, visible ? 1 : 0);
        }

        public void SetAlwaysOnTop(bool alwaysOnTop)
        {
            _alwaysOnTop = alwaysOnTop;
            SetWindowPos(hWnd, alwaysOnTop ? HWND_TOPMOST : HWND_NOTOPMOST, 0, 0, 0, 0, (uint)SetWindowPosFlags.SWP_NOMOVE | (uint)SetWindowPosFlags.SWP_NOSIZE);
        }

        protected WNDCLASSEX RegisterClass()
        {
            WNDCLASSEX wndClass = new WNDCLASSEX
            {
                cbSize = (uint)Marshal.SizeOf(typeof(WNDCLASSEX)),
                style = 0x0020,
                lpfnWndProc = _wndProcDelegate,
                hInstance = Marshal.GetHINSTANCE(typeof(Program).Module),
                lpszClassName = WindowClass
            };

            RegisterClassExA(ref wndClass);

            return wndClass;
        }

        protected abstract IntPtr CreateWin32Window(WNDCLASSEX wndClass, Vector2? size, Vector2? position);

        private NOTIFYICONDATAA _nid;

        public void SetTrayIcon(string iconPath, string tooltip)
        {
            if (_nid.hWnd == hWnd) throw new Exception("Another tray icon has already been added for this window!");

            NOTIFYICONDATAA nid = new NOTIFYICONDATAA
            {
                cbSize = Marshal.SizeOf(typeof(NOTIFYICONDATAA)),
                hWnd = hWnd,
                uID = 1,
                uFlags = (int)NIF.NIF_MESSAGE | (int)NIF.NIF_ICON | (int)NIF.NIF_TIP,
                uCallbackMessage = (int)WindowMessages.WM_USER + 1,
                szTip = tooltip
            };

            _nid = nid;

            IntPtr hIcon = LoadImage(IntPtr.Zero, iconPath, (int)IMAGE_ICON, 0, 0, LR_LOADFROMFILE);
            nid.hIcon = hIcon;

            Shell_NotifyIconA((uint)NIF.NIM_ADD, ref nid);
        }

        public void SetWindowIcon(string? iconPath, string? smallIconPath = null)
        {
            if (iconPath == null)
            {
                SendMessage(hWnd, WM_SETICON, (IntPtr)ICON_BIG, IntPtr.Zero);
                SendMessage(hWnd, WM_SETICON, (IntPtr)ICON_SMALL, IntPtr.Zero);

                return;
            }

            // Load icon from file
            IntPtr hIcon = LoadImage(IntPtr.Zero, iconPath, IMAGE_ICON, 0, 0, LR_LOADFROMFILE);
            IntPtr smallHIcon = hIcon;

            if (smallIconPath != null)
                smallHIcon = LoadImage(IntPtr.Zero, smallIconPath, IMAGE_ICON, 0, 0, LR_LOADFROMFILE);

            if (hIcon != IntPtr.Zero && smallHIcon != IntPtr.Zero)
            {
                // Set small and big icon
                SendMessage(hWnd, WM_SETICON, (IntPtr)ICON_BIG, hIcon);
                SendMessage(hWnd, WM_SETICON, (IntPtr)ICON_SMALL, smallHIcon);
            }
            else
            {
                throw new Exception("Failed to load icon.");
            }
        }

        private IntPtr hiddenOwnerWindowHandle;

        public void SetTaskbarIconVisibility(bool visible)
        {
            if (!visible)
            {
                // Create a dummy window (invisible) to act as the owner
                IntPtr hiddenOwner = CreateWindowEx((int)WindowStyles.WS_EX_TOOLWINDOW, "STATIC", "",
                    WS_OVERLAPPEDWINDOW, 0, 0, 0, 0, IntPtr.Zero, IntPtr.Zero, GetModuleHandle(null), IntPtr.Zero);

                hiddenOwnerWindowHandle = hiddenOwner;

                ShowWindow(hiddenOwner, 0); // Ensure it never appears

                if (hWnd != IntPtr.Zero)
                {
                    SetWindowLongPtr(hWnd, GWL_HWNDPARENT, hiddenOwner);
                }
            }
            else
            {
                SetWindowLongPtr(hWnd, GWL_HWNDPARENT, IntPtr.Zero);
            }
        }

        void DisposeHiddenWindow()
        {
            if (hiddenOwnerWindowHandle != IntPtr.Zero)
            {
                DestroyWindow(hiddenOwnerWindowHandle);
                hiddenOwnerWindowHandle = IntPtr.Zero;
            }
        }

        public virtual void Dispose()
        {
            _isRunning = false;
            DestroyWindow(hWnd);
            DisposeHiddenWindow();
        }

        public virtual void UpdateWindowFrame()
        {
        }

        public MultiAccess<Cursor> ActiveCursor = new MultiAccess<Cursor>(Cursor.ARROW);

        protected void UpdateAllowResize(bool allow)
        {
            int toggle = (int)WindowStyles.WS_THICKFRAME | (int)WindowStyles.WS_MAXIMIZEBOX;
            int style = GetWindowLong(hWnd, (int)WindowLongs.GWL_STYLE);

            if (allow)
                style |= toggle;  // Add the styles to allow resizing
            else
                style &= ~toggle; // Remove the styles to prevent resizing

            SetWindowLong(hWnd, (int)WindowLongs.GWL_STYLE, style);
            // SetWindowPos(hWnd, IntPtr.Zero, (int)WindowPosition.x, (int)WindowPosition.y, (int)WindowPosition.x + (int)WindowSize.x, (int)WindowPosition.y + (int)WindowSize.y, 0x0040 | 0x0010); // SWP_FRAMECHANGED | SWP_NOMOVE
        }


        public virtual void OnWindowResized(Vector2 size)
        {
            OnWindowResize?.Invoke(size);
            _isDirty = true;

            RecalcClientBounds();
        }

        public virtual void OnWindowMoved()
        {
            GetWindowRect(hWnd, out var rect);
            WindowPosition = new Vector2(rect.left, rect.top);

            RecalcClientBounds();
        }

        void RecalcClientBounds()
        {
            RECT clientRect;
            GetClientRect(hWnd, out clientRect);

            GetWindowRect(hWnd, out var rect);

            WindowSize = new Vector2(rect.right - rect.left, rect.bottom - rect.top);
            Bounds = new SKRect(0, 0, clientRect.right - clientRect.left, clientRect.bottom - clientRect.top);
        }

        public Vector2 GlobalPointToClient(Vector2 p)
        {
            var point = new POINT() { x = (int)p.x, y = (int)p.y };
            ScreenToClient(hWnd, ref point);
            return new Vector2(point.x, point.y);
        }

        protected IntPtr WindowsProcedure(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case (int)WindowMessages.WM_GETMINMAXINFO:
                    {
                        MINMAXINFO minMaxInfo = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));

                        minMaxInfo.ptMinTrackSize = new POINT() { x = (int)WindowMinSize.x, y = (int)WindowMinSize.y };
                        minMaxInfo.ptMaxTrackSize = new POINT() { x = (int)WindowMaxSize.x, y = (int)WindowMaxSize.y };

                        Marshal.StructureToPtr(minMaxInfo, lParam, true);
                        break;
                    }

                case (int)WindowMessages.WM_USER + 1:
                    if ((int)lParam == (int)WindowMessages.WM_RBUTTONDOWN)
                        OnTrayIconClick?.Invoke(new MouseInputCode(1, 0));
                    if ((int)lParam == (int)WindowMessages.WM_LBUTTONDOWN)
                        OnTrayIconClick?.Invoke(new MouseInputCode(0, 0));
                    if ((int)lParam == (int)WindowMessages.WM_MBUTTONDOWN)
                        OnTrayIconClick?.Invoke(new MouseInputCode(2, 0));
                    if ((int)lParam == (int)WindowMessages.WM_RBUTTONUP)
                        OnTrayIconClick?.Invoke(new MouseInputCode(1, 1));
                    if ((int)lParam == (int)WindowMessages.WM_LBUTTONUP)
                        OnTrayIconClick?.Invoke(new MouseInputCode(0, 1));
                    if ((int)lParam == (int)WindowMessages.WM_MBUTTONUP)
                        OnTrayIconClick?.Invoke(new MouseInputCode(2, 1));
                    return IntPtr.Zero;

                case (int)WindowMessages.WM_INITMENUPOPUP:
                case (int)WindowMessages.WM_SETTINGCHANGE:
                    UpdateSysDarkmode();
                    break;

                // case (uint)WindowMessages.WM_RENDER:
                //     RenderContext.UpdateWindow();
                //     return IntPtr.Zero;

                // Later move to overlay wnd sub class
                // case (int)Win32Helper.WindowMessages.WM_ACTIVATE:
                //     SetAlwaysOnTop();
                //     return IntPtr.Zero;

                case (int)WindowMessages.WM_SIZING:
                case (int)WindowMessages.WM_SIZE:
                    OnWindowResized(new Vector2(wParam, lParam));
                    return IntPtr.Zero;

                case (int)WindowMessages.WM_ENTERSIZEMOVE:
                    _isResizing = true;
                    break;

                case (int)WindowMessages.WM_EXITSIZEMOVE:
                    _isDirty = true;
                    _isResizing = false;
                    break;

                case (int)WindowMessages.WM_MOVING:
                case (int)WindowMessages.WM_MOVE:
                    OnWindowMoved();
                    return IntPtr.Zero;

                case (int)WindowMessages.WM_KEYDOWN:
                    return IntPtr.Zero;
                case (int)WindowMessages.WM_LBUTTONDOWN:
                    MouseAction?.Invoke(new MouseInputCode(0, 0));
                    return IntPtr.Zero;
                case (int)WindowMessages.WM_RBUTTONDOWN:
                    MouseAction?.Invoke(new MouseInputCode(1, 0));
                    return IntPtr.Zero;
                case (int)WindowMessages.WM_MBUTTONDOWN:
                    MouseAction?.Invoke(new MouseInputCode(2, 0));
                    return IntPtr.Zero;
                case (int)WindowMessages.WM_LBUTTONUP:
                    MouseAction?.Invoke(new MouseInputCode(0, 1));
                    return IntPtr.Zero;
                case (int)WindowMessages.WM_RBUTTONUP:
                    MouseAction?.Invoke(new MouseInputCode(1, 1));
                    return IntPtr.Zero;
                case (int)WindowMessages.WM_MBUTTONUP:
                    MouseAction?.Invoke(new MouseInputCode(2, 1));
                    return IntPtr.Zero;

                case (int)WindowMessages.WM_SETCURSOR:
                    SetCursor(LoadCursor(IntPtr.Zero, (int)ActiveCursor.Value));
                    return IntPtr.Zero;

                case (int)WindowMessages.WM_CLOSE:
                    _isRunning = false;
                    Dispose();
                    return IntPtr.Zero;

                case (int)WindowMessages.WM_DESTROY:
                    PostQuitMessage(0);
                    return IntPtr.Zero;
            }
            return DefWindowProcW(hWnd, msg, wParam, lParam);
        }

        public RECT? GetMonitorRect(int monitorIndex)
        {
            RECT rect = new RECT();
            int count = 0;

            MonitorEnumDelegate callback = (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData) =>
            {
                if (count == monitorIndex)
                {
                    rect = lprcMonitor;
                    return false; // stop enumeration once we've got our monitor
                }
                count++;
                return true;
            };

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero);
            return rect;
        }

        #region Windows

        public const int WM_SETICON = 0x0080;
        public const int ICON_SMALL = 0;
        public const int ICON_BIG = 1;

        public const uint IMAGE_ICON = 1;
        public const uint LR_LOADFROMFILE = 0x00000010;

        public const int CW_USEDEFAULT = unchecked((int)0x80000000);

        public const int GWL_HWNDPARENT = -8;

        public const int HWND_TOPMOST = -1;
        public const int HWND_NOTOPMOST = -2;

        public const int WS_OVERLAPPEDWINDOW =
            (int)WindowStyles.WS_OVERLAPPED |
            (int)WindowStyles.WS_CAPTION |
            (int)WindowStyles.WS_SYSMENU |
            (int)WindowStyles.WS_THICKFRAME |
            (int)WindowStyles.WS_MINIMIZEBOX |
            (int)WindowStyles.WS_MAXIMIZEBOX;

        public const int WS_NATIVE =
            (int)WindowStyles.WS_OVERLAPPED |
            (int)WindowStyles.WS_CAPTION |
            (int)WindowStyles.WS_SYSMENU |
            (int)WindowStyles.WS_MINIMIZEBOX;


        const int HTLEFT = 10;
        const int HTRIGHT = 11;
        const int HTTOP = 12;
        const int HTBOTTOM = 15;
        const int HTTOPLEFT = 13;
        const int HTBOTTOMLEFT = 14;
        const int HTTOPRIGHT = 16;
        const int HTBOTTOMRIGHT = 17;
        const int HTCLIENT = 1;

        [DllImport("dwmapi.dll")]
        public static extern void DwmFlush();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
        protected static int GET_X_LPARAM(IntPtr lParam) => (int)(lParam.ToInt32() & 0xFFFF);
        protected static int GET_Y_LPARAM(IntPtr lParam) => (int)((lParam.ToInt32() >> 16) & 0xFFFF);

        [DllImport("user32.dll", SetLastError = true)]
        protected static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("uxtheme.dll", EntryPoint = "#135")]
        protected static extern int ShouldSystemUseDarkMode();

        [DllImport("uxtheme.dll", EntryPoint = "#136")]
        protected static extern void AllowDarkModeForWindow(IntPtr hWnd, bool allow);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        protected static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll", SetLastError = true)]
        protected static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        protected static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        protected static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        protected static extern IntPtr LoadImage(IntPtr hInstance, string lpszName, uint uType, int cx, int cy, uint fuLoad);

        [DllImport("shell32.dll")]
        protected static extern bool Shell_NotifyIconA(uint dwMessage, ref NOTIFYICONDATAA lpData);

        [DllImport("user32.dll")]
        protected static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        protected static extern IntPtr DefWindowProcW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        protected static extern void PostQuitMessage(int nExitCode);

        [DllImport("user32.dll", SetLastError = true)]
        protected static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        protected static extern IntPtr SetCursor(IntPtr hCursor);

        [DllImport("user32.dll")]
        protected static extern IntPtr LoadCursor(IntPtr hInstance, int lpCursorName);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        protected static extern ushort RegisterClassExA(ref WNDCLASSEX lpwcx);

        [DllImport("user32.dll")]
        protected static extern bool GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        [DllImport("user32.dll")]
        protected static extern bool TranslateMessage([In] ref MSG lpMsg);
        [DllImport("user32.dll")]
        protected static extern IntPtr DispatchMessage([In] ref MSG lpmsg);

        [DllImport("dwmapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        protected static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

        [DllImport("user32.dll")]
        protected static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        protected static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        protected static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("gdi32.dll", SetLastError = true)]
        protected static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll", SetLastError = true)]
        protected static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll", SetLastError = true)]
        protected static extern bool DeleteObject(IntPtr hObject);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst,
             ref POINT pptDst, ref SIZE psize, IntPtr hdcSrc,
             ref POINT pptSrc, uint crKey, ref BLENDFUNCTION pblend, uint dwFlags);

        [DllImport("user32.dll")]
        protected static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("gdi32.dll", SetLastError = true)]
        protected static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("user32.dll", SetLastError = true)]
        protected static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll", SetLastError = true)]
        protected static extern IntPtr CreateDIBSection(IntPtr hdc, [In] ref BITMAPINFO pbmi,
             uint iUsage, out IntPtr ppvBits, IntPtr hSection, uint dwOffset);

        [DllImport("user32.dll")]
        protected static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip,
    MonitorEnumDelegate lpfnEnum, IntPtr dwData);

        protected delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor,
            ref RECT lprcMonitor, IntPtr dwData);


        [DllImport("user32.dll")]
        protected static extern int GetSystemMetrics(int nIndex);

        [DllImport("dwmapi.dll")]
        protected static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

        [DllImport("dwmapi.dll")]
        protected static extern int DwmSetWindowAttribute(
            IntPtr hwnd,
            uint attr,
            ref int attrValue,
            int attrSize);

        public enum DWMWINDOWATTRIBUTE
        {
            DWMWA_SYSTEMBACKDROP_TYPE = 38,
            DWMWA_MICA_EFFECT = 1029,      // Dark mode Mica
            DWMWA_CAPTION_COLOR = 35,      // Title bar color
            DWMWA_TEXT_COLOR_SYSTEM = 36,   // System text color adaptation
            DWMWA_USE_IMMERSIVE_DARK_MODE = 20
        }


        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        protected static extern IntPtr CreateWindowEx(
            int dwExStyle,
            string lpClassName,
            string lpWindowName,
            int dwStyle,
            int x,
            int y,
            int nWidth,
            int nHeight,
            IntPtr hWndParent,
            IntPtr hMenu,
            IntPtr hInstance,
            IntPtr lpParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        protected static extern IntPtr CreateWindowExA(
            int dwExStyle,
            string lpClassName,
            string lpWindowName,
            int dwStyle,
            int x,
            int y,
            int nWidth,
            int nHeight,
            IntPtr hWndParent,
            IntPtr hMenu,
            IntPtr hInstance,
            IntPtr lpParam);

        #endregion
    }

    public enum DWM_SYSTEMBACKDROP_TYPE
    {
        DWMSBT_AUTO = 1,
        DWMSBT_NONE = 2,
        DWMSBT_MAINWINDOW = 3,
        DWMSBT_TRANSIENTWINDOW = 4,
        DWMSBT_TABBEDWINDOW = 5
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MARGINS
    {
        public int cxLeftWidth;
        public int cxRightWidth;
        public int cyTopHeight;
        public int cyBottomHeight;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MINMAXINFO
    {
        public POINT ptReserved;
        public POINT ptMaxSize;
        public POINT ptMaxPosition;
        public POINT ptMinTrackSize;
        public POINT ptMaxTrackSize;
    }

    public delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    public struct PAINTSTRUCT
    {
        public IntPtr hdc;
        public bool fErase;
        public RECT rcPaint;
        public bool fRestore;
        public bool fIncUpdate;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] rgbReserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int left, top, right, bottom;
    }

    [Flags]
    public enum SetWindowPosFlags : uint
    {
        SWP_NOMOVE = 0x0002,
        SWP_NOSIZE = 0x0001,
        SWP_SHOWWINDOW = 0x0040,
        SWP_NOZORDER = 0x0004,
        SWP_NOACTIVATE = 0x0010
    }

    public enum WindowStyles : long
    {
        WS_POPUP = unchecked((int)0x80000000),
        WS_EX_LAYERED = 0x00080000,
        WS_EX_APPWINDOW = 0x00040000,
        WS_EX_TOOLWINDOW = 0x00000080,
        WS_VISIBLE = 0x10000000L,
        WS_OVERLAPPED = 0x00000000,
        WS_CAPTION = 0x00C00000,
        WS_SYSMENU = 0x00080000,
        WS_THICKFRAME = 0x00040000,
        WS_MINIMIZEBOX = 0x00020000,
        WS_MAXIMIZEBOX = 0x00010000,
        WS_BORDER = 0x00800000
    }

    public enum WindowLongs : int
    {
        GWL_EXSTYLE = -20,
        GWL_STYLE = -16
    }

    public enum NIF : uint
    {
        NIM_ADD = 0x00000000,
        NIM_DELETE = 0x00000002,
        NIF_MESSAGE = 0x00000001,
        NIF_ICON = 0x00000002,
        NIF_TIP = 0x00000004
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct NOTIFYICONDATAA
    {
        public int cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct WNDCLASSEX
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
        [MarshalAs(UnmanagedType.LPTStr)]
        public string lpszMenuName;
        [MarshalAs(UnmanagedType.LPTStr)]
        public string lpszClassName;
        public IntPtr hIconSm;
    }

    public struct MouseInputCode
    {
        public int button; // 0: left, 1: right, 2: middle
        public int state; // 0: down, 1: up

        public MouseInputCode(int btn, int state)
        {
            this.button = btn;
            this.state = state;
        }
    }

    public enum Cursor : int
    {
        ARROW = 32512,
        IBEAM = 32513,
        WAIT = 32514,
        BLOCK = 32648,
        HAND = 32649
    }

    public enum WindowMessages : uint
    {
        WM_SETTINGCHANGE = 0x001A,
        WM_INITMENUPOPUP = 0x0117,
        WM_NCHITTEST = 0x0084,

        WM_GETMINMAXINFO = 0x24,

        WM_ACTIVATE = 0x0006,
        WM_DESTROY = 0x0002,
        WM_PAINT = 0x000F,
        WM_SIZE = 0x0005,
        WM_KEYDOWN = 0x0100,
        WM_MOUSEMOVE = 0x0200,

        WM_LBUTTONDOWN = 0x0201,
        WM_LBUTTONUP = 0x0202,
        WM_MBUTTONDOWN = 0x0207,
        WM_MBUTTONUP = 0x0208,
        WM_RBUTTONDOWN = 0x0204,
        WM_RBUTTONUP = 0x0205,

        WM_MOUSEHOVER = 0x02A1,
        WM_MOUSELEAVE = 0x02A3,

        WM_DROPFILES = 0x0233,
        WM_SETCURSOR = 0x0020,
        WM_TIMER = 0x0113,
        WM_QUIT = 0x0012,
        WM_SETICON = 0x80,
        WM_USER = 0x0400,
        WM_COMMAND = 0x0111,
        WM_MENUDRAG = 0x123,
        WM_CLOSE = 0x0010,

        WM_MOVING = 0x0216,
        WM_MOVE = 0x0003,
        WM_SIZING = 0x0214,
        WM_EXITSIZEMOVE = 0x0232,
        WM_ENTERSIZEMOVE = 0x0231
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public UIntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public POINT pt;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SIZE
    {
        public int cx;
        public int cy;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BLENDFUNCTION
    {
        public byte BlendOp;
        public byte BlendFlags;
        public byte SourceConstantAlpha;
        public byte AlphaFormat;
    }

    // Alpha blend options used in BLENDFUNCTION
    public enum AlphaBlendOptions : byte
    {
        AC_SRC_OVER = 0x00,
        AC_SRC_ALPHA = 0x01
    }
}