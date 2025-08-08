using System.Runtime.InteropServices;
using FenUISharp.Logging;
using FenUISharp.Native;

namespace FenUISharp
{
    public class FWindowProperties : IDisposable
    {
        private WeakReference<FWindow> window { get; set; }
        public FWindow Window { get => window.TryGetTarget(out var target) ? target : throw new Exception("Window not set."); }

        public bool HideWindowOnClose { get; set; } = false; // Only hides the window when pressing the close button

        public bool IsWindowVisible { get => Win32APIs.IsWindowVisible(Window.hWnd); set => ShowWindow(value ? ShowWindowCommand.SW_SHOW : ShowWindowCommand.SW_HIDE); }
        public bool AllowResize { set => SetWindowStyle((int)WindowStyles.WS_THICKFRAME, value); }
        public bool HasSystemMenu { set => SetWindowStyle((int)WindowStyles.WS_SYSMENU, value); }
        public bool HasMaximizeButton { set => SetWindowStyle((int)WindowStyles.WS_MAXIMIZEBOX, value); }
        public bool HasMinimizeButton { set => SetWindowStyle((int)WindowStyles.WS_MINIMIZEBOX, value); }

        public bool UseMica { set => UpdateMica(value, MicaBackdropType); get => _useMica; }
        private bool _useMica = false;
        public MicaBackdropType MicaBackdropType { get => _micaBackdropType; set => UpdateMica(_useMica, value); }
        private MicaBackdropType _micaBackdropType = MicaBackdropType.MainWindow;

        public bool ExcludeFromPeek
        {
            set { int val = value ? 1 : 0; Win32APIs.DwmSetWindowAttribute(Window.hWnd, 12 /* DWMWA_EXCLUDED_FROM_PEEK */, ref val, sizeof(int)); }
        }

        public bool VisibleInTaskbar { get => _taskbarIconVisible; set { HideTaskbarIcon(value); } }
        internal IntPtr _hiddenOwnerWindowHandle = IntPtr.Zero;
        private bool _taskbarIconVisible;

        public bool UseSystemDarkMode { set { _sysDarkMode = value; UpdateSysDarkmode(); } get => _sysDarkMode; }
        private bool _sysDarkMode = false;

        public bool IsWindowFocused { get => _isFocused; }
        internal bool _isFocused = false;

        public FWindowProperties(FWindow window)
        {
            this.window = new WeakReference<FWindow>(window);
        }

        public void ShowWindow(ShowWindowCommand showWindowCommand)
        {
            Win32APIs.ShowWindow(Window.hWnd, (int)showWindowCommand);
        }

        public void SetWindowStyle(int style, bool enabled)
        {
            int styl = Win32APIs.GetWindowLong(Window.hWnd, (int)WindowLongs.GWL_STYLE);

            if (enabled)
                styl |= style;
            else
                styl &= ~style;

            Win32APIs.SetWindowLong(Window.hWnd, (int)WindowLongs.GWL_STYLE, styl);
        }

        public void OverrideWindowStyle(int style)
            => Win32APIs.SetWindowLong(Window.hWnd, (int)WindowLongs.GWL_STYLE, style);

        public void HideTaskbarIcon(bool isVisible)
        {
            const int GWL_HWNDPARENT = -8;
            _taskbarIconVisible = false;

            if (isVisible)
            {
                // Revert to using no parent
                Win32APIs.SetWindowLongPtr(Window.hWnd, GWL_HWNDPARENT, IntPtr.Zero /*No parent*/);

                if (_hiddenOwnerWindowHandle != IntPtr.Zero)
                {
                    // Destroy old invisible window
                    Win32APIs.DestroyWindow(_hiddenOwnerWindowHandle);
                    _hiddenOwnerWindowHandle = IntPtr.Zero;
                }
            }
            else
            {
                try
                {
                    // Create a dummy window (invisible) to act as the owner
                    _hiddenOwnerWindowHandle = Win32APIs.CreateWindowEx(
                        (int)WindowStyles.WS_EX_TOOLWINDOW,
                        "STATIC", "",
                        (int)WindowStyles.WS_OVERLAPPEDWINDOW,
                        0, 0,
                        0, 0,
                        IntPtr.Zero, IntPtr.Zero,
                        Win32APIs.GetModuleHandle(null), IntPtr.Zero);

                    // Hide window
                    Win32APIs.ShowWindow(_hiddenOwnerWindowHandle, 0); // Ensure it never appears

                    // Set the parent of the main window to the invisible tool window
                    if (Window.hWnd != IntPtr.Zero)
                        Win32APIs.SetWindowLongPtr(Window.hWnd, GWL_HWNDPARENT, _hiddenOwnerWindowHandle);
                }
                catch (Exception e)
                {
                    FLogger.Error($"Exception while creating invisible parent {e.Message}");
                }
            }
        }

        internal virtual void UpdateSysDarkmode()
        {
            Window._isDirty = true;

            const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
            const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19; // Windows 10 1809+
            int useDarkMode = _sysDarkMode ? 1 : 0;

            if (Win32APIs.DwmSetWindowAttribute(Window.hWnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDarkMode, sizeof(int)) != 0)
                // If it fails, try the older one
                Win32APIs.DwmSetWindowAttribute(Window.hWnd, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref useDarkMode, sizeof(int));

            if (Win32APIs.ShouldSystemUseDarkMode() == 1) // Check if the system is in dark mode
                Win32APIs.AllowDarkModeForWindow(Window.hWnd, true);
        }

        internal void UpdateMica(bool useMica, MicaBackdropType backdropType = MicaBackdropType.MainWindow)
        {
            _useMica = useMica;
            _micaBackdropType = backdropType;

            // First, apply the Mica system backdrop
            int micaEffect = useMica ? (int)backdropType : (int)MicaBackdropType.None;
            Win32APIs.DwmSetWindowAttribute(
                Window.hWnd,
                (uint)DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE,
                ref micaEffect,
                Marshal.SizeOf<int>()
            );

            // int frameExtension = useMica ? -1 : 0;

            // // Extend the frame into the client area
            // MARGINS margins = new MARGINS
            // {
            //     cxLeftWidth     = frameExtension,
            //     cxRightWidth    = frameExtension,
            //     cyTopHeight     = frameExtension,
            //     cyBottomHeight  = frameExtension
            // };

            // Win32APIs.DwmExtendFrameIntoClientArea(hWnd, ref margins);
        }

        public void Dispose()
        {
            if (_hiddenOwnerWindowHandle != IntPtr.Zero)
                // Destroy the hidden owner window if it exists
                Win32APIs.DestroyWindow(_hiddenOwnerWindowHandle);
        }
    }
}