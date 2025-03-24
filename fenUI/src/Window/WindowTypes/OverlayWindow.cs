using System.Runtime.InteropServices;
using Microsoft.Win32;
using SkiaSharp;

namespace FenUISharp
{
    public class OverlayWindow : Window
    {
        private bool _hideTaskbarIcon;

        // Only needed when taskbar icon is hidden
        private static IntPtr hiddenOwnerWindow = IntPtr.Zero;

        public OverlayWindow(
            string title, string className, RenderContextType type) :
        base(title, className, type, new Vector2(0, 0), null, true, true)
        {
            AllowResizing = false;

            SetMaximizedFullscreen();
        }

        void SetMaximizedFullscreen(){
            throw new NotImplementedException();
        }

        protected override IntPtr CreateWin32Window(WNDCLASSEX wndClass, Vector2? size, Vector2? position)
        {
            bool centerPos = position == null;

            var hWnd = CreateWindowExA(
                0,
                this.WindowClass,
                this.WindowTitle,
                WS_NATIVE,
                centerPos ? CW_USEDEFAULT : (int)position?.x,
                centerPos ? CW_USEDEFAULT : (int)position?.y,
                (int)size?.x,
                (int)size?.y,
                IntPtr.Zero,
                IntPtr.Zero,
                wndClass.hInstance,
                IntPtr.Zero);

            return hWnd;
        }

        // protected override void OnRenderFrame()
        // {
        //     base.OnRenderFrame();

        //     RenderContext.Surface.Canvas.Clear(GetTitlebarColor());
        // }

        SKColor GetTitlebarColor()
        {
            if(!_sysDarkMode)
                return new SKColor(243, 243, 243); // Windows default light mode color
            else
                return new SKColor(32, 32, 32); // Windows default dark mode color
        }

        public override void Dispose()
        {
            base.Dispose();

            if (hiddenOwnerWindow != IntPtr.Zero)
            {
                DestroyWindow(hiddenOwnerWindow);
                hiddenOwnerWindow = IntPtr.Zero;
            }
        }

        private const int GWL_HWNDPARENT = -8;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
    }
}