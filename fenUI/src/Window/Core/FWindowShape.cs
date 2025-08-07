using FenUISharp.Mathematics;
using FenUISharp.Native;

namespace FenUISharp
{
    public class FWindowShape
    {
        private WeakReference<FWindow> window { get; set; }
        public FWindow Window { get => window.TryGetTarget(out var target) ? target : throw new Exception("Window not set."); }

        public Vector2 Position
        {
            get
            {
                Win32APIs.GetWindowRect(Window.hWnd, out RECT rect);
                return new Vector2(rect.left, rect.top);
            }
            protected set
            {
                Win32APIs.MoveWindow(Window.hWnd, (int)value.x, (int)value.y, (int)Size.x, (int)Size.y, true);
            }
        }

        public Vector2 Size
        {
            get
            {
                Win32APIs.GetWindowRect(Window.hWnd, out RECT rect);
                return new Vector2(rect.Width, rect.Height);
            }
            protected set
            {
                Win32APIs.MoveWindow(Window.hWnd, (int)Position.x, (int)Position.y, (int)value.x, (int)value.y, true);
            }
        }

        public Vector2 ClientSize
        {
            get
            {
                Win32APIs.GetClientRect(Window.hWnd, out RECT rect);
                return new Vector2(rect.Width, rect.Height);
            }
        }

        // Setting minimum and maximum sizes for the window
        public Vector2 MinSize { get; set; } = new Vector2(150, 150);
        public Vector2 MaxSize { get; set; } = new Vector2(float.MaxValue, float.MaxValue);

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
    }
}