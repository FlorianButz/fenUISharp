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
        public bool AllowMaximize { set => SetWindowStyle((int)WindowStyles.WS_MAXIMIZEBOX, value); }
        public bool AllowMinimize { set => SetWindowStyle((int)WindowStyles.WS_MINIMIZEBOX, value); }

        public bool UseMica { set => UpdateMica(value, MicaBackdropType); get => _useMica; }
        private bool _useMica = false;
        public MicaBackdropType MicaBackdropType { get => _micaBackdropType; set => UpdateMica(_useMica, value); }
        private MicaBackdropType _micaBackdropType = MicaBackdropType.MainWindow;

        public bool AlwaysOnTop
        {
            set
            {
                _alwaysOnTop = value;
                Win32APIs.SetWindowPos(Window.hWnd, _alwaysOnTop ? -1 /* HWND_TOPMOST */ : -2 /* HWND_NOTOPMOST */, 0, 0, 0, 0, (uint)SetWindowPosFlags.SWP_NOMOVE | (uint)SetWindowPosFlags.SWP_NOSIZE);
            }
            get => _alwaysOnTop;
        }
        private bool _alwaysOnTop = false;

        public bool ExcludeFromAeroPeek { set { int val = value ? 1 : 0; Win32APIs.DwmSetWindowAttribute(Window.hWnd, 12 /* DWMWA_EXCLUDED_FROM_PEEK */, ref val, sizeof(int)); } }

        public bool VisibleInTaskbar { get => _taskbarIconVisible; set { HideTaskbarIcon(value); } }
        internal IntPtr _hiddenOwnerWindowHandle = IntPtr.Zero;
        private bool _taskbarIconVisible;

        public bool UseSystemDarkMode { set { _sysDarkMode = value; UpdateSysDarkmode(); } get => _sysDarkMode; }
        private bool _sysDarkMode = false;

        public bool IsWindowFocused { get => _isFocused; }
        internal bool _isFocused = false;

        private NOTIFYICONDATAA _trayIconData;

        public FWindowProperties(FWindow window)
        {
            this.window = new WeakReference<FWindow>(window);
        }

        public void ShowWindow(ShowWindowCommand showWindowCommand)
        {
            FLogger.Log<FWindowProperties>($"Setting window visibility: {showWindowCommand.ToString()}");
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

        public void CreateTrayIcon(string iconPath, string tooltip)
        {
            if (_trayIconData.hWnd == Window.hWnd) throw new InvalidOperationException($"Another tray icon has already been added for the window {Window.hWnd}!");

            FLogger.Log<FWindowProperties>($"Adding tray icon for window {Window.hWnd}");

            _trayIconData = new NOTIFYICONDATAA
            {
                cbSize = Marshal.SizeOf(typeof(NOTIFYICONDATAA)),
                hWnd = Window.hWnd,
                uID = 1,
                uFlags = (int)NIF.NIF_MESSAGE | (int)NIF.NIF_ICON | (int)NIF.NIF_TIP,
                uCallbackMessage = (int)WindowMessages.WM_USER + 1,
                szTip = tooltip
            };

            FLogger.Log<FWindowProperties>($"Turning image to pointer");

            IntPtr hIcon = Win32APIs.LoadImage(
                IntPtr.Zero,
                iconPath,
                1 /* IMAGE_ICON */,
                0, 0,
                0x00000010 /* LR_LOADFROMFILE */
            );
            _trayIconData.hIcon = hIcon;

            FLogger.Log<FWindowProperties>($"Setting Shell Notify Icon to image with pointer {hIcon}");
            Win32APIs.Shell_NotifyIconA((uint)NIF.NIM_ADD, ref _trayIconData);
        }

        public void SetWindowIcon(string? iconPath, string? smallIconPath = null)
        {
            if (iconPath == null)
            {
                FLogger.Log<FWindowProperties>($"Removing window icon for window {Window.hWnd}");

                Win32APIs.SendMessage(Window.hWnd, 0x0080 /* WM_SETICON */, (IntPtr)1 /* ICON_BIG */, IntPtr.Zero);
                Win32APIs.SendMessage(Window.hWnd, 0x0080 /* WM_SETICON */, (IntPtr)0 /* ICON_SMALL */, IntPtr.Zero);
                return;
            }

            // Load icon from file
            FLogger.Log<FWindowProperties>($"Loading big icon from file...");
            IntPtr hIcon = Win32APIs.LoadImage(IntPtr.Zero, iconPath, 1 /* IMAGE_ICON */, 0, 0, 0x00000010 /* LR_LOADFROMFILE */);
            IntPtr smallHIcon = hIcon; // Setting small icon to big icon by default

            if (smallIconPath != null)
            {
                FLogger.Log<FWindowProperties>($"Loading small icon from file...");
                smallHIcon = Win32APIs.LoadImage(IntPtr.Zero, smallIconPath, 1 /* IMAGE_ICON */, 0, 0, 0x00000010 /* LR_LOADFROMFILE */);
            }

            if (hIcon != IntPtr.Zero && smallHIcon != IntPtr.Zero)
            {
                // Set small and big icon
                FLogger.Log<FWindowProperties>($"Setting big icon ({hIcon}) and small icon ({smallHIcon})...");
                Win32APIs.SendMessage(Window.hWnd, 0x0080 /* WM_SETICON */, (IntPtr)1 /* ICON_BIG */, hIcon);
                Win32APIs.SendMessage(Window.hWnd, 0x0080 /* WM_SETICON */, (IntPtr)0 /* ICON_SMALL */, smallHIcon);
            }
            else
            {
                throw new Exception("Failed to load icon.");
            }
        }


        public void Dispose()
        {

        }

        public void DisposeParent()
        {
            // Revert to using no parent
            Win32APIs.SetWindowLongPtr(Window.hWnd, -8 /* GWL_HWNDPARENT */, IntPtr.Zero /*No parent*/);

            if (_hiddenOwnerWindowHandle != IntPtr.Zero)
                // Destroy the hidden owner window if it exists
                Win32APIs.DestroyWindow(_hiddenOwnerWindowHandle);

            _hiddenOwnerWindowHandle = IntPtr.Zero;
        }
    }
}