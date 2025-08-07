using System.Runtime.InteropServices;
using FenUISharp.Logging;
using FenUISharp.Mathematics;
using FenUISharp.Native;

namespace FenUISharp
{
    public class FTestWin : FWindow
    {
        public FTestWin(string title, string className, Vector2? position = null, Vector2? size = null) : base(title, className, position, size)
        {
        }

        public override nint CreateWindow(WNDCLASSEX wndClass, Vector2 position, Vector2 size)
        {
            if (position == new Vector2(-1, -1))
            {
                var r = Win32APIs.GetMonitorRect(0);
                position = new Vector2((r.right - r.left) / 2 - (size.x / 2), (r.bottom - r.top) / 2 - (size.y / 2));
            }

            var created_hWnd = Win32APIs.CreateWindowExA(
                dwExStyle: (int)WindowStyles.WS_EX_NOREDIRECTIONBITMAP | (int)WindowStyles.WS_EX_APPWINDOW,
                lpClassName: (string)this.WindowClass,
                lpWindowName: (string)this.WindowTitle,
                dwStyle: (int)(WindowStyles.WS_NATIVE | WindowStyles.WS_VISIBLE),
                x: (int)position.x,
                y: (int)position.y,
                nWidth: (int)size.x,
                nHeight: (int)size.y,
                hWndParent: IntPtr.Zero,
                hMenu: IntPtr.Zero,
                hInstance: wndClass.hInstance,
                lpParam: IntPtr.Zero
            );

            UpdateMica();

            return created_hWnd;
        }

        public void UpdateMica()
        {
            // First, apply the Mica system backdrop
            int micaEffect = true ? (true ? (int)DWM_SYSTEMBACKDROP_TYPE.DWMSBT_MAINWINDOW : (int)DWM_SYSTEMBACKDROP_TYPE.DWMSBT_TRANSIENTWINDOW) : (int)DWM_SYSTEMBACKDROP_TYPE.DWMSBT_NONE;
            Win32APIs.DwmSetWindowAttribute(
                hWnd,
                (uint)DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE,
                ref micaEffect,
                Marshal.SizeOf<int>()
            );

            // Extend the frame into the client area
            MARGINS margins = new MARGINS
            {
                cxLeftWidth = -1,
                cxRightWidth = -1,
                cyTopHeight = -1,
                cyBottomHeight = -1
            };

            Win32APIs.DwmExtendFrameIntoClientArea(hWnd, ref margins);
        }
    }
}