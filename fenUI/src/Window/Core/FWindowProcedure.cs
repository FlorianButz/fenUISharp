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
                case WindowMessages.WM_CHAR: // Keyboard input
                    Window.LogicDispatcher.Invoke(() => Window.Callbacks.OnKeyboardInputTextReceived?.Invoke((char)wParam));
                    break;

                case (int)WindowMessages.WM_DEVICECHANGE:
                    Window.LogicDispatcher.Invoke(() => Window.Callbacks.OnDevicesChanged?.Invoke());
                    break;

                case (int)WindowMessages.WM_GETMINMAXINFO:
                    MINMAXINFO minMaxInfo = Marshal.PtrToStructure<MINMAXINFO>(lParam);
                    minMaxInfo.ptMinTrackSize = new POINT() { x = (int)Window.Shape.MinSize.x, y = (int)Window.Shape.MinSize.y };
                    minMaxInfo.ptMaxTrackSize = new POINT() { x = (int)Window.Shape.MaxSize.x, y = (int)Window.Shape.MaxSize.y };
                    Marshal.StructureToPtr(minMaxInfo, lParam, true);
                    break;

                case (int)WindowMessages.WM_USER + 1:
                    if ((int)lParam == (int)WindowMessages.WM_RBUTTONUP)
                        Window.LogicDispatcher.Invoke(() => Window.Callbacks.TrayMouseAction?.Invoke(new MouseInputCode(MouseInputButton.Right, MouseInputState.Up)));

                    if ((int)lParam == (int)WindowMessages.WM_LBUTTONUP)
                        Window.LogicDispatcher.Invoke(() => Window.Callbacks.TrayMouseAction?.Invoke(new MouseInputCode(MouseInputButton.Left, MouseInputState.Up)));
                    return IntPtr.Zero;

                case (int)WindowMessages.WM_INITMENUPOPUP:
                case (int)WindowMessages.WM_SETTINGCHANGE:
                    Window.Properties.UpdateSysDarkmode();
                    break;

                case (int)WindowMessages.WM_SIZING:
                case (int)WindowMessages.WM_SIZE:
                    WindowRzd(new Vector2(wParam, lParam));
                    return IntPtr.Zero;

                case (int)WindowMessages.WM_ENTERSIZEMOVE:
                    _isResizing = true;
                    Window.LogicDispatcher.Invoke(() => Window.Callbacks.OnWindowResize?.Invoke(Window.Shape.Size));
                    break;

                case (int)WindowMessages.WM_EXITSIZEMOVE:
                    Window._isDirty = true;
                    _isResizing = false;
                    Window.LogicDispatcher.Invoke(() => Window.Callbacks.OnWindowEndResize?.Invoke(Window.Shape.Size));
                    break;

                case (int)WindowMessages.WM_KILLFOCUS:
                    Window._isDirty = true;
                    Window.Properties._isFocused = false;
                    return IntPtr.Zero;
                case (int)WindowMessages.WM_SETFOCUS:
                    Window._isDirty = true;
                    Window.Properties._isFocused = true;
                    return IntPtr.Zero;

                case (int)WindowMessages.WM_MOVING:
                case (int)WindowMessages.WM_MOVE:
                    Window.LogicDispatcher.Invoke(() =>
                        Window.Callbacks.OnWindowMoved?.Invoke(Window.Shape.Position));
                    return IntPtr.Zero;

                case (int)WindowMessages.WM_NCDESTROY:
                    IntPtr ptr = Win32APIs.GetWindowLongPtrA(hWnd, -21);
                    if (ptr != IntPtr.Zero)
                    {
                        GCHandle.FromIntPtr(ptr).Free();
                        Win32APIs.SetWindowLongPtrA(hWnd, -21, IntPtr.Zero);
                    }
                    break;

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

                case (int)WindowMessages.WM_SETCURSOR:
                    int hitTest = lParam.ToInt32() & 0xFFFF;
                    if (hitTest == HitTest.HTLEFT       || hitTest == HitTest.HTRIGHT     ||
                        hitTest == HitTest.HTTOP        || hitTest == HitTest.HTTOPLEFT   ||
                        hitTest == HitTest.HTTOPRIGHT   || hitTest == HitTest.HTBOTTOM    ||
                        hitTest == HitTest.HTBOTTOMLEFT || hitTest == HitTest.HTBOTTOMRIGHT)
                        return Win32APIs.DefWindowProcW(hWnd, msg, wParam, lParam);
                    else
                        Win32APIs.SetCursor(Win32APIs.LoadCursor(IntPtr.Zero, (int)Window.ActiveCursor.Value));
                    return IntPtr.Zero;

                case (int)WindowMessages.WM_CLOSE:
                    FLogger.Log("Window closed");

                    Window.Properties.IsWindowVisible = false; // Make sure the window is closes seemingly faster by hiding it first
                    if (!Window.Properties.HideWindowOnClose)
                    {
                        Window.LogicDispatcher.Invoke(() =>
                        {
                            Window._isRunning = false;
                            Window.Callbacks.OnWindowClose?.Invoke();
                            Window.Dispose();
                        });
                    }
                    return IntPtr.Zero;

                case (int)WindowMessages.WM_DESTROY:
                    Window.Callbacks.OnWindowDestroy?.Invoke();
                    Win32APIs.PostQuitMessage(0);
                    return IntPtr.Zero;
            }

            // If no special handling is needed, fallback to the default window procedure
            return Win32APIs.DefWindowProcW(hWnd, msg, wParam, lParam);
        }

        private Vector2 oldSize;
        private volatile bool _isResizing = false;

        protected virtual void WindowRzd(Vector2 size)
        {
            if (!_isResizing && size != oldSize)
                Window.LogicDispatcher.Invoke(() => Window.Callbacks.OnWindowEndResize?.Invoke(size));
            oldSize = size;

            Window._isDirty = true;

            if (Window.SkiaDirectCompositionContext != null)
                Window.LogicDispatcher.Invoke(() => Window.SkiaDirectCompositionContext.OnResize(size));

            Window.LogicDispatcher.Invoke(() => Window.FullRedraw()); // Make sure it gets executed a tick later
        }

        public void Dispose()
        {
            
        }
    }
}