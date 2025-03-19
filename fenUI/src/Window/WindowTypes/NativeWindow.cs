namespace FenUISharp
{
    public class NativeWindow : Window
    {
        private bool _hideTaskbarIcon;

        // Only needed when taskbar icon is hidden
        private static IntPtr hiddenOwnerWindow = IntPtr.Zero;

        public NativeWindow(
            string title, string className,
            Vector2? windowSize = null, Vector2? windowPosition = null,
            bool hideTaskbarIcon = false, bool alwaysOnTop = false) :
        base(title, className, windowSize, windowPosition, alwaysOnTop)
        {
            _hideTaskbarIcon = hideTaskbarIcon;
            SetTaskbarIconVisibility(_hideTaskbarIcon);
        }

        protected override IntPtr CreateWin32Window(WNDCLASSEX wndClass, Vector2? size, Vector2? position)
        {
            var hWnd = CreateWindowEx(
                            (int)WindowStyles.WS_VISIBLE,
                            this.WindowClass,
                            this.WindowTitle,
                            (int)WindowStyles.WS_EX_APPWINDOW,
                            0, 0, 0, 0,
                            IntPtr.Zero,
                            IntPtr.Zero,
                            wndClass.hInstance,
                            IntPtr.Zero);

            return hWnd;
        }

        public bool SetTaskbarIconVisibility(bool visible)
        {
            try
            {
                if (!visible)
                {
                    // Create hidden owner window if it doesn't exist
                    if (hiddenOwnerWindow == IntPtr.Zero)
                    {
                        hiddenOwnerWindow = CreateWindowEx(
                            (int)WindowStyles.WS_EX_TOOLWINDOW,
                            "STATIC",
                            "HiddenOwnerWindow",
                            (int)WindowStyles.WS_OVERLAPPED,
                            0, 0, 0, 0,
                            IntPtr.Zero,
                            IntPtr.Zero,
                            GetModuleHandle(null),
                            IntPtr.Zero);
                    }

                    // Set the hidden window as the owner of our target window
                    if (hiddenOwnerWindow != IntPtr.Zero)
                    {
                        return SetWindowLong(hWnd, GWL_HWNDPARENT, hiddenOwnerWindow.ToInt32()) == 1;
                    }
                }
                else
                {
                    // Remove owner to show taskbar icon
                    return SetWindowLong(hWnd, GWL_HWNDPARENT, IntPtr.Zero.ToInt32()) == 1;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            if (hiddenOwnerWindow != IntPtr.Zero)
            {
                DestroyWindow(hiddenOwnerWindow);
                hiddenOwnerWindow = IntPtr.Zero;
            }
        }
    }
}