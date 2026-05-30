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

            var created_hWnd = Win32APIs.CreateWindowExA(
                dwExStyle:      (int)WindowStyles.WS_EX_NOREDIRECTIONBITMAP,
                lpClassName:    (string)this.WindowClass,
                lpWindowName:   this.WindowTitle,
                dwStyle:        (int)WindowStyles.WS_POPUP,
                x:              (int)position.x,
                y:              (int)position.y,
                nWidth:         (int)size.x,
                nHeight:        (int)size.y,
                hWndParent:     IntPtr.Zero,
                hMenu:          IntPtr.Zero,
                hInstance:      wndClass.hInstance,
                lpParam:        IntPtr.Zero
            );

            return created_hWnd;
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