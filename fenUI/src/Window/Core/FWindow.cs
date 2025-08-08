using System.Runtime.InteropServices;
using System.Text;
using FenUISharp.Logging;
using FenUISharp.Mathematics;
using FenUISharp.Native;
using SkiaSharp;

namespace FenUISharp
{
    public abstract class FWindow : IDisposable
    {
        public IntPtr hWnd { get; private set; }

        public string WindowTitle
        {
            get { StringBuilder s = new(); Win32APIs.GetWindowTextA(hWnd, s, 999); return s.ToString(); }
            set { Win32APIs.SetWindowTextA(hWnd, value); }
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

        public SkiaDirectCompositionContext? SkiaDirectCompositionContext { get; set; }

        public MultiAccess<Cursor> ActiveCursor = new MultiAccess<Cursor>(Cursor.ARROW);

        internal bool _isRunning = false;
        internal bool _isDirty = true;
        internal bool _fullRedraw;
        private static readonly WndProcDelegate _wndProcDelegate = StaticWndProc;

        public FWindow(string title, string className, Vector2? position = null, Vector2? size = null)
        {
            this.WindowClass = className;

            var wndClass = RegisterClass(className);

            // Creating the hidden owner
            // IntPtr hiddenOwnerHandle = IntPtr.Zero;
            // if (!isTaskbarIconVisible)
            //     FWindowProperties.CreateHiddenOwner(out hiddenOwnerHandle);

            // Create the window
            hWnd = CreateWindow(wndClass, position ?? new(-1, -1), size ?? new(600, 800));
            if (hWnd == IntPtr.Zero)
                throw new Exception($"Window creation failed: error {Marshal.GetLastWin32Error()}");

            this.WindowTitle = title;

            // Creating the components of the window
            var components = GetComponents();

            // Assigning the components to the properties
            Shape = components.Item1;
            Procedure = components.Item2;
            Loop = components.Item3;
            Callbacks = components.Item4;
            Properties = components.Item5;
            Surface = components.Item6;

            // Properties._hiddenOwnerWindowHandle = hiddenOwnerHandle;
            Properties.UseSystemDarkMode = true; // Default to true, can be changed later

            // Creating dispatchers
            LogicDispatcher = new Dispatcher();
            WindowDispatcher = new Dispatcher();

            // Making sure every instance of the Window class has its own WndProc
            // Allocating this instance of the FWindow class to a GCHandle and using the IntPtr
            GCHandle gch = GCHandle.Alloc(this);
            Win32APIs.SetWindowLongPtr(hWnd, -21 /* GWLP_USERDATA */, GCHandle.ToIntPtr(gch));

            // Pre initialize OLE DragDrop
            DragDropRegistration.Initialize();
        }

        protected (FWindowShape, FWindowProcedure, FWindowLoop, FWindowCallbacks, FWindowProperties, FWindowSurface) GetComponents()
        {
            var Shape = new FWindowShape(this);
            var Procedure = new FWindowProcedure(this) { _isRunning = () => _isRunning };
            var Loop = new FWindowLoop(this) { _isRunning = () => _isRunning };
            var Callbacks = new FWindowCallbacks(this);
            var Properties = new FWindowProperties(this);
            var Surface = new FWindowSurface(this);

            return (Shape, Procedure, Loop, Callbacks, Properties, Surface);
        }

        public void WithView(FenUISharp.Objects.View model)
        {
            LogicDispatcher.Invoke(() =>
            {
                Surface.RootViewPane.ViewModel = model;
            });
        }

        public abstract IntPtr CreateWindow(WNDCLASSEX wndClass, Vector2 position, Vector2 size);

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

            if (ptr != IntPtr.Zero)
            {
                GCHandle gch = GCHandle.FromIntPtr(ptr);

                if (gch.Target != null)
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
            SkiaDirectCompositionContext = new(this, DrawAction);
        }

        public virtual void DrawAction(SKCanvas canvas)
        {
            ClearSurface(canvas);
            DrawBackdrop(canvas);

            Surface.Draw(canvas);
        }

        public virtual void ClearSurface(SKCanvas canvas)
            => canvas.Clear(Properties.UseMica ? SKColors.Transparent : Surface.ClearColor());

        public virtual void DrawBackdrop(SKCanvas canvas)
        {

        }

        public void BeginWindowLoop()
        {
            // Display the window
            // Properties.ShowWindow(ShowWindowCommand.SW_SHOW);

            // Initialize OLE drag drop
            SetupOLEDragDrop();

            // Start the window loop
            _isRunning = true;
            Loop.Begin(SetupWindowLogicOnBegin);
        }

        private void SetupOLEDragDrop()
        {
            // Setup OLE DragDrop
            // Keep a reference to prevent garbage collection
            DropTarget = new DragDropHandler(this)
            {
                dragEnter = (x) => LogicDispatcher.Invoke(() => Callbacks.OnDragEnter?.Invoke(x)),
                dragDrop = (x) => LogicDispatcher.Invoke(() => Callbacks.OnDragDrop?.Invoke(x)),
                dragOver = (x) => LogicDispatcher.Invoke(() => Callbacks.OnDragOver?.Invoke(x)),
                dragLeave = () => LogicDispatcher.Invoke(() => Callbacks.OnDragLeave?.Invoke())
            };

            // Get COM interface pointer for the drop target
            IntPtr pDropTarget = Marshal.GetComInterfaceForObject(
                DropTarget,
                typeof(IDropTarget)
            );

            // Register the window for drag-drop
            int hr = DragDropRegistration.RegisterDragDrop(hWnd, pDropTarget);
            if (hr != 0)
                Marshal.ThrowExceptionForHR(hr);

            // Release the COM interface pointer
            Marshal.Release(pDropTarget);
        }

        public void RequestFocus()
            => Win32APIs.SetForegroundWindow(hWnd);

        public void Dispose()
        {
            Shape?.Dispose();
            Procedure?.Dispose();
            Loop?.Dispose();
            Properties?.Dispose();
            Surface?.Dispose();

            SkiaDirectCompositionContext?.Dispose();
        }

        public void Redraw() => _isDirty = true;
        public void FullRedraw()
        {
            _isDirty = true;
            _fullRedraw = true;

            if (Surface.RootViewPane != null)
                Surface.RootViewPane.RecursiveInvalidate(Objects.UIObject.Invalidation.All);
        }
    }
}