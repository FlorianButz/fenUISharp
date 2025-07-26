using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using FenUISharp.Mathematics;
using FenUISharp.Native;
using FenUISharp.Objects;
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

        protected FenUISharp.Objects.ModelViewPane RootViewPane { get; private set; }

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
        public KeyboardInputManager WindowKeyboardInput { get; private set; }

        public SKRect Bounds { get; private set; }

        public IntPtr hWnd { get; private set; }
        private IntPtr devicesChangedHandle;

        public DragDropHandler DropTarget { get; private set; }
        public FRenderContext RenderContext { get; private set; }

        public Dispatcher Dispatcher { get; private set; }

        #endregion

        #region Actions

        public Action<MouseInputCode> MouseAction { get; set; }
        public Action<MouseInputCode> TrayMouseAction { get; set; }

        public Action OnBeginRender { get; set; }
        public Action OnEndRender { get; set; }

        public Action OnPostUpdate { get; set; }
        public Action OnPreUpdate { get; set; }

        public Action OnDevicesChanged { get; set; }

        public Action OnFocusLost { get; set; }
        public Action OnFocusGained { get; set; }
        public Action<Vector2> OnWindowResize { get; set; }
        public Action<Vector2> OnWindowMoved { get; set; }
        public Action OnWindowClose { get; set; }
        public Action OnWindowDestroy { get; set; }

        internal Action<char> OnKeyboardInputTextReceived { get; set; }

        #endregion

        #region Private

        private readonly WndProcDelegate _wndProcDelegate;
        protected bool _alwaysOnTop;
        protected volatile bool _isDirty = false;
        protected volatile bool _fullRedraw = false;
        volatile bool _isResizing = false;

        volatile bool _stopRunningFlag = false;
        volatile bool _isRunning = false;

        protected bool _lastIsWindowFocused = false;
        public bool IsWindowFocused { get; protected set; } = false;
        public bool IsWindowShown { get; protected set; } = false;

        private Thread _renderThread;
        private RenderContextType _startWithType;

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
            FenUI.activeInstances.Add(this);
            Dispatcher = new();

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

        public void WithView(FenUISharp.Objects.View model)
        {
            Dispatcher.Invoke(() =>
            {
                RootViewPane.ViewModel = model;
            });
        }

        protected virtual void CreateAndUpdateRenderContext(RenderContextType type)
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
            DropTarget = new DragDropHandler(this);

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

                SetupLogic();
                LogicLoop();
            });
            _renderThread.Name = "Logic Thread";
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

        private void SetupLogic()
        {
            FContext.WithWindow(this); // Make sure to activate this window for the current thread

            // Initialize FRenderContext
            CreateAndUpdateRenderContext(_startWithType);

            FContext.WithRootViewPane(null);

            WindowKeyboardInput = new(this);

            RootViewPane = new(null);
            RootViewPane.Layout.Alignment.Value = () => new(0.5f, 0.5f);
            RootViewPane.Layout.StretchHorizontal.Value = () => true;
            RootViewPane.Layout.StretchVertical.Value = () => true;

            FContext.WithRootViewPane(RootViewPane);
        }

        private void LogicLoop()
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

                    Thread.Sleep(100); continue;
                }

                if (currentTime >= nextFrameTime)
                {
                    DeltaTime = (float)(currentTime - previousFrameTime) / 1000.0f;
                    previousFrameTime = currentTime;

                    OnWindowUpdateCall();

                    if (IsNextFrameRendering())
                    {
                        OnBeginRender?.Invoke();
                        RenderFrame();
                        OnEndRender?.Invoke();

                        _isDirty = false;
                        _fullRedraw = false;
                    }

                    nextFrameTime = currentTime + frameInterval;
                }

                // await Task.Delay(needsRender ? 1 : 16); // If not rendering, sleep longer (saves CPU); Edit: Not good, since the Update method inside the UIObjects gets skipped then; Edit: Maybe that's not that bad, but right now I don't want to refactor stuff to work like that
                Thread.Sleep(1);
            }

            RootViewPane.Dispose(); // Should recursively dispose everything, at least I hope it does
        }
        
        private void OnWindowUpdateCall(bool isPaused = false)
        {
            Time += DeltaTime;
            Dispatcher.UpdateQueue(); // This MUST be executed as first in update

            if (!isPaused)
                OnPreUpdate?.Invoke();

            RootViewPane.OnEarlyUpdate();
            RootViewPane.OnUpdate();        // First update iteration

            if (!isPaused)
                OnPostUpdate?.Invoke();
            
            RootViewPane.OnLateUpdate();    // Second update iteration

            if (_lastIsWindowFocused != IsWindowFocused)
            {
                if (IsWindowFocused) OnFocusGained?.Invoke();
                else OnFocusLost?.Invoke();
            }
            _lastIsWindowFocused = IsWindowFocused;
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

                ClearCanvasArea(clipPath, _canvas);

                if (RenderContext.Surface != null)
                    OnRenderFrame(RenderContext.Surface);

                RootViewPane.DrawToSurface(_canvas);

                if (DebugDisplayBounds)
                {
                    foreach (var component in GetAllUIObjects())
                    {
                        if (component.RenderThisFrame())
                        {
                            var bounds = component.Shape.GlobalBounds;
                            bounds.Inflate(2, 2);
                            _canvas.DrawRect(bounds, new SKPaint() { IsStroke = true, StrokeWidth = 1f, Color = SKColors.Blue });
                            _canvas.DrawCircle(component.Transform.LocalToGlobal(component.Transform.Anchor.CachedValue).x, component.Transform.LocalToGlobal(component.Transform.Anchor.CachedValue).y, 2, new SKPaint() { IsStroke = true, StrokeWidth = 1f, Color = SKColors.Blue });
                            var boundsInteractive = component.InteractiveSurface.GlobalSurface.CachedValue;
                            boundsInteractive.Inflate(1, 1);
                            _canvas.DrawRect(bounds, new SKPaint() { IsStroke = true, StrokeWidth = 1f, Color = SKColors.Yellow });
                            _canvas.DrawCircle(component.Transform.LocalToGlobal(component.Layout.AlignmentAnchor.CachedValue).x, component.Transform.LocalToGlobal(component.Layout.AlignmentAnchor.CachedValue).y, 1, new SKPaint() { IsStroke = true, StrokeWidth = 1f, Color = SKColors.Yellow });
                        }
                    }
                }

                _canvas.RestoreToCount(notClipped);

                if (DebugDisplayAreaCache)
                {
                    using (var paint = new SKPaint() { Color = SKColors.Red.WithAlpha(1) })
                        _canvas.DrawRect(Bounds, paint);
                }

                if (RenderContext.Surface != null)
                    OnAfterRenderFrame(RenderContext.Surface);

                RenderContext.EndDraw();
            }
        }

        public void ClearCanvasArea(SKPath path, SKCanvas canvas)
        {
            using var paint = new SKPaint { BlendMode = SKBlendMode.Clear };
            canvas.DrawPath(path, paint);
        }

        public SKPath GetCurrentDirtyClipPath()
        {
            // Return a copy to avoid external modifications
            return _cachedDirtyPath != null ? new SKPath(_cachedDirtyPath) : new SKPath();
        }

        private SKPath? _cachedDirtyPath;
        private SKPath? _lastDirtyPath;
        private SKPath GetDirtyClipPath()
        {
            // Clear the old cached path
            _cachedDirtyPath?.Dispose();
            
            var clipPath = new SKPath();
            
            if (_isDirty)
            {
                clipPath.AddRect(Bounds);
                _cachedDirtyPath = clipPath;
                return new SKPath(clipPath); // Return copy
            }

            foreach (var component in GetAllUIObjects())
            {
                if (component.WindowRedrawThisObject && component.GlobalEnabled && component.GlobalVisible)
                {
                    int pad = 4;
                    var bounds = component.Shape.GlobalBounds;
                    bounds.Inflate(pad, pad);
                    clipPath.AddRect(bounds);

                    var lastbounds = component.Shape.LastGlobalBounds;
                    lastbounds.Inflate(pad, pad);
                    clipPath.AddRect(lastbounds);

                    component.WindowRedrawThisObject = false;
                }
            }

            SKPath lastPath = null;
            if (_lastDirtyPath != null) lastPath = new SKPath(_lastDirtyPath);
            _lastDirtyPath?.Dispose();
            _lastDirtyPath = new SKPath(clipPath);

            if (lastPath != null)
                clipPath.AddPath(lastPath, SKPathAddMode.Append);

            _cachedDirtyPath = new SKPath(clipPath);
            
            return clipPath;
        }

        internal List<UIObject> GetAllUIObjects()
        {
            List<UIObject> list = new();
            RecursiveAddChildrenToList(RootViewPane, list);
            return list;
        }

        private void RecursiveAddChildrenToList(UIObject parent, List<UIObject> list)
        {
            if(!list.Contains(parent)) list.Add(parent);
            parent.Children.ToList().ForEach(x =>
            {
                if(!list.Contains(parent)) list.Add(x);
                RecursiveAddChildrenToList(x, list);
            });
        }

        // public List<UIComponent> OrderUIComponents(List<UIComponent> uiComponents)
        // {
        //     return uiComponents.AsEnumerable().OrderBy(e => { if (e != null) return e.Transform.ZIndex; else return -99; }).ThenBy(e => { if (e != null) return e.Transform.CreationIndex; else return -99; }).ToList();
        // }

        public void Redraw() => _isDirty = true;
        public void FullRedraw()
        {
            _isDirty = true;
            _fullRedraw = true;

            if(RenderContext != null && RenderContext.Surface != null)
                RenderContext.RecreateSurface();
            if(RootViewPane != null)
                RootViewPane.RecursiveInvalidate(Objects.UIObject.Invalidation.All);
        }

        public bool IsNextFrameRendering()
        {
            var i = GetAllUIObjects().Any(x => x.WindowRedrawThisObject && x.Enabled.CachedValue && x.Visible.CachedValue);
            return i || DebugDisplayAreaCache || _isDirty || _fullRedraw;
        }

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
            WindowKeyboardInput.Dispose();

            FenUI.activeInstances.Remove(this);
        }

        public void DisposeAndDestroyWindow()
        {
            SetWindowVisibility(false); // To make sure the window disappears instantly (or as fast as possible) hide it first

            Dispose();
            _stopRunningFlag = true;
        }

        internal virtual void UpdateWindowFrame()
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
        protected virtual void WindowMoved()
        {
        }

        protected virtual void OnEndResize()
        {
            RenderContext?.OnEndResize();

            RootViewPane.Layout.RecursivelyUpdateLayout();
            Dispatcher.Invoke(() => FullRedraw()); // Make sure it gets executed a tick later
        }

        // Main thread methods

        Vector2 oldSize;

        protected virtual void WindowRzd(Vector2 size)
        {
            if (!_isResizing && size != oldSize)
                Dispatcher.Invoke(() => OnEndResize());
            oldSize = size;

            _isDirty = true;

            if (RenderContext != null)
                Dispatcher.Invoke(() => RenderContext.OnResize(size));

            RecalcClientBounds();
            Dispatcher.Invoke(() => FullRedraw()); // Make sure it gets executed a tick later
        }

        private void OnWindowMvd()
        {
            Dispatcher.Invoke(() =>
            {
                WindowMoved();
                OnWindowMoved?.Invoke(WindowPosition);
            });

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

                if (gch.Target != null)
                {
                    var instance = (Window)gch.Target;
                    return instance.WindowsProcedure(hWnd, msg, wParam, lParam);
                }
            }

            return DefWindowProcW(hWnd, msg, wParam, lParam);
        }

        protected IntPtr WindowsProcedure(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case 0x0102: // Keyboard input
                    Dispatcher.Invoke(() =>  OnKeyboardInputTextReceived?.Invoke((char)wParam));
                    break;

                case (int)WindowMessages.WM_DEVICECHANGE:
                    Dispatcher.Invoke(() => OnDevicesChanged?.Invoke());
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
                        Dispatcher.Invoke(() => TrayMouseAction?.Invoke(new MouseInputCode(MouseInputButton.Right, MouseInputState.Up)));

                    if ((int)lParam == (int)WindowMessages.WM_LBUTTONUP)
                        Dispatcher.Invoke(() => TrayMouseAction?.Invoke(new MouseInputCode(MouseInputButton.Left, MouseInputState.Up)));
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
                    Dispatcher.Invoke(() => OnWindowResize?.Invoke(WindowSize));
                    break;

                case (int)WindowMessages.WM_EXITSIZEMOVE:
                    _isDirty = true;
                    _isResizing = false;
                    Dispatcher.Invoke(() => OnEndResize());
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
                    Dispatcher.Invoke(() => MouseAction?.Invoke(new MouseInputCode(MouseInputButton.Left, MouseInputState.Down)));
                    return IntPtr.Zero;
                case (int)WindowMessages.WM_RBUTTONDOWN:
                    Dispatcher.Invoke(() => MouseAction?.Invoke(new MouseInputCode(MouseInputButton.Right, MouseInputState.Down)));
                    return IntPtr.Zero;
                case (int)WindowMessages.WM_MBUTTONDOWN:
                    Dispatcher.Invoke(() => MouseAction?.Invoke(new MouseInputCode(MouseInputButton.Middle, MouseInputState.Down)));
                    return IntPtr.Zero;
                case (int)WindowMessages.WM_LBUTTONUP:
                    Dispatcher.Invoke(() => MouseAction?.Invoke(new MouseInputCode(MouseInputButton.Left, MouseInputState.Up)));
                    return IntPtr.Zero;
                case (int)WindowMessages.WM_RBUTTONUP:
                    Dispatcher.Invoke(() => MouseAction?.Invoke(new MouseInputCode(MouseInputButton.Right, MouseInputState.Up)));
                    return IntPtr.Zero;
                case (int)WindowMessages.WM_MBUTTONUP:
                    Dispatcher.Invoke(() => MouseAction?.Invoke(new MouseInputCode(MouseInputButton.Middle, MouseInputState.Up)));
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
                    {
                        Dispatcher.Invoke(() =>
                        {
                            _isRunning = false;
                            OnWindowClose?.Invoke();
                            Dispose();
                        });
                    }
                    return IntPtr.Zero;

                case (int)WindowMessages.WM_DESTROY:
                    OnWindowDestroy?.Invoke();
                    PostQuitMessage(0);
                    return IntPtr.Zero;
            }
            return DefWindowProcW(hWnd, msg, wParam, lParam);
        }

        private const int DEVICE_NOTIFY_WINDOW_HANDLE = 0x00000000;
        private const int DBT_DEVTYP_DEVICEINTERFACE = 0x00000005;

        private static readonly Guid GUID_DEVINTERFACE_AUDIO_RENDER =
            new Guid("E6327CAD-DCEC-4949-AE8A-991E976A79D2");

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr RegisterDeviceNotification(IntPtr hRecipient, IntPtr notificationFilter, int flags);

        [StructLayout(LayoutKind.Sequential)]
        private struct DEV_BROADCAST_DEVICEINTERFACE
        {
            public int dbcc_size;
            public int dbcc_devicetype;
            public int dbcc_reserved;
            public Guid dbcc_classguid;
            public short dbcc_name;
        }

        protected void RegisterForDeviceNotifications()
        {
            var dbi = new DEV_BROADCAST_DEVICEINTERFACE
            {
                dbcc_size = Marshal.SizeOf(typeof(DEV_BROADCAST_DEVICEINTERFACE)),
                dbcc_devicetype = DBT_DEVTYP_DEVICEINTERFACE,
                dbcc_classguid = GUID_DEVINTERFACE_AUDIO_RENDER
            };

            IntPtr buffer = Marshal.AllocHGlobal(Marshal.SizeOf(dbi));
            Marshal.StructureToPtr(dbi, buffer, false);

            devicesChangedHandle = RegisterDeviceNotification(hWnd, buffer,
                DEVICE_NOTIFY_WINDOW_HANDLE);

            Marshal.FreeHGlobal(buffer);
        }

        protected RECT GetMonitorRect(int monitorIndex)
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

        internal const int WS_OVERLAPPEDWINDOW =
            (int)WindowStyles.WS_OVERLAPPED |
            (int)WindowStyles.WS_CAPTION |
            (int)WindowStyles.WS_SYSMENU |
            (int)WindowStyles.WS_THICKFRAME |
            (int)WindowStyles.WS_MINIMIZEBOX |
            (int)WindowStyles.WS_MAXIMIZEBOX;

        internal const int WS_NATIVE =
            (int)WindowStyles.WS_OVERLAPPED |
            (int)WindowStyles.WS_CAPTION |
            (int)WindowStyles.WS_SYSMENU |
            (int)WindowStyles.WS_MINIMIZEBOX;

        internal const int WS_NOTITLEBAR =
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
        protected static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst,
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
        protected static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        [DllImport("user32.dll")]
        protected static extern int GetSystemMetrics(int nIndex);

        [DllImport("dwmapi.dll")]
        protected static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

        [DllImport("dwmapi.dll")]
        internal static extern int DwmSetWindowAttribute(
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
}