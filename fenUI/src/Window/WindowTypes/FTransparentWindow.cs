using System.Runtime.InteropServices;
using FenUISharp.Mathematics;
using FenUISharp.Native;
using FenUISharp.Objects;
using Microsoft.Win32;
using SkiaSharp;

namespace FenUISharp
{
    public class FTransparentWindow : FWindow
    {
        public FTransparentWindow(
            string title, string className, Vector2? position, Vector2? size) :
            base(title, className, position, size)
        {
            Properties.AllowResize = false;
        }

        protected override nint CreateWindow(WNDCLASSEX wndClass, Vector2 position, Vector2 size)
        {
            if (position == new Vector2(-1, -1))
            {
                var r = Win32APIs.GetMonitorRect(0);
                position = new Vector2((r.right - r.left) / 2 - (size.x / 2), (r.bottom - r.top) / 2 - (size.y / 2));
            }

            var hWnd = Win32APIs.CreateWindowExA(
                (int)WindowStyles.WS_EX_NOREDIRECTIONBITMAP,
                this.WindowClass,
                this.WindowTitle,
                (int)WindowStyles.WS_POPUP,
                (int)position.x,
                (int)position.y,
                (int)size.x,
                (int)size.y,
                IntPtr.Zero,
                IntPtr.Zero,
                wndClass.hInstance,
                IntPtr.Zero);

            return hWnd;
        }

        internal override void ClearSurface(SKCanvas canvas)
        {
            canvas.Clear(SKColors.Transparent);
        }
    }
}