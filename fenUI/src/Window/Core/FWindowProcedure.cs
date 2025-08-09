using System.Runtime.InteropServices;
using FenUISharp.Logging;
using FenUISharp.Mathematics;
using FenUISharp.Native;

namespace FenUISharp
{
    public class FWindowProcedure : IDisposable
    {
        private WeakReference<FWindow> window { get; set; }
        public FWindow Window { get => window.TryGetTarget(out var target) ? target : throw new Exception("Window not set."); }

        public Func<bool>? _isRunning { get; set; }

        public FWindowProcedure(FWindow window)
        {
            this.window = new WeakReference<FWindow>(window);
        }

        public IntPtr WindowsProcedure(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            Window.WindowDispatcher.UpdateQueue(); // Dispatch queued events

            switch (msg)
            {
                // When a char is typed and the window is focused
                case WindowMessages.WM_CHAR: // Keyboard input
                    Window.LogicDispatcher.Invoke(() => Window.Callbacks.OnKeyboardInputTextReceived?.Invoke((char)wParam));
                    break;

                // When a device is changed. I don't know what this does
                case (int)WindowMessages.WM_DEVICECHANGE:
                    FLogger.Log<FWindowProcedure>($"WM_DEVICECHANGE: Device change for window {Window.hWnd}");

                    Window.LogicDispatcher.Invoke(() => Window.Callbacks.OnDevicesChanged?.Invoke());
                    break;

                // When the window is resized and needs min/max size info
                case (int)WindowMessages.WM_GETMINMAXINFO:

                    // Put minimum and maximum window size into MINMAXINFO struct
                    MINMAXINFO minMaxInfo = Marshal.PtrToStructure<MINMAXINFO>(lParam);
                    minMaxInfo.ptMinTrackSize = new POINT() { x = (int)Window.Shape.MinSize.x, y = (int)Window.Shape.MinSize.y };
                    minMaxInfo.ptMaxTrackSize = new POINT() { x = (int)Window.Shape.MaxSize.x, y = (int)Window.Shape.MaxSize.y };

                    // Put structure into lParam as pointer
                    Marshal.StructureToPtr(minMaxInfo, lParam, true);
                    break;

                // Mouse callbacks when tray icon was clicked
                case (int)WindowMessages.WM_USER + 1:
                    // FLogger.Log<FWindowProcedure>($"WM_USER + 1: Executed action {lParam}"); // Too many outputs, makes log cramped

                    // Notify on right mb
                    if ((int)lParam == (int)WindowMessages.WM_RBUTTONUP)
                        Window.LogicDispatcher.Invoke(() => Window.Callbacks.TrayMouseAction?.Invoke(new MouseInputCode(MouseInputButton.Right, MouseInputState.Up)));

                    // Notify on left mb
                    if ((int)lParam == (int)WindowMessages.WM_LBUTTONUP)
                        Window.LogicDispatcher.Invoke(() => Window.Callbacks.TrayMouseAction?.Invoke(new MouseInputCode(MouseInputButton.Left, MouseInputState.Up)));
                    return IntPtr.Zero;

                // Update system darkmode if neeed
                case (int)WindowMessages.WM_INITMENUPOPUP:
                case (int)WindowMessages.WM_SETTINGCHANGE:
                    FLogger.Log<FWindowProcedure>($"WM_INITMENUPOPUP/WM_SETTINGCHANGE: Window {Window.hWnd}");
                    Window.Properties.UpdateSysDarkmode();
                    break;

                // When the window is in a resize action
                case (int)WindowMessages.WM_SIZING:
                case (int)WindowMessages.WM_SIZE:
                    _isSizing = true;

                    // if (!_isSizeMoving)
                    //     FLogger.Log<FWindowProcedure>($"Started resizing");

                    WindowRzd(Window.Shape.Size);
                    return IntPtr.Zero;

                // When the window is currently being moved
                case (int)WindowMessages.WM_MOVING:
                case (int)WindowMessages.WM_MOVE:
                    // Console.WriteLine("WM_MOVE");
                    Window.LogicDispatcher.Invoke(() =>
                        Window.Callbacks.OnWindowMove?.Invoke(Window.Shape.Position));

                        Window.LogicDispatcher.Invoke(() => Window.SkiaDirectCompositionContext?.OnResize(Window.Shape.Size));
                    
                    return IntPtr.Zero;

                case (int)WindowMessages.WM_ENTERSIZEMOVE:
                    FLogger.Log<FWindowProcedure>($"");
                    FLogger.Log<FWindowProcedure>($"WM_ENTERSIZEMOVE: Window {Window.hWnd}");
                    _isSizeMoving = true;
                    break;

                // When the window exits a resize or move action
                case (int)WindowMessages.WM_EXITSIZEMOVE:
                    FLogger.Log<FWindowProcedure>($"");
                    FLogger.Log<FWindowProcedure>($"WM_EXITSIZEMOVE: Window {Window.hWnd}");

                    // If _isResizing was true, there was a resize action
                    if (_isSizing)
                    {
                        Vector2 size = Window.Shape.Size;

                        FLogger.Log<FWindowProcedure>($"Stopped resizing: {size}");
                        Window.LogicDispatcher.Invoke(() => Window.Callbacks.OnWindowEndResize?.Invoke(size));

                        Window.FullRedraw();
                    }
                    else
                    {
                        // Otherwise, there was a move action
                        FLogger.Log<FWindowProcedure>($"Stopped moving: {Window.Shape.Position}");
                        Window.LogicDispatcher.Invoke(() => Window.Callbacks.OnWindowEndMove?.Invoke(Window.Shape.Position));
                    }

                    Window.FullRedraw();
                    _isSizeMoving = false;
                    _isSizing = false;
                    break;

                // When the focus of the window is killed
                case (int)WindowMessages.WM_KILLFOCUS:
                    Window._isDirty = true;

                    // Delay by one frame
                    Window.Properties._isFocused = false;

                    // Notify that focus was lost
                    Window.LogicDispatcher?.Invoke(() => Window.Callbacks.OnFocusLost?.Invoke());
                    return IntPtr.Zero;

                // When the window gains focus
                case (int)WindowMessages.WM_SETFOCUS:
                    Window._isDirty = true;
                    Window.Properties._isFocused = true;

                    // Notify that focus was gained
                    Window.LogicDispatcher?.Invoke(() => Window.Callbacks.OnFocusGained?.Invoke());
                    return IntPtr.Zero;

                // Executed when the not client area is destroyed. Happens after WM_DESTROY
                case (int)WindowMessages.WM_NCDESTROY:
                    FLogger.Log<FWindowProcedure>($"WM_NCDESTROY: Free up GCHandle for window {Window.hWnd}");

                    IntPtr ptr = Win32APIs.GetWindowLongPtrA(hWnd, -21);
                    if (ptr != IntPtr.Zero)
                    {
                        GCHandle.FromIntPtr(ptr).Free();
                        Win32APIs.SetWindowLongPtrA(hWnd, -21, IntPtr.Zero);
                    }

                    FLogger.Log<FWindowProcedure>($"Fully destroyed window {hWnd}. Bye!");
                    FLogger.Log<FWindowProcedure>($"");
                    break;

                // Client area & focused keyboard and mouse callbacks
                case (int)WindowMessages.WM_KEYDOWN:
                    return IntPtr.Zero;
                case (int)WindowMessages.WM_LBUTTONDOWN:
                    Window.LogicDispatcher.Invoke(() => Window.Callbacks.ClientMouseAction?.Invoke(new MouseInputCode(MouseInputButton.Left, MouseInputState.Down)));
                    return IntPtr.Zero;
                case (int)WindowMessages.WM_RBUTTONDOWN:
                    Window.LogicDispatcher.Invoke(() => Window.Callbacks.ClientMouseAction?.Invoke(new MouseInputCode(MouseInputButton.Right, MouseInputState.Down)));
                    return IntPtr.Zero;
                case (int)WindowMessages.WM_MBUTTONDOWN:
                    Window.LogicDispatcher.Invoke(() => Window.Callbacks.ClientMouseAction?.Invoke(new MouseInputCode(MouseInputButton.Middle, MouseInputState.Down)));
                    return IntPtr.Zero;
                case (int)WindowMessages.WM_LBUTTONUP:
                    Window.LogicDispatcher.Invoke(() => Window.Callbacks.ClientMouseAction?.Invoke(new MouseInputCode(MouseInputButton.Left, MouseInputState.Up)));
                    return IntPtr.Zero;
                case (int)WindowMessages.WM_RBUTTONUP:
                    Window.LogicDispatcher.Invoke(() => Window.Callbacks.ClientMouseAction?.Invoke(new MouseInputCode(MouseInputButton.Right, MouseInputState.Up)));
                    return IntPtr.Zero;
                case (int)WindowMessages.WM_MBUTTONUP:
                    Window.LogicDispatcher.Invoke(() => Window.Callbacks.ClientMouseAction?.Invoke(new MouseInputCode(MouseInputButton.Middle, MouseInputState.Up)));
                    return IntPtr.Zero;

                // Set cursor message, returns the wanted cursor
                case (int)WindowMessages.WM_SETCURSOR:

                    // Hit test
                    int hitTest = lParam.ToInt32() & 0xFFFF;

                    // Check if the mouse is inside the client area. If not (titlebar), return default WindowProc
                    if (hitTest == HitTest.HTLEFT || hitTest == HitTest.HTRIGHT ||
                        hitTest == HitTest.HTTOP || hitTest == HitTest.HTTOPLEFT ||
                        hitTest == HitTest.HTTOPRIGHT || hitTest == HitTest.HTBOTTOM ||
                        hitTest == HitTest.HTBOTTOMLEFT || hitTest == HitTest.HTBOTTOMRIGHT)
                        return Win32APIs.DefWindowProcW(hWnd, msg, wParam, lParam);
                    else
                        // Otherwise, set the cursor to the wanted value
                        Win32APIs.SetCursor(Win32APIs.LoadCursor(IntPtr.Zero, (int)Window.ActiveCursor.Value));

                    return IntPtr.Zero;

                // When the window close button is pressed (X)
                case (int)WindowMessages.WM_CLOSE:
                    FLogger.Log<FWindowProcedure>($"WM_CLOSE: Window with handle {Window.hWnd} closed");

                    // The window should always hide first, even if the HideWindowOnClose is false
                    // Hiding the window first is much faster than waiting for the destruction
                    Window.Properties.IsWindowVisible = false;

                    // Check if HideWindowOnClose is false
                    if (!Window.Properties.HideWindowOnClose)
                    {
                        // FLogger.Log<FWindowProcedure>($"Adding disposal of window {Window.hWnd} to logic queue");

                        // Initiate window destruction and disposal
                        FLogger.Log<FWindowProcedure>($"Disposing window {Window.hWnd}...");
                        Window.Callbacks.OnWindowClose?.Invoke();
                        Window.Dispose();
                    }

                    return IntPtr.Zero;

                // When the window is destroyed
                case (int)WindowMessages.WM_DESTROY:

                    // Notify when the window gets destroyed
                    Window.Callbacks.OnWindowDestroy?.Invoke();

                    // Post the quit message. 0 means OK
                    FLogger.Log<FWindowProcedure>("WM_DESTROY: Posting quit message 0");
                    Win32APIs.PostQuitMessage(0);

                    return IntPtr.Zero;
            }

            // If no special handling is needed, fallback to the default window procedure
            return Win32APIs.DefWindowProcW(hWnd, msg, wParam, lParam);
        }

        private Vector2 oldSize;
        internal volatile bool _isSizing = false;
        internal volatile bool _isSizeMoving = false;
        internal volatile bool _isNotInitialResize = false;

        protected virtual void WindowRzd(Vector2 size)
        {
            if (oldSize != size)
                Window.LogicDispatcher.Invoke(() => Window.Callbacks.OnWindowResize?.Invoke(size));

            // Update old resize
            oldSize = size;

            // Set window to dirty
            Window._isDirty = true;

            // Calling a full redraw
            Window.LogicDispatcher.Invoke(() => Window.FullRedraw()); // Make sure it gets executed a tick later
        }

        public void Dispose()
        {

        }
    }
}