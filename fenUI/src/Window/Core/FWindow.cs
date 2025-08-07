using System.Runtime.InteropServices;
using FenUISharp.Logging;
using FenUISharp.Mathematics;
using FenUISharp.Native;
using SkiaSharp;

namespace FenUISharp
{
    public abstract class FWindow : IDisposable
    {
        public IntPtr hWnd { get; private set; }

        public string WindowTitle { get; protected set; }
        public string WindowClass { get; protected set; }

        public int TargetRefreshRate { get; set; } = 60;

        public FWindowShape Shape { get; protected set; }
        public FWindowProcedure Procedure { get; protected set; }
        public FWindowLoop Loop { get; protected set; }
        public FWindowCallbacks Callbacks { get; protected set; }
        public DragDropHandler? DropTarget { get; private set; }

        public FTime Time { get; protected set; } = new FTime();

        public Dispatcher LogicDispatcher { get; private set; }
        public Dispatcher WindowDispatcher { get; private set; }

        public SkiaDirectCompositionContext? SkiaDirectCompositionContext { get; set; }

        private bool _isRunning = false;
        private static readonly WndProcDelegate _wndProcDelegate = StaticWndProc;

        public FWindow(string title, string className, Vector2? position = null, Vector2? size = null)
        {
            this.WindowTitle = title;
            this.WindowClass = className;

            var wndClass = RegisterClass(className);

            hWnd = CreateWindow(wndClass, position ?? new(-1, -1), size ?? new(600, 800));
            if (hWnd == IntPtr.Zero)
                throw new Exception($"Window creation failed: error {Marshal.GetLastWin32Error()}");

            // Creating the components of the window
            var components = GetComponents();

            // Assigning the components to the properties
            Shape = components.Item1;
            Procedure = components.Item2;
            Loop = components.Item3;
            Callbacks = components.Item4;

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

        protected (FWindowShape, FWindowProcedure, FWindowLoop, FWindowCallbacks) GetComponents()
        {
            var Shape = new FWindowShape(this);
            var Procedure = new FWindowProcedure(this) { _isRunning = () => _isRunning};
            var Loop = new FWindowLoop(this) { _isRunning = () => _isRunning };
            var Callbacks = new FWindowCallbacks(this);

            return (Shape, Procedure, Loop, Callbacks);
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

        public void DrawAction(SKCanvas canvas)
        {
            canvas.Clear(new SKColor(0, 0, 0, 1)); // A tiny bit of alpha (not zero!)
            // throw new NotImplementedException();
        }

        public void BeginWindowLoop()
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

            // Start the window loop
            _isRunning = true;
            Loop.Begin(SetupWindowLogicOnBegin);
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}