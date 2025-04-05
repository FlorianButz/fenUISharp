using System.Runtime.InteropServices;
using Microsoft.Win32;
using SkiaSharp;

namespace FenUISharp
{

    public class TransparentWindow : Window
    {
        public TransparentWindow(
            string title, string className, RenderContextType type, Vector2? windowSize, Vector2? windowPosition) :
            base(title, className, type, windowSize, windowPosition, true, true)
        {
            if (type == RenderContextType.DirectX) throw new ArgumentException("DirectX is currently not supported with transparent windows.");

            AllowResizing = false;
        }

        public override void UpdateWindowFrame()
        {
            base.UpdateWindowFrame();

            POINT ptSrc = new POINT { x = 0, y = 0 };
            POINT ptDst = new POINT { x = (int)WindowPosition.x, y = (int)WindowPosition.y };
            SIZE size = new SIZE { cx = (int)WindowSize.x, cy = (int)WindowSize.y };

            BLENDFUNCTION blend = new BLENDFUNCTION
            {
                BlendOp = (int)AlphaBlendOptions.AC_SRC_OVER,
                SourceConstantAlpha = 255,
                AlphaFormat = (int)AlphaBlendOptions.AC_SRC_ALPHA
            };

            IntPtr hdcScreen = GetDC(IntPtr.Zero);
            UpdateLayeredWindow(
                hWnd,
                hdcScreen,
                ref ptDst,
                ref size,
                RenderContext._hdcMemory,
                ref ptSrc,
                0,
                ref blend,
                (int)LayeredWindowFlags.ULW_ALPHA
            );

            ReleaseDC(IntPtr.Zero, hdcScreen);
            DwmFlush();
        }

        protected override IntPtr CreateWin32Window(WNDCLASSEX wndClass, Vector2? size, Vector2? position)
        {
            Console.WriteLine(WindowClass);
            WindowSize = size ?? WindowSize;
            WindowPosition = position ?? WindowPosition;

            Vector2? centeredPos = null;
            if (position == null)
            {
                var r = GetMonitorRect(0).Value;
                centeredPos = new Vector2((r.right - r.left) / 2 - (WindowSize.x / 2), (r.bottom - r.top) / 2 - (WindowSize.y / 2));
            }

            var hWnd = CreateWindowExA(
                (int)WindowStyles.WS_EX_LAYERED,
                this.WindowClass,
                this.WindowTitle,
                (int)WindowStyles.WS_POPUP,
                (int)(centeredPos ?? WindowPosition).x,
                (int)(centeredPos ?? WindowPosition).y,
                (int)WindowSize.x,
                (int)WindowSize.y,
                IntPtr.Zero,
                IntPtr.Zero,
                wndClass.hInstance,
                IntPtr.Zero);

            WindowPosition = centeredPos ?? WindowPosition;

            return hWnd;
        }
    }
}