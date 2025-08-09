using FenUISharp.Mathematics;
using FenUISharp.Native;
using SkiaSharp;

namespace FenUISharp
{
    public class FWindowShape : IDisposable
    {
        private WeakReference<FWindow> window { get; set; }
        public FWindow Window { get => window.TryGetTarget(out var target) ? target : throw new Exception("Window not set."); }

        /// <summary>
        /// Gets the corrected DPI scale for this window
        /// </summary>
        public float WindowDPIScale { get => GetDpiScale(); }

        /// <summary>
        /// Get the global position of this window
        /// </summary>
        public Vector2 Position
        {
            get
            {
                Win32APIs.GetWindowRect(Window.hWnd, out RECT rect);
                return new Vector2(rect.left, rect.top);
            }
            set
            {
                Win32APIs.MoveWindow(Window.hWnd, (int)value.x, (int)value.y, (int)Size.x, (int)Size.y, true);
            }
        }

        /// <summary>
        /// Get the size of the full window
        /// </summary>
        public Vector2 Size
        {
            get
            {
                Win32APIs.GetWindowRect(Window.hWnd, out RECT rect);
                return new Vector2(rect.Width, rect.Height);
            }
            set
            {
                Win32APIs.MoveWindow(Window.hWnd, (int)Position.x, (int)Position.y, (int)value.x, (int)value.y, true);
            }
        }

        /// <summary>
        /// Get the size of the window's client area
        /// </summary>
        public Vector2 ClientSize
        {
            get
            {
                Win32APIs.GetClientRect(Window.hWnd, out RECT rect);
                return new Vector2(rect.Width, rect.Height);
            }
        }

        /// <summary>
        /// Creates simple Bounds for the window
        /// </summary>
        public SKRect Bounds { get => SKRect.Create(Position.x, Position.y, ClientSize.x, ClientSize.y); }

        /// <summary>
        /// Minimum size for this window
        /// </summary>
        public Vector2 MinSize { get; set; } = new Vector2(150, 150);

        /// <summary>
        /// Maximum size for this window
        /// </summary>
        public Vector2 MaxSize { get; set; } = new Vector2(float.MaxValue, float.MaxValue);

        /// <summary>
        /// Returns the index of the monitor which the window is on
        /// </summary>
        public int CurrentMonitorIndex { get => CurrentMonitor(); }

        public FWindowShape(FWindow window)
        {
            this.window = new WeakReference<FWindow>(window);
        }

        public Vector2 ClientPointToGlobal(Vector2 clientPoint)
        {
            POINT globalPoint = new POINT
            {
                x = (int)clientPoint.x,
                y = (int)clientPoint.y
            };

            Win32APIs.ClientToScreen(Window.hWnd, ref globalPoint);
            return new Vector2(globalPoint.x, globalPoint.y);
        }

        public Vector2 GlobalPointToClient(Vector2 globalPoint)
        {
            POINT clientPoint = new POINT
            {
                x = (int)globalPoint.x,
                y = (int)globalPoint.y
            };

            Win32APIs.ClientToScreen(Window.hWnd, ref clientPoint);
            return new Vector2(clientPoint.x, clientPoint.y);
        }

        public int GetMonitorIndexFromPoint(Vector2 p)
        {
            POINT globalPoint = new POINT
            {
                x = (int)p.x,
                y = (int)p.y
            };

            // Finding the monitor from the point
            IntPtr hMonitor = Win32APIs.MonitorFromPoint(globalPoint, 0x00000002 /* MONITOR_DEFAULTTONEAREST */);

            // Creating a list of monitors
            var monitors = new List<IntPtr>();

            // Creating a callback which will add the monitors to the list
            Win32APIs.MonitorEnumDelegate callback = (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData) =>
            {
                monitors.Add(hMonitor);
                return true;
            };

            // Running the enumeration
            Win32APIs.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero);
            
            // Finding the index of the monitor which the point is inside
            return monitors.IndexOf(hMonitor);
        }

        private int CurrentMonitor()
        {
            // Finding the monitor from the point
            IntPtr hMonitor = Win32APIs.MonitorFromWindow(Window.hWnd, 0x00000002 /* MONITOR_DEFAULTTONEAREST */);

            // Creating a list of monitors
            var monitors = new List<IntPtr>();

            // Creating a callback which will add the monitors to the list
            Win32APIs.MonitorEnumDelegate callback = (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData) =>
            {
                monitors.Add(hMonitor);
                return true;
            };

            // Running the enumeration
            Win32APIs.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero);

            // Finding the index of the monitor which the point is inside
            return monitors.IndexOf(hMonitor);
        }

        private float GetDpiScale()
        {
            // Get DPI from handle
            var dpi = Win32APIs.GetDpiForWindow(Window.hWnd);
            return dpi / 96.0f; // 96 DPI is 100% scaling
        }

        public void Dispose()
        {

        }
    }
}