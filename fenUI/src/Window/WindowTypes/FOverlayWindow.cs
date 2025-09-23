using System.Runtime.InteropServices;
using FenUISharp.Mathematics;
using FenUISharp.Native;
using Microsoft.Win32;
using SkiaSharp;

namespace FenUISharp
{
    public class FOverlayWindow : FTransparentWindow
    {
        private int _activeDisplay = 0;
        public int ActiveDisplayIndex { get => _activeDisplay; set { _activeDisplay = value; UpdateWindowMetrics(_activeDisplay); } }

        public FOverlayWindow(
            string title, string className, int monitorIndex = 0) :
            base(title, className, new(0, 0), new(0, 0))
        {
            UpdateWindowMetrics(monitorIndex);

            // Exclude window from aero peek. Not needed with transparent overlays
            Properties.ExcludeFromAeroPeek = true;
            
            // Not needed in taskbar
            Properties.VisibleInTaskbar = false;
        }

        public void UpdateWindowMetrics(int activeMonitorDisplay = 0)
        {
            int x, y, width, height;

            // if (activeMonitorDisplay == 0)
            // {
            //     // Use primary monitor metrics from system metrics
            //     width = Win32APIs.GetSystemMetrics(0);  // SM_CXSCREEN
            //     height = Win32APIs.GetSystemMetrics(1); // SM_CYSCREEN
            //     x = 0;
            //     y = 0;
            // }
            // else
            {
                // Otherwise get rect of monitor from the index
                var monitorRect = Win32APIs.GetMonitorRect(activeMonitorDisplay);

                // Extract values
                x = monitorRect.left;
                y = monitorRect.top;
                width = monitorRect.right - monitorRect.left;
                height = monitorRect.bottom - monitorRect.top;
            }

            // Set window position and size
            Win32APIs.SetWindowPos(hWnd, IntPtr.Zero, x, y, width, height, (uint)SetWindowPosFlags.SWP_NOZORDER | (uint)SetWindowPosFlags.SWP_NOACTIVATE);

            // Trigger buffer invalidation
            FullRedraw();

            // Trigger wndarea rebuild
            Shape.RebuildWindowArea();
        }
    }
}