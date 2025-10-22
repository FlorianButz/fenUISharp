using System.Runtime.InteropServices;
using FenUISharp.Logging;
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
                return new Vector2(rect.Width, rect.Height) / WindowDPIScale;
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

        /// <summary>
        /// Returns the internal Windows identification name of the current monitor
        /// </summary>
        public string CurrentMonitorName { get => CurrentMonitorNameString(); }

        /// <summary>
        /// Get the handle of the current monitor
        /// </summary>
        public nint CurrentMonitorHandle => Win32APIs.MonitorFromWindow(Window.hWnd, 0x00000002 /* MONITOR_DEFAULTTONEAREST */);

        private Dictionary<object, Func<SKRect>> WindowRegion = new();
        private bool _windowRegionsDirty;

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

        private string CurrentMonitorNameString()
        {
            // Finding the monitor from the point
            IntPtr hMonitor = Win32APIs.MonitorFromWindow(Window.hWnd, 0x00000002 /* MONITOR_DEFAULTTONEAREST */);

            MONITORINFOEX info = new MONITORINFOEX();
            info.cbSize = Marshal.SizeOf(info);
            Win32APIs.GetMonitorInfo(hMonitor, ref info);
            string deviceName = info.szDevice;

            // Returning monitor name
            return deviceName;
        }

        private float GetDpiScale()
        {
            // Get DPI from handle
            var dpi = Win32APIs.GetDpiForWindow(Window.hWnd);
            return dpi / 96.0f; // 96 DPI is 100% scaling
        }

        /// <summary>
        /// Specifies a new cropped window region. Whole window is default. Values should be in client coordinate space
        /// </summary>
        /// <param name="owner">The caller of this method</param>
        /// <param name="area">The area the window should be cropped to. This is only invoked after setting or when calling RebuildWindowArea(). It is not updated continously!</param>
        public void AddOrUpdateWindowRegion(object owner, Func<SKRect> area)
        {
            if (WindowRegion.ContainsKey(owner))
            {
                var a = area.Invoke();

                SKRect oldRect = WindowRegion[owner].Invoke();
                if (DifferentSize(oldRect.Left, a.Left) ||
                    DifferentSize(oldRect.Right, a.Right) ||
                    DifferentSize(oldRect.Top, a.Top) ||
                    DifferentSize(oldRect.Bottom, a.Bottom))
                    _windowRegionsDirty = true;

                WindowRegion[owner] = area;
            }
            else
            {
                WindowRegion.Add(owner, area);
                _windowRegionsDirty = true;
            }
        }

        bool DifferentSize(float a, float b) => Math.Abs(a - b) > 0.5f;

        /// <summary>
        /// Removes the added region which was bound to the owner object.
        /// </summary>
        /// <param name="owner">The key</param>
        public void DissolveWindowRegion(object owner)
        {
            if (WindowRegion.ContainsKey(owner))
            {
                WindowRegion.Remove(owner);
                _windowRegionsDirty = true;
            }
        }

        internal List<Func<SKRect>> GetWinRegion() => WindowRegion.Values.ToList();

        private void UpdateWindowRegions()
        {
            if (WindowRegion.Count == 0)
            {
                Win32APIs.SetWindowRgn(Window.hWnd, IntPtr.Zero, true);
                return;
            }

            FLogger.Log<FWindowShape>("Rebuild window regions");

            // Define final region pointer
            IntPtr finalRegion = IntPtr.Zero;
            foreach (var obj in WindowRegion.ToList()) // Go through all applied regions
            {
                // Invoke rect function
                var rect = obj.Value.Invoke();

                // Make DPI aware
                rect = SKMatrix.CreateScale(Window.Shape.WindowDPIScale, Window.Shape.WindowDPIScale).MapRect(rect);

                IntPtr rgn = Win32APIs.CreateRectRgn((int)(rect.Left), (int)(rect.Top),
                    (int)(rect.Left + rect.Width),
                    (int)(rect.Top + rect.Height));

                if (finalRegion == IntPtr.Zero)
                    finalRegion = rgn; // Take the first one
                else
                {
                    Win32APIs.CombineRgn(finalRegion, finalRegion, rgn, CombineModes.RGN_OR);
                    Win32APIs.DeleteObject(rgn);
                }
            }

            if (finalRegion != IntPtr.Zero)
                // DONT DeleteObject(finalRegion); OS owns it now.
                Win32APIs.SetWindowRgn(Window.hWnd, finalRegion, true);
            else
                Win32APIs.SetWindowRgn(Window.hWnd, IntPtr.Zero, true);

            _windowRegionsDirty = false;
        }

        public void RebuildWindowArea()
            => _windowRegionsDirty = true;

        internal void UpdateShape()
        {
            if (_windowRegionsDirty)
                UpdateWindowRegions();
        }

        public void Dispose()
        {

        }
    }
}