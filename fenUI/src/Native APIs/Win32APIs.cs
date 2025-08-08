using System.Runtime.InteropServices;
using System.Text;

namespace FenUISharp.Native
{
    internal class WindowStyles
    {
        // Premade composites
        public const int WS_OVERLAPPEDWINDOW =
            (int)WindowStyles.WS_OVERLAPPED |
            (int)WindowStyles.WS_CAPTION |
            (int)WindowStyles.WS_SYSMENU |
            (int)WindowStyles.WS_THICKFRAME |
            (int)WindowStyles.WS_MINIMIZEBOX |
            (int)WindowStyles.WS_MAXIMIZEBOX;

        public const int WS_NATIVE =
            (int)WindowStyles.WS_OVERLAPPED |
            (int)WindowStyles.WS_CAPTION |
            (int)WindowStyles.WS_SYSMENU |
            (int)WindowStyles.WS_MINIMIZEBOX;

        public const int WS_NOTITLEBAR =
            (int)WindowStyles.WS_OVERLAPPED |
            (int)WindowStyles.WS_POPUP |
            (int)WindowStyles.WS_THICKFRAME;

        // Extended window styles
        public const int WS_EX_NOREDIRECTIONBITMAP = 0x00200000;
        public const int WS_EX_LAYERED = 0x00080000;
        public const int WS_EX_APPWINDOW = 0x00040000;
        public const int WS_EX_TOOLWINDOW = 0x00000080;

        // Window styles
        public const int WS_POPUP = unchecked((int)0x80000000);
        public const int WS_VISIBLE = 0x10000000;
        public const int WS_OVERLAPPED = 0x00000000;
        public const int WS_CAPTION = 0x00C00000;
        public const int WS_SYSMENU = 0x00080000;
        public const int WS_THICKFRAME = 0x00040000;
        public const int WS_MINIMIZEBOX = 0x00020000;
        public const int WS_MAXIMIZEBOX = 0x00010000;
        public const int WS_BORDER = 0x0080000;
    }

    internal class DWMWINDOWATTRIBUTE
    {
        public const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
        public const int DWMWA_MICA_EFFECT = 1029;      // Dark mode Mica
        public const int DWMWA_CAPTION_COLOR = 35;      // Title bar color
        public const int DWMWA_TEXT_COLOR_SYSTEM = 36;   // System text color adaptation
        public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    }

    internal class WindowMessages
    {
        public const uint WM_CHAR = 0x0102;
        public const uint WM_NCCREATE = 0x0081;
        public const uint WM_NCDESTROY = 0x0082;
        public const uint WM_CREATE = 0x0001;
        public const uint WM_DESTROY = 0x0002;
        public const uint WM_SIZE = 0x0005;
        public const uint WM_PAINT = 0x000F;
        public const uint WM_CLOSE = 0x0010;
        public const uint WM_SETFOCUS = 0x0007;
        public const uint WM_KILLFOCUS = 0x0008;
        public const int WM_SETTINGCHANGE = 0x001A;
        public const int WM_DEVICECHANGE = 0x0219;
        public const int WM_INITMENUPOPUP = 0x0117;
        public const int WM_NCHITTEST = 0x0084;
        public const int WM_DPICHANGED = 0x02E0;
        public const int WM_GETMINMAXINFO = 0x24;
        public const int WM_ACTIVATE = 0x0006;
        public const int WM_KEYDOWN = 0x0100;
        public const int WM_MOUSEMOVE = 0x0200;
        public const int WM_LBUTTONDOWN = 0x0201;
        public const int WM_LBUTTONUP = 0x0202;
        public const int WM_MBUTTONDOWN = 0x0207;
        public const int WM_MBUTTONUP = 0x0208;
        public const int WM_RBUTTONDOWN = 0x0204;
        public const int WM_RBUTTONUP = 0x0205;
        public const int WM_MOUSEHOVER = 0x02A1;
        public const int WM_MOUSELEAVE = 0x02A3;
        public const int WM_DROPFILES = 0x0233;
        public const int WM_SETCURSOR = 0x0020;
        public const int WM_TIMER = 0x0113;
        public const int WM_QUIT = 0x0012;
        public const int WM_SETICON = 0x80;
        public const int WM_USER = 0x0400;
        public const int WM_COMMAND = 0x0111;
        public const int WM_MENUDRAG = 0x123;
        public const int WM_MOVING = 0x0216;
        public const int WM_MOVE = 0x0003;
        public const int WM_SIZING = 0x0214;
        public const int WM_EXITSIZEMOVE = 0x0232;
        public const int WM_ENTERSIZEMOVE = 0x023;
    }

    public enum ShowWindowCommand : int
    {
        SW_HIDE = 0,
        SW_SHOWNORMAL = 1,
        SW_NORMAL = 1,
        SW_SHOWMINIMIZED = 2,
        SW_SHOWMAXIMIZED = 3,
        SW_MAXIMIZE = 3,
        SW_SHOWNOACTIVATE = 4,
        SW_SHOW = 5,
        SW_MINIMIZE = 6,
        SW_SHOWMINNOACTIVE = 7,
        SW_SHOWNA = 8,
        SW_RESTORE = 9,
        SW_SHOWDEFAULT = 10,
        SW_FORCEMINIMIZE = 11
    }

    public class HitTest
    {
        public const int HTLEFT = 10;
        public const int HTRIGHT = 11;
        public const int HTTOP = 12;
        public const int HTTOPLEFT = 13;
        public const int HTTOPRIGHT = 14;
        public const int HTBOTTOM = 15;
        public const int HTBOTTOMLEFT = 16;
        public const int HTBOTTOMRIGHT = 17;
        public const int HTCLIENT = 1;
    }

    internal class Win32APIs
    {
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr SetParent(IntPtr hWnd, IntPtr hWndParent);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern void PostQuitMessage(int nExitCode);

        [DllImport("user32.dll")]
        internal static extern IntPtr SetCursor(IntPtr hCursor);

        [DllImport("user32.dll")]
        internal static extern IntPtr LoadCursor(IntPtr hInstance, int lpCursorName);

        [DllImport("uxtheme.dll", EntryPoint = "#135")]
        internal static extern int ShouldSystemUseDarkMode();

        [DllImport("uxtheme.dll", EntryPoint = "#136")]
        internal static extern void AllowDarkModeForWindow(IntPtr hWnd, bool allow);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int GetWindowTextA(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int SetWindowTextA(IntPtr hWnd, string lpString);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        internal static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetWindowLongPtrA(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        internal static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll")]
        internal static extern IntPtr SetWindowLongPtrA(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll")]
        internal static extern bool SetLayeredWindowAttributes(IntPtr hWnd, uint crKey, byte bAlpha, uint dwFlags);

        [DllImport("user32.dll")]
        internal static extern bool GetLayeredWindowAttributes(IntPtr hWnd, out uint pcrKey, out byte pbAlpha, out uint pdwFlags);

        [DllImport("user32.dll")]
        internal static extern bool UpdateLayeredWindow(IntPtr hWnd, IntPtr hdcDst, ref POINT pptDst, ref SIZE psize, IntPtr hdcSrc, ref POINT pptSrc, uint crKey, ref BLENDFUNCTION pblend, uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        internal static extern ushort RegisterClassExA(ref WNDCLASSEX lpwcx);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr CreateWindowEx(
            int dwExStyle,
            string lpClassName,
            string lpWindowName,
            int dwStyle,
            int X,
            int Y,
            int nWidth,
            int nHeight,
            IntPtr hWndParent,
            IntPtr hMenu,
            IntPtr hInstance,
            IntPtr lpParam);


        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        internal static extern IntPtr CreateWindowExA(
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

        [DllImport("dwmapi.dll")]
        internal static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

        [DllImport("dwmapi.dll")]
        internal static extern int DwmSetWindowAttribute(
            IntPtr hwnd,
            uint attr,
            ref int attrValue,
            int attrSize);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool SetWindowSize(IntPtr hWnd, int width, int height);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll")]
        internal static extern IntPtr DefWindowProcW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetWindowLongPtrW(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        internal static extern IntPtr SetWindowLongPtrW(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll")]
        internal static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        internal static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        internal static extern bool GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        [DllImport("user32.dll")]
        internal static extern bool TranslateMessage([In] ref MSG lpMsg);

        [DllImport("user32.dll")]
        internal static extern IntPtr DispatchMessage([In] ref MSG lpmsg);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        internal static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        internal static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);


        [DllImport("user32.dll")]
        internal static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip,
            MonitorEnumDelegate lpfnEnum, IntPtr dwData);

        internal delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor,
            ref RECT lprcMonitor, IntPtr dwData);

        internal static RECT GetMonitorRect(int monitorIndex)
        {
            RECT rect = new RECT();
            int count = 0;

            MonitorEnumDelegate callback = (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData) =>
            {
                if (count == monitorIndex)
                {
                    rect = lprcMonitor;
                    return false; // Stop enumeration after finding the desired monitor
                }
                count++;
                return true;
            };

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero);
            return rect;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SIZE
    {
        public int cx;
        public int cy;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BLENDFUNCTION
    {
        public byte BlendOp;
        public byte BlendFlags;
        public byte SourceConstantAlpha;
        public byte AlphaFormat;
    }

    // Alpha blend options used in BLENDFUNCTION
    public enum AlphaBlendOptions : byte
    {
        AC_SRC_OVER = 0x00,
        AC_SRC_ALPHA = 0x01
    }

    // DWM_SYSTEMBACKDROP_TYPE
    public enum MicaBackdropType
    {
        Auto = 1,
        None = 2,
        MainWindow = 3,
        TransientWindow = 4,
        // DWMSBT_TABBEDWINDOW = 5
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MARGINS
    {
        public int cxLeftWidth;
        public int cxRightWidth;
        public int cyTopHeight;
        public int cyBottomHeight;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MINMAXINFO
    {
        public POINT ptReserved;
        public POINT ptMaxSize;
        public POINT ptMaxPosition;
        public POINT ptMinTrackSize;
        public POINT ptMaxTrackSize;
    }

    public delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    public struct PAINTSTRUCT
    {
        public IntPtr hdc;
        public bool fErase;
        public RECT rcPaint;
        public bool fRestore;
        public bool fIncUpdate;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] rgbReserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int left, top, right, bottom;

        public int Width => right - left;
        public int Height => bottom - top;
    }

    [Flags]
    public enum SetWindowPosFlags : uint
    {
        SWP_NOMOVE = 0x0002,
        SWP_NOSIZE = 0x0001,
        SWP_SHOWWINDOW = 0x0040,
        SWP_NOZORDER = 0x0004,
        SWP_NOACTIVATE = 0x0010
    }
    
    public class WindowLongs
    {
        public const int GWL_EXSTYLE = -20;
        public const int GWL_STYLE = -16;
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

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
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
        public string lpszMenuName;
        public string lpszClassName;
        public IntPtr hIconSm;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CREATESTRUCT
    {
        public IntPtr lpCreateParams;
        public IntPtr hInstance;
        public IntPtr hMenu;
        public IntPtr hwndParent;
        public int cy;
        public int cx;
        public int y;
        public int x;
        public int style;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpszName;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpszClass;
        public uint dwExStyle;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public UIntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public POINT pt;
    }
}