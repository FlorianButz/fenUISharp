using System.Runtime.InteropServices;
using System.Text;

namespace FenUISharp.Native
{
    internal static class WindowStyles
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
        public const int WS_EX_TRANSPARENT = 0x00000020;
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

    internal static class GLOBALHOOKTYPE
    {
        public const int WH_MOUSE_LL = 14;
        public const int WH_KEYBOARD_LL = 13;
        public const int WM_MOUSEWHEEL = 0x020A;
        public const int WM_KEYDOWN = 0x0100;
        public const int WM_KEYUP = 0x0101;
    }

    internal static class DWMWINDOWATTRIBUTE
    {
        public const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
        public const int DWMWA_MICA_EFFECT = 1029;      // Dark mode Mica
        public const int DWMWA_CAPTION_COLOR = 35;      // Title bar color
        public const int DWMWA_TEXT_COLOR_SYSTEM = 36;   // System text color adaptation
        public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    }

    internal enum WindowMessages : int
    {
        WM_CHAR = 0x0102,
        WM_NCCREATE = 0x0081,
        WM_NCDESTROY = 0x0082,
        WM_CREATE = 0x0001,
        WM_DESTROY = 0x0002,
        WM_SIZE = 0x0005,
        WM_PAINT = 0x000F,
        WM_CLOSE = 0x0010,
        WM_SETFOCUS = 0x0007,
        WM_KILLFOCUS = 0x0008,
        WM_SETTINGCHANGE = 0x001A,
        WM_DEVICECHANGE = 0x0219,
        WM_INITMENUPOPUP = 0x0117,
        WM_NCHITTEST = 0x0084,
        WM_DPICHANGED = 0x02E0,
        WM_GETMINMAXINFO = 0x24,
        WM_ACTIVATE = 0x0006,
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
        WM_MOVING = 0x0216,
        WM_MOVE = 0x0003,
        WM_SIZING = 0x0214,
        WM_EXITSIZEMOVE = 0x0232,
        WM_ENTERSIZEMOVE = 0x0231,
        WM_MOUSEWHEEL = 0x020A,
        WM_WINDOWPOSCHANGING = 0x0046,
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

    public static class HitTest
    {
        public const int HTNOWHERE = 0;
        public const int HTLEFT = 10;
        public const int HTRIGHT = 11;
        public const int HTTOP = 12;
        public const int HTTOPLEFT = 13;
        public const int HTTOPRIGHT = 14;
        public const int HTBOTTOM = 15;
        public const int HTBOTTOMLEFT = 16;
        public const int HTBOTTOMRIGHT = 17;
        public const int HTCLIENT = 1;
        public const int HTTRANSPARENT = -1;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BITMAPINFOHEADER
    {
        public uint biSize;
        public int biWidth;
        public int biHeight;
        public ushort biPlanes;
        public ushort biBitCount;
        public uint biCompression;
        public uint biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public uint biClrUsed;
        public uint biClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RGBQUAD
    {
        public byte rgbBlue;
        public byte rgbGreen;
        public byte rgbRed;
        public byte rgbReserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BITMAPINFO
    {
        public BITMAPINFOHEADER bmiHeader;
        public RGBQUAD bmiColors; // only one here, we don’t need a full array for 32bpp
    }

    public static class CombineModes
    {
        public const int RGN_AND = 1;
        public const int RGN_OR = 2;
        public const int RGN_XOR = 3;
        public const int RGN_DIFF = 4;
        public const int RGN_COPY = 5;
    }

    public static class PolyFillMode
    {
        public const int ALTERNATE = 1; // even-odd
        public const int WINDING = 2;
    }

    public enum MouseMessages
    {
        WM_LBUTTONDOWN = 0x0201,
        WM_LBUTTONUP = 0x0202,
        WM_RBUTTONDOWN = 0x0204,
        WM_RBUTTONUP = 0x0205,
        WM_MBUTTONDOWN = 0x0207,
        WM_MBUTTONUP = 0x0208,
    }


    public delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
    public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    public struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KBDLLHOOKSTRUCT
    {
        public int vkCode;
        public int scanCode;
        public int flags;
        public int time;
        public IntPtr dwExtraInfo;
    }

    internal static class Win32APIs
    {

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr SetWindowsHookEx(
            int idHook,
            LowLevelMouseProc lpfn,
            IntPtr hMod,
            uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr SetWindowsHookEx(
            int idHook,
            LowLevelKeyboardProc lpfn,
            IntPtr hMod,
            uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr CallNextHookEx(
            IntPtr hhk,
            int nCode,
            IntPtr wParam,
            IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern bool SetWindowDisplayAffinity(IntPtr hwnd, double dwAffinity);

        [DllImport("gdi32.dll")]
        internal static extern IntPtr CreateRectRgn(int left, int top, int right, int bottom);

        [DllImport("gdi32.dll")]
        internal static extern IntPtr CreatePolygonRgn(POINT[] pptl, int cPoints, int fnPolyFillMode);

        [DllImport("gdi32.dll")]
        internal static extern int CombineRgn(IntPtr hrgnDest, IntPtr hrgnSrc1, IntPtr hrgnSrc2, int fnCombineMode);

        [DllImport("user32.dll")]
        internal static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

        [DllImport("gdi32.dll", SetLastError = true)]
        internal static extern IntPtr CreateDIBSection(
            IntPtr hdc,
            [In] ref BITMAPINFO pbmi,
            uint iUsage,
            out IntPtr ppvBits,
            IntPtr hSection,
            uint dwOffset);

        [DllImport("gdi32.dll")]
        internal static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        internal static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll")]
        internal static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        internal static extern bool DeleteDC(IntPtr hdc);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("user32.dll")]
        internal static extern IntPtr SetCapture(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        internal static extern IntPtr ReleaseCapture(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern uint GetDpiForWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        internal static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        internal static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

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
        internal static extern bool PeekMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

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
        internal static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

        [DllImport("user32.dll")]
        internal static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll")]
        internal static extern IntPtr LoadImage(IntPtr hInstance, string lpszName, uint uType, int cx, int cy, uint fuLoad);

        [DllImport("shell32.dll")]
        internal static extern bool Shell_NotifyIconA(uint dwMessage, ref NOTIFYICONDATAA lpData);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern int SetCurrentProcessExplicitAppUserModelID(string AppID);

        [DllImport("user32.dll")]
        internal static extern bool SetProcessDpiAwarenessContext(IntPtr dpiContext);

        [DllImport("user32.dll", CharSet = CharSet.Ansi)]
        internal static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devMode);

        [DllImport("user32.dll")]
        internal static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

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


    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct DEVMODE
    {
        private const int CCHDEVICENAME = 32;
        private const int CCHFORMNAME = 32;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
        public string dmDeviceName;
        public ushort dmSpecVersion;
        public ushort dmDriverVersion;
        public ushort dmSize;
        public ushort dmDriverExtra;
        public uint dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public uint dmDisplayOrientation;
        public uint dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)]
        public string dmFormName;
        public ushort dmLogPixels;
        public uint dmBitsPerPel;
        public uint dmPelsWidth;
        public uint dmPelsHeight;
        public uint dmDisplayFlags;
        public uint dmDisplayFrequency; // <- REFRESH RATE
        public uint dmICMMethod;
        public uint dmICMIntent;
        public uint dmMediaType;
        public uint dmDitherType;
        public uint dmReserved1;
        public uint dmReserved2;
        public uint dmPanningWidth;
        public uint dmPanningHeight;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct DISPLAY_DEVICE
    {
        public int cb;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceString;
        public uint StateFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceKey;
    }

    // DPI awareness
    internal static class DPI_AWARENESS_CONTEXT
    {
        public static readonly IntPtr UNAWARE = new IntPtr(-1);
        public static readonly IntPtr SYSTEM_AWARE = new IntPtr(-2);
        public static readonly IntPtr PER_MONITOR_AWARE = new IntPtr(-3);
        public static readonly IntPtr PER_MONITOR_AWARE_V2 = new IntPtr(-4);
        public static readonly IntPtr UNAWARE_GDISCALED = new IntPtr(-5);
    }

    // Struct for monitor info
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct MONITORINFOEX
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
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

    public static class WindowLongs
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