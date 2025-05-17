using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using FenUISharp.Components;
using FenUISharp.Mathematics;
using FenUISharp.Themes;
using FenUISharp.WinFeatures;
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

        public Vector2 WindowMinSize { get; set; } = new Vector2(100, 100);
        public Vector2 WindowMaxSize { get; set; } = new Vector2(float.MaxValue, float.MaxValue);

        protected bool _allowResize = true;
        public bool AllowResizing { get => _allowResize; set { _allowResize = value; UpdateAllowResize(_allowResize); } }

        private bool _hasSystemMenu = true;
        public bool HasSystemMenu { get => _hasSystemMenu; set { _hasSystemMenu = value; UpdateHasSysMenu(_hasSystemMenu); } }

        private bool _hasMaximizeButton = true;
        public bool CanMinimize { get => _hasMaximizeButton; set { _hasMaximizeButton = value; UpdateHasMaximizeButton(_hasMaximizeButton); } }

        private bool _hasMinimizeButton = true;
        public bool CanMaximize { get => _hasMinimizeButton; set { _hasMinimizeButton = value; UpdateHasMinimizeButton(_hasMinimizeButton); } }

        private bool _showInTaskbar = true;
        public bool ShowInTaskbar { get => _showInTaskbar; set { _showInTaskbar = value; SetTaskbarIconVisibility(_showInTaskbar); } }

        protected bool _hasTitlebar = false;

        public bool HideWindowOnClose { get; set; } = false;
        public bool PauseUpdateLoopWhenLoseFocus { get; set; } = false;
        public bool PauseUpdateLoopWhenHidden { get; set; } = false;

        protected int _refreshRate = 60;
        public int RefreshRate { get => _refreshRate; set { _refreshRate = value; RenderContext?.OnWindowPropertyChanged(); } }
        public double DeltaTime { get; private set; }
        public double Time { get; private set; }

        public Vector2 ClientMousePosition
        {
            get
            {
                // var mousePos = new POINT() { x = (int)GlobalHooks.MousePosition.x, y = (int)GlobalHooks.MousePosition.y };
                // ScreenToClient(hWnd, ref mousePos);
                return GlobalPointToClient(new Vector2(GlobalHooks.MousePosition.x, GlobalHooks.MousePosition.y));
            }
        }

        protected bool _sysDarkMode = false;
        public bool SystemDarkMode { get => _sysDarkMode; set { _sysDarkMode = value; UpdateSysDarkmode(); } }

        public bool DebugDisplayBounds { get; set; } = false;
        public bool DebugDisplayAreaCache { get; set; } = false;

        public ThemeManager WindowThemeManager { get; private set; }

        public SKRect Bounds { get; private set; }

        public IntPtr hWnd { get; private set; }

        public DragDropHandler DropTarget { get; private set; }
        public FRenderContext RenderContext { get; private set; }

        #endregion

        #region Actions

        public Action<MouseInputCode> MouseAction { get; set; }
        public Action<MouseInputCode> TrayMouseAction { get; set; }

        public Action OnBeginRender { get; set; }
        public Action OnEndRender { get; set; }
        public Action OnUpdate { get; set; }

        public Action OnFocusLost { get; set; }
        public Action OnFocusGained { get; set; }
        public Action<Vector2> OnWindowResize { get; set; }
        public Action<Vector2> OnWindowMoved { get; set; }
        public Action OnWindowClose { get; set; }
        public Action OnWindowDestroy { get; set; }

        public Action<char> Char { get; set; }

        #endregion

        #region Private

        protected volatile List<UIComponent> UiComponents = new List<UIComponent>();

        private readonly WndProcDelegate _wndProcDelegate;
        protected bool _alwaysOnTop;
        protected volatile bool _isDirty = false;
        protected volatile bool _fullRedraw = false;
        volatile bool _isResizing = false;

        volatile bool _stopRunningFlag = false;
        volatile bool _isRunning = false;
        volatile bool _windowCloseFlag = false;

        protected bool _lastIsWindowFocused = false;
        public bool IsWindowFocused { get; protected set; } = false;
        public bool IsWindowShown { get; protected set; } = false;

        private Thread _renderThread;
        private RenderContextType _startWithType;

        private MouseInputCode? mouseInputActionFlag = null;
        private MouseInputCode? trayMouseActionFlag = null;

        #endregion

        #region Constructors

        public enum RenderContextType
        {
            Software,
            DirectX
        }

        public Window(
            string title, string className, RenderContextType type,
            Vector2? windowSize = null, Vector2? windowPosition = null,
            bool alwaysOnTop = false, bool hideTaskbarIcon = false, bool hasTitlebar = true
        )
        {
            if (!FenUI.HasBeenInitialized) throw new Exception("FenUI has to be initialized before creating a window.");

            WindowTitle = title;
            WindowClass = className;

            WindowSize = (windowSize == null || windowSize.Value.Magnitude < 1) ? new Vector2(400, 300) : windowSize.Value;
            WindowPosition = (windowPosition == null) ? new Vector2(0, 0) : windowPosition.Value;

            _wndProcDelegate = StaticWndProc;
            _hasTitlebar = hasTitlebar;

            // Create window and handle
            hWnd = CreateWin32Window(RegisterClass(WindowClass), WindowSize, windowPosition);
            if (hWnd == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                throw new Exception($"Window creation failed: error {error}");
            }

            // Making sure every instance of the Window class has its own WndProc
            GCHandle gch = GCHandle.Alloc(this);
            SetWindowLongPtr(hWnd, -21 /* GWLP_USERDATA */, GCHandle.ToIntPtr(gch));

            WindowThemeManager = new ThemeManager(Resources.GetTheme("default-dark"));

            // Pre initialize OLE DragDrop
            DragDropRegistration.Initialize();

            _alwaysOnTop = alwaysOnTop;
            SetAlwaysOnTop(_alwaysOnTop);

            _startWithType = type;

            SetTaskbarIconVisibility(!hideTaskbarIcon);
            RecalcClientBounds();
        }

        #endregion

        public virtual void CreateAndUpdateRenderContext(RenderContextType type)
        {
            switch (type)
            {
                case RenderContextType.Software: RenderContext = new SoftwareRenderContext(this); break;
                case RenderContextType.DirectX: RenderContext = new DirectRenderContext(this); break;
            }
        }

        protected virtual void UpdateSysDarkmode()
        {
            _isDirty = true;

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

                // Initialize FRenderContext
                CreateAndUpdateRenderContext(_startWithType);

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

                    if (!_isRunning) break;

                    if (_stopRunningFlag) _isRunning = false;
                    Thread.Sleep(1); // Prevent too high cpu usage
                }
            }
        }

        private async void RenderLoop()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            double frameInterval = 1000.0 / RefreshRate;
            double nextFrameTime = 0;
            double previousFrameTime = 0;

            while (_isRunning)
            {
                double currentTime = stopwatch.Elapsed.TotalMilliseconds;
                IsWindowShown = IsWindowVisible(hWnd);

                if (!IsWindowFocused && PauseUpdateLoopWhenLoseFocus || !IsWindowShown && PauseUpdateLoopWhenHidden)
                {
                    previousFrameTime = currentTime; // Make sure delta time doesn't go too crazy when updates are resumed
                    OnWindowUpdateCall(true);

                    await Task.Delay(100); continue;
                }

                if (currentTime >= nextFrameTime)
                {
                    DeltaTime = (float)(currentTime - previousFrameTime) / 1000.0f;
                    previousFrameTime = currentTime;

                    OnWindowUpdateCall();

                    if (IsNextFrameRendering())
                    {
                        if (_fullRedraw)
                            UiComponents.ForEach(x => x.RecursiveInvalidate());

                        // Console.WriteLine("test");

                        OnBeginRender?.Invoke();
                        RenderFrame();
                        OnEndRender?.Invoke();

                        _isDirty = false;
                        _fullRedraw = false;
                        UiComponents.ForEach(x => x.GloballyInvalidated = false);
                    }

                    nextFrameTime = currentTime + frameInterval;
                }

                // await Task.Delay(needsRender ? 1 : 16); // If not rendering, sleep longer (saves CPU); Edit: Not good, since the Update method inside the FUIComponents gets skipped then
                await Task.Delay(1);
            }

            new List<UIComponent>(UiComponents).ForEach(x => x?.Dispose());
        }

        protected virtual void OnRenderFrame(SKSurface surface) { }
        protected virtual void OnAfterRenderFrame(SKSurface surface) { }

        private readonly object _renderLock = new object();

        protected virtual void RenderFrame()
        {
            if (_isResizing) return;

            lock (_renderLock)
            {
                var _canvas = RenderContext.BeginDraw().Canvas;
                if (_canvas == null) return;

                int notClipped = _canvas.Save();
                using var clipPath = GetDirtyClipPath();
                _canvas.ClipPath(clipPath);

                if(RenderContext.Surface != null)
                    OnRenderFrame(RenderContext.Surface);

                foreach (var component in OrderUIComponents(UiComponents))
                {
                    if (!RMath.IsRectPartiallyInside(component.Transform.FullBounds, clipPath)) continue;

                    int savedBeforeComponent = _canvas.Save();
                    if (component.Enabled && component.Transform.Parent == null)
                        component.DrawToScreen(_canvas);

                    if (DebugDisplayBounds)
                    {
                        _canvas.DrawRect(component.Transform.Bounds, new SKPaint() { IsStroke = true, Color = SKColors.Red });
                        _canvas.DrawRect(component.InteractionBounds, new SKPaint() { IsStroke = true, Color = SKColors.Green });
                    }
                    _canvas.RestoreToCount(savedBeforeComponent);
                }

                _canvas.RestoreToCount(notClipped);

                if (DebugDisplayAreaCache)
                {
                    using (var paint = new SKPaint() { Color = SKColors.Red.WithAlpha(1) })
                        _canvas.DrawRect(Bounds, paint);
                }

                if(RenderContext.Surface != null)
                    OnAfterRenderFrame(RenderContext.Surface);

                RenderContext.EndDraw();
            }
        }

        public SKPath GetDirtyClipPath()
        {
            var clipPath = new SKPath();
            if (_isDirty)
            {
                clipPath.AddRect(Bounds);
                return clipPath;
            }

            foreach (var component in UiComponents)
            {
                if (component.SelfInvalidated)
                {
                    var bounds = component.Transform.FullBounds;
                    bounds.Inflate(1, 1);
                    clipPath.AddRect(bounds);
                }
            }

            return clipPath;
        }

        public List<UIComponent> OrderUIComponents(List<UIComponent> uiComponents)
        {
            return uiComponents.AsEnumerable().OrderBy(e => e.Transform.ZIndex).ThenBy(e => e.Transform.CreationIndex).ToList();
        }

        private void OnWindowUpdateCall(bool isPaused = false)
        {
            if (!isPaused)
                OnUpdate?.Invoke();

            Time += DeltaTime;

            foreach (char c in _queuedInputChars) Char?.Invoke(c);
            _queuedInputChars.Clear();

            if (_onEndResizeFlag)
            {
                _onEndResizeFlag = false;
                OnEndResize();
            }

            if (_lastIsWindowFocused != IsWindowFocused)
            {
                if (IsWindowFocused) OnFocusGained?.Invoke();
                else OnFocusLost?.Invoke();
            }
            _lastIsWindowFocused = IsWindowFocused;

            if (_onWindowMovedFlag)
            {
                _onWindowMovedFlag = false;
                WindowMoved();
                OnWindowMoved?.Invoke(WindowPosition);
            }

            if (_isResizing)
                OnWindowResize?.Invoke(WindowSize);

            if (_windowCloseFlag)
            {
                _windowCloseFlag = false;
                _isRunning = false;
                OnWindowClose?.Invoke();
                Dispose();
            }

            if (mouseInputActionFlag != null)
            {
                MouseAction?.Invoke(mouseInputActionFlag.Value);
                mouseInputActionFlag = null;
            }
            if (trayMouseActionFlag != null)
            {
                TrayMouseAction?.Invoke(trayMouseActionFlag.Value);
                trayMouseActionFlag = null;
            }
        }

        public void Redraw() => _isDirty = true;
        public void FullRedraw()
        {
            _isDirty = true;
            _fullRedraw = true;
            RenderContext.RecreateSurface();
        }

        public void AddUIComponent(UIComponent component)
        {
            if (component == null) return;
            if (UiComponents.Contains(component)) return;

            component.WindowRoot = this;
            UiComponents.Add(component);
        }

        public void RemoveUIComponent(UIComponent component)
        {
            if (component == null || !UiComponents.Contains(component)) return;

            // component.WindowRoot = null; // Don't do, could break some stuff
            UiComponents.Remove(component);
        }

        public void DestroyUIComponent(UIComponent component)
        {
            if (component == null) return;

            component.Dispose();
            UiComponents.Remove(component);
        }

        public bool IsNextFrameRendering()
        {
            return UiComponents.Any(x => x.GloballyInvalidated && x.Enabled && x.Visible && x.Transform.Parent == null && !x.IsOutsideClip()) || DebugDisplayAreaCache || _isDirty || _fullRedraw;
        }

        public List<UIComponent> GetUIComponents() => UiComponents;

        public void SetWindowVisibility(bool visible) => ShowWindow(hWnd, visible ? 1 : 0);

        public void SetAlwaysOnTop(bool alwaysOnTop)
        {
            _alwaysOnTop = alwaysOnTop;
            SetWindowPos(hWnd, alwaysOnTop ? HWND_TOPMOST : HWND_NOTOPMOST, 0, 0, 0, 0, (uint)SetWindowPosFlags.SWP_NOMOVE | (uint)SetWindowPosFlags.SWP_NOSIZE);
        }

        protected virtual WNDCLASSEX RegisterClass(string className)
        {
            WindowClass = className;
            WNDCLASSEX wndClass = new WNDCLASSEX
            {
                cbSize = (uint)Marshal.SizeOf(typeof(WNDCLASSEX)),
                style = 0x0020,
                lpfnWndProc = _wndProcDelegate,
                hInstance = Marshal.GetHINSTANCE(typeof(FenUI).Module),
                lpszClassName = className
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
            _showInTaskbar = visible;
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
            DestroyWindow(hWnd);
            _stopRunningFlag = false;
            DisposeHiddenWindow();

            // Console.WriteLine("Destroyed " + Thread.CurrentThread.ManagedThreadId);
        }

        public void DisposeAndDestroyWindow()
        {
            SetWindowVisibility(false); // To make sure the window disappears instantly (or as fast as possible) hide it first

            Dispose();
            _stopRunningFlag = true;
        }

        public virtual void UpdateWindowFrame()
        {
        }

        public MultiAccess<Cursor> ActiveCursor = new MultiAccess<Cursor>(Cursor.ARROW);

        private void UpdateAllowResize(bool allowResize)
        {
            int toggle = (int)WindowStyles.WS_THICKFRAME;
            SetWindowStyle(toggle, allowResize);
        }

        protected void UpdateHasSysMenu(bool allow)
        {
            int toggle = (int)WindowStyles.WS_SYSMENU;
            SetWindowStyle(toggle, allow);
        }

        protected void UpdateHasMaximizeButton(bool allow)
        {
            int toggle = (int)WindowStyles.WS_MAXIMIZEBOX;
            SetWindowStyle(toggle, allow);
        }

        protected void UpdateHasMinimizeButton(bool allow)
        {
            int toggle = (int)WindowStyles.WS_MINIMIZEBOX;
            SetWindowStyle(toggle, allow);
        }

        protected void SetWindowStyle(int style, bool enabled)
        {
            int styl = GetWindowLong(hWnd, (int)WindowLongs.GWL_STYLE);
            if (enabled)
                styl |= style;
            else
                styl &= ~style;
            SetWindowLong(hWnd, (int)WindowLongs.GWL_STYLE, styl);
        }

        // UI Thread methods
        protected virtual void WindowMoved() { }

        protected virtual void OnEndResize()
        {
            RenderContext?.OnEndResize();
            new List<UIComponent>(UiComponents).ForEach(x =>
            {
                x?.Invalidate();
                x?.Transform.UpdateLayout();
            });
        }

        // Main thread methods

        bool _onEndResizeFlag = false;
        Vector2 oldSize;
        private void OnEndRsz()
        {
            _onEndResizeFlag = true;
        }

        public virtual void WindowRzd(Vector2 size)
        {
            if (!_isResizing && size != oldSize)
                OnEndRsz();
            oldSize = size;

            _isDirty = true;

            if (RenderContext != null)
                RenderContext.OnResize(size);

            RecalcClientBounds();
        }

        private bool _onWindowMovedFlag = false;
        private void OnWindowMvd()
        {
            _onWindowMovedFlag = true;
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

        public void SetWindowSize(Vector2 size)
        {
            WindowSize = size;
            IntPtr zOrder = _alwaysOnTop ? HWND_TOPMOST : HWND_NOTOPMOST;
            SetWindowPos(hWnd, zOrder, 0, 0, (int)size.x, (int)size.y,
                (int)SetWindowPosFlags.SWP_NOACTIVATE | (int)SetWindowPosFlags.SWP_NOMOVE);

            RecalcClientBounds();
        }

        public void SetWindowPosition(Vector2 pos)
        {
            WindowPosition = pos;
            IntPtr zOrder = _alwaysOnTop ? HWND_TOPMOST : HWND_NOTOPMOST;
            SetWindowPos(hWnd, zOrder, (int)pos.x, (int)pos.y, 0, 0,
                (int)SetWindowPosFlags.SWP_NOACTIVATE | (int)SetWindowPosFlags.SWP_NOSIZE);

            RecalcClientBounds();
        }

        private static IntPtr StaticWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            IntPtr ptr = GetWindowLongPtrA(hWnd, -21);

            if (ptr != IntPtr.Zero)
            {
                GCHandle gch = GCHandle.FromIntPtr(ptr);

                if (gch.Target != null) {
                    var instance = (Window)gch.Target;
                    return instance.WindowsProcedure(hWnd, msg, wParam, lParam);
                }
            }

            return DefWindowProcW(hWnd, msg, wParam, lParam);
        }

        private Queue<char> _queuedInputChars = new();

        protected IntPtr WindowsProcedure(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case 0x0102:
                    _queuedInputChars.Enqueue((char)wParam);
                    break;

                case (int)WindowMessages.WM_GETMINMAXINFO:
                    {
                        MINMAXINFO minMaxInfo = Marshal.PtrToStructure<MINMAXINFO>(lParam);

                        minMaxInfo.ptMinTrackSize = new POINT() { x = (int)WindowMinSize.x, y = (int)WindowMinSize.y };
                        minMaxInfo.ptMaxTrackSize = new POINT() { x = (int)WindowMaxSize.x, y = (int)WindowMaxSize.y };

                        Marshal.StructureToPtr(minMaxInfo, lParam, true);
                        break;
                    }

                case (int)WindowMessages.WM_USER + 1:
                    if ((int)lParam == (int)WindowMessages.WM_RBUTTONUP)
                        trayMouseActionFlag = new MouseInputCode(1, 1);
                    if ((int)lParam == (int)WindowMessages.WM_LBUTTONUP)
                        trayMouseActionFlag = new MouseInputCode(0, 1);
                    return IntPtr.Zero;

                case (int)WindowMessages.WM_INITMENUPOPUP:
                case (int)WindowMessages.WM_SETTINGCHANGE:
                    UpdateSysDarkmode();
                    break;

                case (int)WindowMessages.WM_SIZING:
                case (int)WindowMessages.WM_SIZE:
                    WindowRzd(new Vector2(wParam, lParam));
                    return IntPtr.Zero;

                case (int)WindowMessages.WM_ENTERSIZEMOVE:
                    _isResizing = true;
                    break;

                case (int)WindowMessages.WM_EXITSIZEMOVE:
                    _isDirty = true;
                    _isResizing = false;
                    OnEndRsz();
                    break;

                case (int)WindowMessages.WM_KILLFOCUS:
                    IsWindowFocused = false;
                    return IntPtr.Zero;
                case (int)WindowMessages.WM_SETFOCUS:
                    _isDirty = true;
                    IsWindowFocused = true;
                    return IntPtr.Zero;

                case (int)WindowMessages.WM_MOVING:
                case (int)WindowMessages.WM_MOVE:
                    OnWindowMvd();
                    return IntPtr.Zero;

                case (int)WindowMessages.WM_NCDESTROY:
                    IntPtr ptr = GetWindowLongPtrA(hWnd, -21);
                    if (ptr != IntPtr.Zero)
                    {
                        GCHandle.FromIntPtr(ptr).Free();
                        SetWindowLongPtr(hWnd, -21, IntPtr.Zero);
                    }
                    break;

                case (int)WindowMessages.WM_KEYDOWN:
                    return IntPtr.Zero;
                case (int)WindowMessages.WM_LBUTTONDOWN:
                    mouseInputActionFlag = new MouseInputCode(0, 0);
                    return IntPtr.Zero;
                case (int)WindowMessages.WM_RBUTTONDOWN:
                    mouseInputActionFlag = new MouseInputCode(1, 0);
                    return IntPtr.Zero;
                case (int)WindowMessages.WM_MBUTTONDOWN:
                    mouseInputActionFlag = new MouseInputCode(2, 0);
                    return IntPtr.Zero;
                case (int)WindowMessages.WM_LBUTTONUP:
                    mouseInputActionFlag = new MouseInputCode(0, 1);
                    return IntPtr.Zero;
                case (int)WindowMessages.WM_RBUTTONUP:
                    mouseInputActionFlag = new MouseInputCode(1, 1);
                    return IntPtr.Zero;
                case (int)WindowMessages.WM_MBUTTONUP:
                    mouseInputActionFlag = new MouseInputCode(2, 1);
                    return IntPtr.Zero;

                case (int)WindowMessages.WM_SETCURSOR:
                    int hitTest = lParam.ToInt32() & 0xFFFF;
                    if (hitTest == HTLEFT || hitTest == HTRIGHT ||
                        hitTest == HTTOP || hitTest == HTTOPLEFT ||
                        hitTest == HTTOPRIGHT || hitTest == HTBOTTOM ||
                        hitTest == HTBOTTOMLEFT || hitTest == HTBOTTOMRIGHT)
                        return DefWindowProcW(hWnd, msg, wParam, lParam);
                    else
                        SetCursor(LoadCursor(IntPtr.Zero, (int)ActiveCursor.Value));
                    return IntPtr.Zero;

                case (int)WindowMessages.WM_CLOSE:
                    SetWindowVisibility(false); // Make sure the window is closes seemingly faster by hiding it first
                    if (!HideWindowOnClose)
                        _windowCloseFlag = true;
                    return IntPtr.Zero;

                case (int)WindowMessages.WM_DESTROY:
                    OnWindowDestroy?.Invoke();
                    PostQuitMessage(0);
                    return IntPtr.Zero;
            }
            return DefWindowProcW(hWnd, msg, wParam, lParam);
        }

        public RECT GetMonitorRect(int monitorIndex)
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

        protected const int WM_SETICON = 0x0080;
        protected const int ICON_SMALL = 0;
        protected const int ICON_BIG = 1;

        protected const uint IMAGE_ICON = 1;
        protected const uint LR_LOADFROMFILE = 0x00000010;

        protected const int CW_USEDEFAULT = unchecked((int)0x80000000);

        protected const int GWL_HWNDPARENT = -8;

        protected const int HWND_TOPMOST = -1;
        protected const int HWND_NOTOPMOST = -2;

        const int WM_SETCURSOR = 0x0020;
        const int HTLEFT = 10;
        const int HTRIGHT = 11;
        const int HTTOP = 12;
        const int HTTOPLEFT = 13;
        const int HTTOPRIGHT = 14;
        const int HTBOTTOM = 15;
        const int HTBOTTOMLEFT = 16;
        const int HTBOTTOMRIGHT = 17;
        const int HTCLIENT = 1;

        protected const uint LWA_COLORKEY = 0x00000001;
        protected const uint LWA_ALPHA = 0x00000002;

        protected const uint MONITOR_DEFAULTTONEAREST = 2;

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

        public const int WS_NOTITLEBAR =
            (int)WindowStyles.WS_OVERLAPPED |
            (int)WindowStyles.WS_POPUP |
            (int)WindowStyles.WS_THICKFRAME;

        static ushort LOWORD(IntPtr value) => (ushort)((long)value & 0xFFFF);
        static ushort HIWORD(IntPtr value) => (ushort)(((long)value >> 16) & 0xFFFF);


        [DllImport("user32.dll")]
        protected static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll", SetLastError = true)]
        protected static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [StructLayout(LayoutKind.Sequential)]
        protected struct MONITORINFO
        {
            public uint cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [DllImport("dwmapi.dll")]
        protected static extern void DwmFlush();

        [DllImport("user32.dll", SetLastError = true)]
        protected static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
        protected static int GET_X_LPARAM(IntPtr lParam) => (int)(lParam.ToInt32() & 0xFFFF);
        protected static int GET_Y_LPARAM(IntPtr lParam) => (int)((lParam.ToInt32() >> 16) & 0xFFFF);

        [DllImport("user32.dll", SetLastError = true)]
        protected static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        protected static extern IntPtr GetWindowLongPtrA(IntPtr hWnd, int nIndex);

        [DllImport("uxtheme.dll", EntryPoint = "#135")]
        protected static extern int ShouldSystemUseDarkMode();

        [DllImport("uxtheme.dll", EntryPoint = "#136")]
        protected static extern void AllowDarkModeForWindow(IntPtr hWnd, bool allow);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        protected static extern IntPtr GetModuleHandle(string? lpModuleName);

        [DllImport("user32.dll", SetLastError = true)]
        protected static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        protected static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        protected static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        protected static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

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

        [DllImport("user32.dll", SetLastError = true)]
        protected static extern bool GetCursorPos(out POINT pt);

        [DllImport("user32.dll")]
        protected static extern bool IsWindowVisible(IntPtr hWnd);

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

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

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

        protected enum DWMWINDOWATTRIBUTE
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

        public int Width => right - left;
        public int Height => bottom - top;
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

    public enum WindowMessages : uint
    {
        WM_SETTINGCHANGE = 0x001A,
        WM_INITMENUPOPUP = 0x0117,
        WM_NCHITTEST = 0x0084,

        WM_DPICHANGED = 0x02E0,

        WM_SETFOCUS = 0x0007,
        WM_KILLFOCUS = 0x0008,

        WM_NCDESTROY = 0x0082,

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