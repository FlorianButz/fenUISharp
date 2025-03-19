using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;
using FenUISharpTest1;
using SkiaSharp;
using System.Drawing;

namespace FenUISharp
{
    public abstract class Window : IDisposable
    {

        #region Window Properties

        public string WindowTitle { get; private set; }
        public string WindowClass { get; private set; }

        public Vector2 WindowPosition { get; private set; }
        public Vector2 WindowSize { get; private set; }

        public IntPtr hWnd { get; private set; }

        public DragDropHandler DropTarget { get; private set; }

        private bool _alwaysOnTop;

        #endregion

        #region Actions

        public Action<MouseInputCode> MouseAction { get; set; }
        public Action<MouseInputCode> MouseMove { get; set; }
        public Action<Vector2> OnWindowResize { get; set; }

        public Action<MouseInputCode> OnTrayIconClick { get; set; }

        #endregion

        #region Private

        private readonly WndProcDelegate _wndProcDelegate;

        #endregion

        #region Constructors

        public Window(
            string title, string className,
            Vector2? windowSize = null, Vector2? windowPosition = null,
            bool alwaysOnTop = false
        )
        {
            WindowFeatures.TryInitialize(); // Initialize all window features
            _wndProcDelegate = WindowsProcedure;

            // Pre initialize OLE DragDrop
            DragDropRegistration.Initialize();

            WindowTitle = title;
            WindowClass = className;

            _alwaysOnTop = alwaysOnTop;

            hWnd = CreateWin32Window(RegisterClass(), windowSize, windowPosition);
            SetAlwaysOnTop(_alwaysOnTop);
        }

        #endregion

        public void SetWindowVisibility(bool visible)
        {
            ShowWindow(hWnd, visible ? 1 : 0);
        }

        public void SetAlwaysOnTop(bool alwaysOnTop)
        {
            SetWindowPos(hWnd, alwaysOnTop ? HWND_TOPMOST : HWND_NOTOPMOST, 0, 0, 0, 0, (uint)SetWindowPosFlags.SWP_NOMOVE | (uint)SetWindowPosFlags.SWP_NOSIZE);
        }

        protected WNDCLASSEX RegisterClass()
        {
            WNDCLASSEX wndClass = new WNDCLASSEX
            {
                cbSize = (uint)Marshal.SizeOf(typeof(WNDCLASSEX)),
                style = 0x0020,
                lpfnWndProc = _wndProcDelegate,
                hInstance = Marshal.GetHINSTANCE(typeof(Program).Module),
                lpszClassName = WindowClass
            };

            RegisterClassExA(ref wndClass);

            return wndClass;
        }

        protected abstract IntPtr CreateWin32Window(WNDCLASSEX wndClass,Vector2? size, Vector2? position);

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

        public virtual void Dispose()
        {
            DestroyWindow(hWnd);
        }

        protected Cursor GetCursorAtMousePosition()
        {
            // Implement actual logic

            return Cursor.ARROW;
        }

        protected IntPtr WindowsProcedure(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case (int)WindowMessages.WM_USER + 1:
                    if ((int)lParam == (int)WindowMessages.WM_RBUTTONDOWN)
                        OnTrayIconClick?.Invoke(MouseInputCode.RDown);
                    if ((int)lParam == (int)WindowMessages.WM_LBUTTONDOWN)
                        OnTrayIconClick?.Invoke(MouseInputCode.LDown);
                    if ((int)lParam == (int)WindowMessages.WM_MBUTTONDOWN)
                        OnTrayIconClick?.Invoke(MouseInputCode.MDown);
                    if ((int)lParam == (int)WindowMessages.WM_RBUTTONUP)
                        OnTrayIconClick?.Invoke(MouseInputCode.RUp);
                    if ((int)lParam == (int)WindowMessages.WM_LBUTTONUP)
                        OnTrayIconClick?.Invoke(MouseInputCode.LUp);
                    if ((int)lParam == (int)WindowMessages.WM_MBUTTONUP)
                        OnTrayIconClick?.Invoke(MouseInputCode.MUp);
                    return IntPtr.Zero;

                // case (uint)Win32Helper.WindowMessages.WM_RENDER:
                //     UpdateWindow();
                //     return IntPtr.Zero;

                // Later move to overlay wnd sub class
                // case (int)Win32Helper.WindowMessages.WM_ACTIVATE:
                //     SetAlwaysOnTop();
                //     return IntPtr.Zero;

                case (int)WindowMessages.WM_SIZE:
                    OnWindowResize?.Invoke(new Vector2(wParam, lParam));
                    return IntPtr.Zero;

                case (int)WindowMessages.WM_KEYDOWN:
                    return IntPtr.Zero;
                case (int)WindowMessages.WM_LBUTTONDOWN:
                    MouseAction?.Invoke(MouseInputCode.LDown);
                    return IntPtr.Zero;
                case (int)WindowMessages.WM_RBUTTONDOWN:
                    MouseAction?.Invoke(MouseInputCode.RDown);
                    return IntPtr.Zero;
                case (int)WindowMessages.WM_MBUTTONDOWN:
                    MouseAction?.Invoke(MouseInputCode.MDown);
                    return IntPtr.Zero;
                case (int)WindowMessages.WM_LBUTTONUP:
                    MouseAction?.Invoke(MouseInputCode.LUp);
                    return IntPtr.Zero;
                case (int)WindowMessages.WM_RBUTTONUP:
                    MouseAction?.Invoke(MouseInputCode.RUp);
                    return IntPtr.Zero;
                case (int)WindowMessages.WM_MBUTTONUP:
                    MouseAction?.Invoke(MouseInputCode.MUp);
                    return IntPtr.Zero;

                case (int)WindowMessages.WM_SETCURSOR:
                    SetCursor(LoadCursor(IntPtr.Zero, (int)GetCursorAtMousePosition()));
                    return IntPtr.Zero;

                case (int)WindowMessages.WM_CLOSE:
                    Dispose();
                    return IntPtr.Zero;

                case (int)WindowMessages.WM_DESTROY:
                    Dispose();
                    PostQuitMessage(0);
                    return IntPtr.Zero;
            }
            return DefWindowProcW(hWnd, msg, wParam, lParam);
        }

        #region Windows

        public const uint IMAGE_ICON = 1;
        public const uint LR_LOADFROMFILE = 0x00000010;

        public const int GWL_HWNDPARENT = -8;

        public const int HWND_TOPMOST = -1;
        public const int HWND_NOTOPMOST = -2;

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        protected static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll", SetLastError = true)]
        protected static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        protected static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        protected static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

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

        [DllImport("user32.dll")]
        protected static extern IntPtr SetCursor(IntPtr hCursor);

        [DllImport("user32.dll")]
        protected static extern IntPtr LoadCursor(IntPtr hInstance, int lpCursorName);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        protected static extern ushort RegisterClassExA(ref WNDCLASSEX lpwcx);

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

        #endregion
    }

    public delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [Flags]
    public enum SetWindowPosFlags : uint
    {
        SWP_NOMOVE = 0x0002,
        SWP_NOSIZE = 0x0001,
        SWP_SHOWWINDOW = 0x0040
    }

    public enum WindowStyles : long
    {
        WS_POPUP = unchecked((int)0x80000000),
        WS_EX_LAYERED = 0x00080000,
        WS_EX_APPWINDOW = 0x00040000,
        WS_EX_TOOLWINDOW = 0x00000080,
        WS_VISIBLE = 0x10000000L,
        WS_OVERLAPPED = 0x00000000
    }

    public enum WindowLongs : int
    {
        GWL_EXSTYLE = -20
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

    public enum MouseInputCode
    {
        LDown, LUp,
        RDown, RUp,
        MDown, MUp
    }

    public enum Cursor : int
    {
        ARROW = 32512,
        IBEAM = 32513,
        WAIT = 32514,
        BLOCK = 32648,
        HAND = 32649
    }

    public enum WindowMessages : uint
    {
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
        WM_CLOSE = 0x0010 //,
        // WM_RENDER = WM_USER + 2
    }
}