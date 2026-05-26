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
        /// <summary>
        /// The HitTestMode specifies the passthrough level of the window.
        /// Passthrough: The window is not interactable in any way
        /// Partial: The window is interactable where FenUI UIObjects are placed
        /// Always: The window is always clickable and blocking
        /// </summary>
        public enum HitTestMode { Passthrough, Partial, Always }
        public HitTestMode WindowHitTest = HitTestMode.Partial; 

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

        public override bool IsAreaClickable(Vector2 mousePosition)
        {
            switch(WindowHitTest)
            {
                case HitTestMode.Passthrough:
                    return false;
                case HitTestMode.Partial:
                    return Surface.MouseHitTest(mousePosition);
                case HitTestMode.Always:
                    return true;
                default:
                    return false;
            }
        }

        internal override void ClearSurface(SKCanvas canvas)
        {
            canvas.Clear(SKColors.Transparent);
        }
    }
}