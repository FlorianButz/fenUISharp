using System.Runtime.InteropServices;
using FenUISharp.Mathematics;
using FenUISharp.Native;
using Microsoft.Win32;
using SkiaSharp;

namespace FenUISharp
{
    public class FTransparentWindow : FWindow
    {
        public FTransparentWindow(
            string title, string className, Vector2? windowSize, Vector2? windowPosition) :
            base(title, className, windowSize, windowPosition)
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
                (int)WindowStyles.WS_EX_LAYERED,
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

        // internal override void UpdateWindowFrame()
        // {
        //     base.UpdateWindowFrame();

        //     POINT ptSrc = new POINT { x = 0, y = 0 };
        //     POINT ptDst = new POINT { x = (int)WindowPosition.x, y = (int)WindowPosition.y };
        //     SIZE size = new SIZE { cx = (int)WindowSize.x, cy = (int)WindowSize.y };

        //     BLENDFUNCTION blend = new BLENDFUNCTION
        //     {
        //         BlendOp = (int)AlphaBlendOptions.AC_SRC_OVER,
        //         SourceConstantAlpha = 255,
        //         AlphaFormat = (int)AlphaBlendOptions.AC_SRC_ALPHA
        //     };

        //     IntPtr hdcScreen = GetDC(IntPtr.Zero);
        //     UpdateLayeredWindow(
        //         hWnd,
        //         hdcScreen,
        //         ref ptDst,
        //         ref size,
        //         RenderContext._hdcMemory,
        //         ref ptSrc,
        //         0,
        //         ref blend,
        //         (int)LayeredWindowFlags.ULW_ALPHA
        //     );

        //     ReleaseDC(IntPtr.Zero, hdcScreen);
        //     DwmFlush();
        // }
    }
}