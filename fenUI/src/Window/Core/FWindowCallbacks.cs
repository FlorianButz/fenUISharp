using FenUISharp.Mathematics;
using FenUISharp.Native;

namespace FenUISharp
{
    public class FWindowCallbacks
    {
        private WeakReference<FWindow> window { get; set; }
        public FWindow Window { get => window.TryGetTarget(out var target) ? target : throw new Exception("Window not set."); }

        public Action<FDropData>? OnDragEnter { get; set; }
        public Action<FDropData>? OnDragOver { get; set; }
        public Action<FDropData>? OnDragDrop { get; set; }
        public Action? OnDragLeave { get; set; }

        public FWindowCallbacks(FWindow window)
        {
            this.window = new WeakReference<FWindow>(window);
        }
    }
}