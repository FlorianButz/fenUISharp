using System.Runtime.InteropServices;

namespace FenUISharp.Native
{
    public class Win32APIs
    {

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

    public enum DWM_SYSTEMBACKDROP_TYPE
    {
        DWMSBT_AUTO = 1,
        DWMSBT_NONE = 2,
        DWMSBT_MAINWINDOW = 3,
        DWMSBT_TRANSIENTWINDOW = 4,
        DWMSBT_TABBEDWINDOW = 5
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

    public enum WindowStyles : long
    {
        WS_POPUP = unchecked((int)0x80000000),
        WS_EX_LAYERED = 0x00080000,
        WS_EX_APPWINDOW = 0x00040000,
        WS_EX_TOOLWINDOW = 0x00000080,
        WS_VISIBLE = 0x10000000L,
        WS_OVERLAPPED = 0x00000000,
        WS_CAPTION = 0x00C00000,
        WS_SYSMENU = 0x00080000,
        WS_THICKFRAME = 0x00040000,
        WS_MINIMIZEBOX = 0x00020000,
        WS_MAXIMIZEBOX = 0x00010000,
        WS_BORDER = 0x00800000
    }

    public enum WindowLongs : int
    {
        GWL_EXSTYLE = -20,
        GWL_STYLE = -16
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

    public enum WindowMessages : uint
    {
        WM_SETTINGCHANGE = 0x001A,
        WM_DEVICECHANGE = 0x0219,
        WM_INITMENUPOPUP = 0x0117,
        WM_NCHITTEST = 0x0084,

        WM_DPICHANGED = 0x02E0,

        WM_SETFOCUS = 0x0007,
        WM_KILLFOCUS = 0x0008,

        WM_NCDESTROY = 0x0082,

        WM_GETMINMAXINFO = 0x24,

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
        WM_CLOSE = 0x0010,

        WM_MOVING = 0x0216,
        WM_MOVE = 0x0003,
        WM_SIZING = 0x0214,
        WM_EXITSIZEMOVE = 0x0232,
        WM_ENTERSIZEMOVE = 0x0231
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