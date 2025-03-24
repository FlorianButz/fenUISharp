using System.Runtime.InteropServices;
using Microsoft.Win32;
using SkiaSharp;

namespace FenUISharp
{
    public class NativeWindow : Window
    {
        private bool _hideTaskbarIcon;

        // Only needed when taskbar icon is hidden
        private static IntPtr hiddenOwnerWindow = IntPtr.Zero;

        public NativeWindow(
            string title, string className, RenderContextType type,
            Vector2? windowSize = null, Vector2? windowPosition = null,
            bool hideTaskbarIcon = false, bool alwaysOnTop = false) :
        base(title, className, type, windowSize, windowPosition, alwaysOnTop)
        {
            _hideTaskbarIcon = hideTaskbarIcon;
            SetTaskbarIconVisibility(_hideTaskbarIcon);
            SetTaskbarIconVisibility(false);
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

        protected override void OnRenderFrame()
        {
            base.OnRenderFrame();

            RenderContext.Surface.Canvas.Clear(GetTitlebarColor());
        }

        SKColor GetTitlebarColor()
        {
            if(!_sysDarkMode)
                return new SKColor(243, 243, 243); // Windows default light mode color
            else
                return new SKColor(32, 32, 32); // Windows default dark mode color
        }

        public void SetTaskbarIconVisibility(bool visible)
        {
            if (!visible)
            {
                // Create a dummy window (invisible) to act as the owner
                IntPtr hiddenOwner = CreateWindowEx((int)WindowStyles.WS_EX_TOOLWINDOW, "STATIC", "",
                    WS_OVERLAPPEDWINDOW, 0, 0, 0, 0, IntPtr.Zero, IntPtr.Zero, GetModuleHandle(null), IntPtr.Zero);

                ShowWindow(hiddenOwner, 0); // Ensure it never appears

                if (hWnd != IntPtr.Zero)
                {
                    SetWindowLongPtr(hWnd, GWL_HWNDPARENT, hiddenOwner);
                }
            }
            else
            {
                SetWindowLongPtr(hWnd, GWL_HWNDPARENT, IntPtr.Zero);
            }
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