using System.Runtime.InteropServices;
using FenUISharp.Themes;
using SkiaSharp;

namespace FenUISharp
{
    internal class ContextMenu : TransparentWindow
    {
        static ContextMenu? _instance;
        float _renderSize = 0f;

        public ContextMenu(Vector2 size) : base("ContextMenu", "fenUICtxMenu", RenderContextType.Software, size, null)
        {
            SetMenuPosition();
            AllowResizing = false;
            ShowInTaskbar = false;

            this.OnFocusLost += () =>
            {
                this.DisposeAndDestroyWindow();
            };

            _isDirty = true; // Render initial frame, regardless of ui components
            _instance = this;

            var panel = new FPanel(this, new Vector2(0, 0), new Vector2(0, 0), 10, WindowThemeManager.GetColor(t => t.Background));
            panel.Transform.StretchHorizontal = true;
            panel.Transform.StretchVertical = true;
            panel.Transform.MarginHorizontal = 10;
            panel.Transform.MarginVertical = 10;

            panel.BorderSize = 1f;
            panel.BorderColor = new ThemeColor(new SKColor(150, 150, 150, 100));

            AnimatorComponent anim = new AnimatorComponent(panel, Easing.EaseOutBack);
            anim.duration = 0.3f;
            anim.onValueUpdate += (t) =>
            {
                _renderSize = t;
            };
            anim.Start();
        }

        int? _globalFadeSaveCount = null;

        protected override void OnRenderFrame(SKSurface surface)
        {
            base.OnRenderFrame(surface);

            if(_renderSize == 1f) return;
            surface.Canvas.Scale(RMath.Remap(_renderSize, 0, 1, 0.95f, 1));

            using(var blur = SKImageFilter.CreateBlur((1 - _renderSize) * 5, (1 - _renderSize) * 5))
            using(var paint = new SKPaint())
            {
                paint.ImageFilter = blur;
                _globalFadeSaveCount = surface.Canvas.SaveLayer(SKRect.Create(Bounds.Width, Bounds.Height), paint);
            }
        }

        protected override void OnAfterRenderFrame(SKSurface surface)
        {
            base.OnAfterRenderFrame(surface);
            
            if (_globalFadeSaveCount == null)
                return;
            var canvas = RenderContext.Surface.Canvas;

            using (var fadePaint = new SKPaint())
            {
                fadePaint.Color = SKColors.White.WithAlpha((byte)(Math.Clamp(255 * _renderSize, 0, 255)));
                fadePaint.BlendMode = SKBlendMode.DstIn;

                canvas.DrawRect(Bounds, fadePaint);
            }

            canvas.RestoreToCount(_globalFadeSaveCount.Value);
            _globalFadeSaveCount = null;
        }

        private void SetMenuPosition()
        {
            var globMousePos = WindowPosition + ClientMousePosition;
            var position = globMousePos;

            IntPtr monitor = MonitorFromPoint(new POINT() { x = (int)globMousePos.x, y = (int)globMousePos.y }, MONITOR_DEFAULTTONEAREST);

            MONITORINFO info = new MONITORINFO();
            info.cbSize = (uint)Marshal.SizeOf(typeof(MONITORINFO));
            GetMonitorInfo(monitor, ref info);

            RECT bounds = info.rcWork;

            if (position.x + WindowSize.x > bounds.right)
                position.x = globMousePos.x - WindowSize.x;

            if (position.y + WindowSize.y > bounds.bottom)
                position.y = globMousePos.y - WindowSize.y;

            WindowPosition = position;
        }

        public static void Create(Func<Window, List<UIComponent>> uiCompoents, Vector2? size = null)
        {
            if (_instance != null) _instance.DisposeAndDestroyWindow();
            _instance = null;

            Thread thread = new Thread(() =>
            {
                var ctx = new ContextMenu(size ?? new Vector2(140, 200));

                var panel = new FPanel(ctx, new Vector2(0, 0), new Vector2(0, 0), 0, new ThemeColor(SKColors.Transparent));
                panel.Transform.StretchVertical = true;
                panel.Transform.StretchHorizontal = true;
                panel.Transform.MarginHorizontal = 5;
                panel.Transform.MarginVertical = 5;

                var list = uiCompoents?.Invoke(ctx);
                list?.ForEach(x => x.Transform.SetParent(panel.Transform));
                var layout = new StackContentComponent(panel, StackContentComponent.ContentStackType.Vertical, StackContentComponent.ContentStackBehavior.Scroll);

                ctx.SetWindowVisibility(true);
                ctx.BeginWindowLoop();
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }
    }
}