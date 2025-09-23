using System.Runtime.InteropServices;
using System.Text;
using FenUISharp.Logging;
using FenUISharp.Mathematics;
using FenUISharp.Native;
using FenUISharp.Themes;
using FenUISharp.WinFeatures;
using SkiaSharp;

namespace FenUISharp
{
    public abstract class FWindow : IDisposable
    {
        public IntPtr hWnd { get; private set; }

        public string WindowTitle
        {
            get { StringBuilder s = new(); Win32APIs.GetWindowTextA(hWnd, s, 999); return s.ToString(); }
            set { FLogger.Log<FWindow>($"Changing title of {hWnd} to {value}"); Win32APIs.SetWindowTextA(hWnd, value); }
        }
        public string WindowClass { get; protected set; }

        public int TargetRefreshRate { get; set; } = 60; // TODO: Ideally use monitors refresh rate

        // Window components

        public FWindowShape Shape { get; protected set; }
        public FWindowProcedure Procedure { get; protected set; }
        public FWindowLoop Loop { get; protected set; }
        public FWindowCallbacks Callbacks { get; protected set; }
        public FWindowProperties Properties { get; protected set; }
        public FWindowSurface Surface { get; protected set; }
        public DragDropHandler? DropTarget { get; private set; }

        public FTime Time { get; protected set; } = new FTime();

        public Dispatcher LogicDispatcher { get; private set; }
        public Dispatcher WindowDispatcher { get; private set; }

        public KeyboardInputManager? WindowKeyboardInput { get; private set; }
        public ThemeManager WindowThemeManager { get; private set; }

        // public Vector2 ClientMousePosition
        // {
        //     get
        //     {
        //         POINT cursorPoint = new() { x = (int)GlobalHooks.MousePosition.x, y = (int)GlobalHooks.MousePosition.y };
        //         Win32APIs.ScreenToClient(hWnd, ref cursorPoint);
        //         return new Vector2(cursorPoint.x, cursorPoint.y);
        //     }
        // }

        public Vector2 ClientMousePosition { get; internal set; }

        public SkiaDirectCompositionContext? SkiaDirectCompositionContext { get; set; }
        public bool DebugDisplayAreaCache { get; internal set; }
        public bool DebugDisplayBounds { get; internal set; }
        public bool DebugDisplayObjectIDs { get; internal set; }

        public MultiAccess<Cursor> ActiveCursor = new MultiAccess<Cursor>(Cursor.ARROW);

        internal bool _disposingOrDisposed = false;
        internal bool _isRunning = false;
        internal bool _isDirty = false;
        internal bool _fullRedraw;
        private static readonly WndProcDelegate _wndProcDelegate = StaticWndProc;

        public FWindow(string title, string className, Vector2? position = null, Vector2? size = null)
        {
            FLogger.Log<FWindow>($"Creating window with title {title} and class {className}");

            if (!FenUI.HasBeenInitialized) throw new Exception("FenUI has to be initialized before creating a window.");

            // Set active instance and current FContext window
            FenUI.activeInstances.Add(this);

            // Creating dispatchers
            FLogger.Log<FWindow>($"Creating dispatchers");
            LogicDispatcher = new Dispatcher();
            WindowDispatcher = new Dispatcher();

            // Register window class
            FLogger.Log<FWindow>($"Registering class...");
            this.WindowClass = className;
            var wndClass = RegisterClass(className);

            // Create the window
            FLogger.Log<FWindow>($"Creating window of type {GetType()}...");
            hWnd = CreateWindow(wndClass, position ?? new(-1, -1), size ?? new(600, 800));
            if (hWnd == IntPtr.Zero)
                throw new Exception($"Window creation failed: error {Marshal.GetLastWin32Error()}");

            // Updating window title. Title is expected to be empty before this line
            this.WindowTitle = title;

            // Creating the components of the window
            FLogger.Log<FWindow>($"Gathering window components");
            var components = GetComponents();

            // Assigning the components to the properties
            FLogger.Log<FWindow>($"Assinging window components to fields");
            Shape = components.Item1;
            Procedure = components.Item2;
            Loop = components.Item3;
            Callbacks = components.Item4;
            Properties = components.Item5;
            Surface = components.Item6;

            Callbacks.OnWindowEndMove += (x) => SetTargetRefreshrateToMonitorRefreshrate();
            Properties.UseSystemDarkMode = true; // Default to true, can be changed later

            WindowThemeManager = new(Resources.GetTheme("default-dark"));

            // Set initially dirty
            FullRedraw();

            // Making sure every instance of the Window class has its own WndProc
            // Allocating this instance of the FWindow class to a GCHandle and using the IntPtr
            FLogger.Log<FWindow>($"Allocating this class and getting GCHandle");
            GCHandle gch = GCHandle.Alloc(this);

            FLogger.Log<FWindow>($"Allocating the GCHandle in GWLP_USERDATA");
            Win32APIs.SetWindowLongPtr(hWnd, -21 /* GWLP_USERDATA */, GCHandle.ToIntPtr(gch));

            // Pre initialize OLE DragDrop
            FLogger.Log<FWindow>($"Pre-Init OLE DragDrop");
            DragDropRegistration.Initialize();

            OnAfterWindowCreation();

            FLogger.Log<FWindow>($"Window creation done!");
            FLogger.Log<FWindow>($"");
        }

        protected virtual void OnAfterWindowCreation() { }

        protected virtual (FWindowShape, FWindowProcedure, FWindowLoop, FWindowCallbacks, FWindowProperties, FWindowSurface) GetComponents()
        {
            var Shape = new FWindowShape(this);
            var Procedure = new FWindowProcedure(this) { _isRunning = () => _isRunning };
            var Callbacks = new FWindowCallbacks(this);
            var Loop = new FWindowLoop(this) { _logicIsRunning = () => _isRunning && !_disposingOrDisposed, _windowIsRunning = () => _isRunning };
            var Properties = new FWindowProperties(this);
            var Surface = new FWindowSurface(this);

            return (Shape, Procedure, Loop, Callbacks, Properties, Surface);
        }

        public virtual void WithView(FenUISharp.Objects.View model)
        {
            LogicDispatcher.Invoke(() =>
            {
                if (Surface.RootViewPane != null)
                    Surface.RootViewPane.ViewModel = model;
            });
        }

        protected abstract IntPtr CreateWindow(WNDCLASSEX wndClass, Vector2 position, Vector2 size);

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

            Win32APIs.RegisterClassExA(ref wndClass);

            return wndClass;
        }

        private static IntPtr StaticWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            IntPtr ptr = Win32APIs.GetWindowLongPtrW(hWnd, -21 /* GWLP_USERDATA */);

            // Make sure it's not null
            if (ptr != IntPtr.Zero)
            {
                // Getting GCHandle from the IntPtr
                GCHandle gch = GCHandle.FromIntPtr(ptr);

                // Testing if it's null or a valid FWindow class
                if (gch.Target != null && gch.Target is FWindow)
                {
                    var instance = (FWindow)gch.Target;
                    return instance.Procedure.WindowsProcedure(hWnd, msg, wParam, lParam);
                }
            }

            // If the GCHandle is not found or the target is null, fallback to the default window procedure
            return Win32APIs.DefWindowProcW(hWnd, msg, wParam, lParam);
            // throw new InvalidOperationException("Window GCHandle not found or target is null."); // Don't throw
        }

        protected virtual void SetupWindowLogicOnBegin()
        {
            // Make sure to activate this window for the current thread
            FContext.WithWindow(this);

            // Create skia render context
            if (!_disableSDXCC)
                SkiaDirectCompositionContext = new(this, DrawAction);

            // Create keyboard input for this window
            WindowKeyboardInput = new(this);

            // Setup the surface components
            Surface.SetupSurface();
        }

        private bool _disableSDXCC = false;
        public void DisableDirectContextCreation()
            => _disableSDXCC = true;

        /// <summary>
        /// Is executed automatically by the SkiaDirectCompositionContext
        /// </summary>
        /// <param name="canvas"></param>
        protected virtual void DrawAction(SKCanvas canvas)
        {
            Surface.Draw(canvas);

            // Reset flags
            _isDirty = false;
            _fullRedraw = false;
        }

        internal virtual void ClearSurface(SKCanvas canvas)
            => canvas.Clear(Properties.UseMica ? SKColors.Transparent : Surface.ClearColor());

        internal virtual void DrawBackdrop(SKCanvas canvas)
        {

        }

        public void BeginWindowLoop()
        {
            // Display the window
            // Properties.ShowWindow(ShowWindowCommand.SW_SHOW);

            // Initialize OLE drag drop
            FLogger.Log<FWindow>($"Setting up OLE DragDrop...");
            SetupOLEDragDrop();

            FLogger.Log<FWindow>($"Begin the loop!");
            FLogger.Log<FWindow>($"");

            // Start the window loop
            _isRunning = true;
            Loop.Begin(SetupWindowLogicOnBegin);
        }

        private void SetupOLEDragDrop()
        {
            // Setup OLE DragDrop
            // Keep a reference to prevent garbage collection
            FLogger.Log<FWindow>($"Creating DragDropHandler");
            DropTarget = new DragDropHandler(this)
            {
                dragEnter = (x) => LogicDispatcher.Invoke(() => Callbacks.OnDragEnter?.Invoke(x)),
                dragDrop = (x) => LogicDispatcher.Invoke(() => Callbacks.OnDragDrop?.Invoke(x)),
                dragOver = (x) => LogicDispatcher.Invoke(() => Callbacks.OnDragOver?.Invoke(x)),
                dragLeave = () => LogicDispatcher.Invoke(() => Callbacks.OnDragLeave?.Invoke())
            };

            // Get COM interface pointer for the drop target
            FLogger.Log<FWindow>($"Get COM interface pointer for the created DragDropHandler");
            IntPtr pDropTarget = Marshal.GetComInterfaceForObject(
                DropTarget,
                typeof(IDropTarget)
            );

            // Register the window for drag-drop
            FLogger.Log<FWindow>($"Registering DragDrop for window {hWnd} using drop target {pDropTarget}");
            int hr = DragDropRegistration.RegisterDragDrop(hWnd, pDropTarget);
            if (hr != 0)
                Marshal.ThrowExceptionForHR(hr);

            // Release the COM interface pointer
            FLogger.Log<FWindow>($"Release COM interface pointer");
            Marshal.Release(pDropTarget);
        }

        public void RequestFocus()
            => Win32APIs.SetForegroundWindow(hWnd);

        internal void CallUpdate() => Update();
        protected virtual void Update()
        {
            
        }

        public virtual void Dispose()
        {
            if (_disposingOrDisposed) return;
            FContext.isDisposingWindow = true;

            // Set disposed flag
            _disposingOrDisposed = true;

            // The window should always hide first, even if the HideWindowOnClose is false
            // Hiding the window first is much faster than waiting for the destruction
            Properties.IsWindowVisible = false;

            // Disposing all components
            FLogger.Log<FWindow>($"Early cleanup of window components...");
            EarlyCleanUp();

            // Callback
            Callbacks.OnWindowDestroy?.Invoke();

            // Execute DestroyWindow on main thread
            FLogger.Log<FWindow>($"Invoking window destruction...");
            WindowDispatcher.Invoke(() =>
            {
                // Remove window
                FLogger.Log<FWindow>($"Destroying window");
                Win32APIs.DestroyWindow(hWnd);
            });

            // Remove this window from the active instances
            FenUI.activeInstances.Remove(this);
        }

        internal void EarlyCleanUp()
        {
            // ALWAYS dispose surface first. All UIObjects will clean up and
            // maybe need references to window keyboard input, a valid FContext etc...
            FLogger.Log<FWindow>($"Disposing surface...");
            Surface?.Dispose();

            // Removing parent
            FLogger.Log<FWindow>($"Disposing parent window...");
            Properties?.DisposeParent();

            FLogger.Log<FWindow>($"Disposing window keyboard input...");
            WindowKeyboardInput?.Dispose();
            WindowKeyboardInput = null;
        }

        internal void CleanUp()
        {
            // Disposing components
            FLogger.Log<FWindow>($"Disposing of window components...");
            Shape?.Dispose();
            Shape = null!;
            Procedure?.Dispose();
            Procedure = null!;
            Loop?.Dispose();
            Loop = null!;
            Properties?.Dispose();
            Properties = null!;
            
            Surface = null!;

            FLogger.Log<FWindow>($"Disposing SkiaDirectCompositionContext...");
            SkiaDirectCompositionContext?.Dispose();
            SkiaDirectCompositionContext = null!;
        }

        protected virtual void SetTargetRefreshrateToMonitorRefreshrate()
        {
            FLogger.Log<FWindow>($"Setting the target refresh rate of window {hWnd}...");

            DISPLAY_DEVICE d = new DISPLAY_DEVICE();
            d.cb = Marshal.SizeOf(d);

            if (Win32APIs.EnumDisplayDevices(null, (uint)Shape.CurrentMonitorIndex, ref d, 0))
            {
                DEVMODE vDevMode = new DEVMODE();
                vDevMode.dmSize = (ushort)Marshal.SizeOf(typeof(DEVMODE));

                FLogger.Log<FWindow>($"Window is on monitor {d.DeviceName}");

                if (Win32APIs.EnumDisplaySettings(d.DeviceName, -1 /* ENUM_CURRENT_SETTINGS */, ref vDevMode))
                {
                    TargetRefreshRate = (int)vDevMode.dmDisplayFrequency;
                    FLogger.Log<FWindow>($"Set target refresh rate of window {hWnd} to {TargetRefreshRate}!");
                }
            }
        }

        internal virtual IntPtr WindowHitTest(IntPtr wParam, IntPtr lParam)
            => IntPtr.Zero;

        public void Redraw() => _isDirty = true;
        public void FullRedraw()
        {
            // Setting dirty flags to true
            _isDirty = true;
            _fullRedraw = true;

            if (Surface.RootViewPane != null)
                Surface.RootViewPane.RecursiveInvalidate(Objects.UIObject.Invalidation.All);
        }
    }
}