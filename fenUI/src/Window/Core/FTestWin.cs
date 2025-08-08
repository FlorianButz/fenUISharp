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
                dwExStyle: (int)(WindowStyles.WS_EX_NOREDIRECTIONBITMAP),
                lpClassName: (string)this.WindowClass,
                lpWindowName: null,
                dwStyle: (int)(WindowStyles.WS_NATIVE | WindowStyles.WS_OVERLAPPEDWINDOW),
                x: (int)position.x,
                y: (int)position.y,
                nWidth: (int)size.x,
                nHeight: (int)size.y,
                hWndParent: IntPtr.Zero,
                hMenu: IntPtr.Zero,
                hInstance: wndClass.hInstance,
                lpParam: IntPtr.Zero
            );

            return created_hWnd;
        }
    }
}