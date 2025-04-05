using System.Drawing;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using SkiaSharp;

namespace FenUISharp
{
    public class NativeWindow : Window
    {
        private bool _useMica = false;
        public bool UseMica { get => _useMica; set { _useMica = value; UpdateMica(); } }
        private bool _useMicaMainWindow = true;
        public bool IsMicaMainWindow { get => _useMicaMainWindow; set { _useMicaMainWindow = value; UpdateMica(); } }

        public NativeWindow(
            string title, string className, RenderContextType type,
            Vector2? windowSize = null, Vector2? windowPosition = null,
            bool alwaysOnTop = false, bool hideTaskbarIcon = false, bool hasTitlebar = true) :
            base(title, className, type, windowSize, windowPosition, alwaysOnTop, hideTaskbarIcon, hasTitlebar)
        {
            MouseAction += OnMouseAction;
        }

        void OnMouseAction(MouseInputCode inputCode)
        {
            if (inputCode.state == (int)MouseInputState.Up &&
                inputCode.button == (int)MouseInputButton.Right)
            {
                ContextMenu.Create((ctx) =>
                {
                    var btn = new FSimpleButton(ctx, new Vector2(0, 25), "Test Text, click!", () => Console.WriteLine("test3"),
color: ctx.WindowThemeManager.GetColor(t => t.Primary), textColor: ctx.WindowThemeManager.GetColor(t => t.OnPrimary));
                    btn.Transform.Alignment = new Vector2(0.5f, 0f);

                    return new List<UIComponent>() { btn };
                });
            }
        }

        protected override IntPtr CreateWin32Window(WNDCLASSEX wndClass, Vector2? size, Vector2? position)
        {
            bool centerPos = position == null;

            if (centerPos)
            {
                var r = GetMonitorRect(0).Value;
                WindowPosition = new Vector2((r.right - r.left) / 2 - (WindowSize.x / 2), (r.bottom - r.top) / 2 - (WindowSize.y / 2));
            }

            var hWnd = CreateWindowExA(
                0,
                this.WindowClass,
                this.WindowTitle,
                _hasTitlebar ? WS_NATIVE : WS_NOTITLEBAR,
                centerPos ? (int)WindowPosition.x : (int)position?.x,
                centerPos ? (int)WindowPosition.y : (int)position?.y,
                (int)size?.x,
                (int)size?.y,
                IntPtr.Zero,
                IntPtr.Zero,
                wndClass.hInstance,
                IntPtr.Zero);

            UpdateMica();

            return hWnd;
        }

        protected override void OnRenderFrame(SKSurface surface)
        {
            base.OnRenderFrame(surface);

            surface.Canvas.Clear(_useMica ? SKColors.Transparent : GetTitlebarColor());
        }

        SKColor GetTitlebarColor()
        {
            if (!_sysDarkMode)
                return new SKColor(243, 243, 243); // Windows default light mode color
            else
                return new SKColor(32, 32, 32); // Windows default dark mode color
        }

        protected override void UpdateSysDarkmode()
        {
            base.UpdateSysDarkmode();

            UpdateMica();
        }

        public void UpdateMica()
        {
            // First, apply the Mica system backdrop
            int micaEffect = _useMica ? (_useMicaMainWindow ? (int)DWM_SYSTEMBACKDROP_TYPE.DWMSBT_MAINWINDOW : (int)DWM_SYSTEMBACKDROP_TYPE.DWMSBT_TRANSIENTWINDOW) : (int)DWM_SYSTEMBACKDROP_TYPE.DWMSBT_NONE;
            DwmSetWindowAttribute(
                hWnd,
                (uint)DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE,
                ref micaEffect,
                Marshal.SizeOf<int>()
            );

            // Extend the frame into the client area
            MARGINS margins = new MARGINS
            {
                cxLeftWidth = -1,
                cxRightWidth = -1,
                cyTopHeight = -1,
                cyBottomHeight = -1
            };

            DwmExtendFrameIntoClientArea(hWnd, ref margins);
        }
    }
}