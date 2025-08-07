using FenUISharp.Mathematics;
using FenUISharp.Native;

namespace FenUISharp
{
    public class FWindowProcedure
    {
        private WeakReference<FWindow> window { get; set; }
        public FWindow Window { get => window.TryGetTarget(out var target) ? target : throw new Exception("Window not set."); }

        public Func<bool>? _isRunning { get; set; }

        public IntPtr WindowsProcedure(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            return Win32APIs.DefWindowProcW(hWnd, msg, wParam, lParam); // Default window procedure
        }

        public FWindowProcedure(FWindow window)
        {
            this.window = new WeakReference<FWindow>(window);
        }
    }
}