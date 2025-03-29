using System.Runtime.InteropServices;
using Microsoft.Win32;
using SkiaSharp;

namespace FenUISharp
{

    public class OverlayWindow : Window
    {
        private int _activeDisplay = 0;
        public int ActiveDisplayIndex { get => _activeDisplay; set { _activeDisplay = value; UpdateWindowMetrics(_activeDisplay); } }

        public OverlayWindow(
            string title, string className, RenderContextType type, int monitorIndex = 0) :
            base(title, className, type, new Vector2(0, 0), null, true, true)
        {
            if(type == RenderContextType.DirectX) throw new ArgumentException("DirectX is currently not supported with transparent overlays.");

            AllowResizing = false;
            UpdateWindowMetrics(monitorIndex);
        }

        public void UpdateWindowMetrics(int activeMonitorDisplay = 0)
        {
            int x, y, width, height;

            if (activeMonitorDisplay == 0)
            {
                // Use primary monitor metrics from system metrics
                width = GetSystemMetrics(0);  // SM_CXSCREEN
                height = GetSystemMetrics(1); // SM_CYSCREEN
                x = 0;
                y = 0;
            }
            else
            {
                var monitorRect = GetMonitorRect(activeMonitorDisplay);
                if (monitorRect == null)
                    throw new ArgumentException("Invalid monitor index");

                x = monitorRect.Value.left;
                y = monitorRect.Value.top;
                width = monitorRect.Value.right - monitorRect.Value.left;
                height = monitorRect.Value.bottom - monitorRect.Value.top;
            }

            WindowSize = new Vector2(width, height);
            WindowPosition = new Vector2(x, y);
            SetWindowPos(hWnd, IntPtr.Zero, x, y, width, height, (uint)SetWindowPosFlags.SWP_NOZORDER | (uint)SetWindowPosFlags.SWP_NOACTIVATE);
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
            WindowSize = new Vector2(GetSystemMetrics(0), GetSystemMetrics(1));

            // Create a borderless popup window with the layered style.
            var hWnd = CreateWindowExA(
                (int)WindowStyles.WS_EX_LAYERED,
                WindowClass,
                WindowTitle,
                (int)WindowStyles.WS_POPUP,
                0, 0,
                (int)WindowSize.x, (int)WindowSize.y,
                IntPtr.Zero, IntPtr.Zero, wndClass.hInstance, IntPtr.Zero);

            return hWnd;
        }
    }
}