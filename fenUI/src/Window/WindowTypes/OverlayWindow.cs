using System.Runtime.InteropServices;
using Microsoft.Win32;
using SkiaSharp;

namespace FenUISharp
{

    public class OverlayWindow : TransparentWindow
    {
        private int _activeDisplay = 0;
        public int ActiveDisplayIndex { get => _activeDisplay; set { _activeDisplay = value; UpdateWindowMetrics(_activeDisplay); } }

        public OverlayWindow(
            string title, string className, RenderContextType type, int monitorIndex = 0) :
            base(title, className, type, null, null)
        {
            if(type == RenderContextType.DirectX) throw new ArgumentException("DirectX is currently not supported with transparent windows.");

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

        protected override IntPtr CreateWin32Window(WNDCLASSEX wndClass, Vector2? size, Vector2? position)
        {
            WindowSize = new Vector2(GetSystemMetrics(0), GetSystemMetrics(1));

            return base.CreateWin32Window(wndClass, WindowSize, position);
        }
    }
}