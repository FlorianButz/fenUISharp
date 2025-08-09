using FenUISharp.Mathematics;
using FenUISharp.Native;
using SkiaSharp;

namespace FenUISharp
{
    public class FWindowCallbacks
    {
        private WeakReference<FWindow> window { get; set; }
        public FWindow Window { get => window.TryGetTarget(out var target) ? target : throw new Exception("Window not set."); }

        public Action<FDropData?>? OnDragEnter { get; set; } // When the drag operation enters the window
        public Action<FDropData?>? OnDragOver { get; set; } // When the drag operation is over the window
        public Action<FDropData?>? OnDragDrop { get; set; } // When the drag operation is dropped on the window
        public Action? OnDragLeave { get; set; } // When the drag operation leaves the window

        public Action<MouseInputCode>? ClientMouseAction { get; set; } // Mouse actions in the client area
        public Action<MouseInputCode>? TrayMouseAction { get; set; } // Mouse actions in the tray icon

        public Action? OnBeginRender { get; set; } // Before the render call
        public Action? OnEndRender { get; set; } // After the render call

        public Action<SKSurface>? OnWindowBeforeDraw { get; set; } // Before the window draw call
        public Action<SKSurface>? OnWindowAfterDraw { get; set; } // After the window draw call

        public Action? OnPostUpdate { get; set; } // After the logic update
        public Action? OnPreUpdate { get; set; } // Before the logic update

        public Action? OnDevicesChanged { get; set; } // Not sure what this does, it was needed once but not anymore

        public Action? OnFocusLost { get; set; } // When the window loses focus
        public Action? OnFocusGained { get; set; } // When the window gains focus

        public Action<Vector2>? OnWindowResize { get; set; } // When resizing
        public Action<Vector2>? OnWindowEndResize { get; set; } // Once the resizing is done
        public Action<Vector2>? OnWindowMove { get; set; } // When moving the window
        public Action<Vector2>? OnWindowEndMove { get; set; } // After moving the window

        public Action? OnWindowClose { get; set; } // When the window is closed
        public Action? OnWindowDestroy { get; set; } // When the window is destroyed

        internal Action<char>? OnKeyboardInputTextReceived { get; set; } // When a character is typed in the window

        public FWindowCallbacks(FWindow window)
        {
            this.window = new WeakReference<FWindow>(window);
        }
    }
}