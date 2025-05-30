using System.Runtime.InteropServices;
using FenUISharp.Components;
using FenUISharp.Mathematics;
using FenUISharp.Themes;
using FenUISharp.WinFeatures;
using SkiaSharp;

namespace FenUISharp
{
    public class ContextMenu : TransparentWindow
    {
        static ContextMenu? _instance;
        float _renderSize = 0f;
        const float _insideMargin = 10f;

        private Vector2 instantiatedPosition;
        private FPanel mainPanel;
        private StackContentComponent? layout;

        public ContextMenu(Vector2 size) : base("ContextMenu", "fenUICtxMenu", RenderContextType.Software, size + _insideMargin * 2, null)
        {
            AllowResizing = false;
            ShowInTaskbar = false;

            this.OnFocusLost += () =>
            {
                this.DisposeAndDestroyWindow();
            };

            _isDirty = true; // Render initial frame, regardless of ui components
            _instance = this;

            var panel = new FPanel(this, new Vector2(0, 0), new Vector2(0, 0), 7.5f, WindowThemeManager.GetColor(t => t.Background));
            mainPanel = panel;

            panel.ShadowColor = new ThemeColor(SKColors.Black.WithAlpha(100));
            panel.DropShadowRadius = 5;

            panel.Transform.StretchHorizontal = true;
            panel.Transform.StretchVertical = true;
            panel.Transform.MarginHorizontal = _insideMargin;
            panel.Transform.MarginVertical = _insideMargin;

            panel.BorderSize = 1f;
            panel.BorderColor = new ThemeColor(new SKColor(150, 150, 150, 100));

            AnimatorComponent anim = new AnimatorComponent(panel, Easing.EaseOutBack);
            anim.Duration = 0.3f;
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

            if (_renderSize == 1f) return;
            surface.Canvas.Scale(RMath.Remap(_renderSize, 0, 1, 0.95f, 1), RMath.Remap(_renderSize, 0, 1, 0.95f, 1), instantiatedPosition.x * WindowSize.x, instantiatedPosition.y * WindowSize.y);

            using (var blur = SKImageFilter.CreateBlur((1 - _renderSize) * 5, (1 - _renderSize) * 5))
            using (var paint = new SKPaint())
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

            if (RenderContext != null && RenderContext.Surface != null)
            {
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
        }

        private void SetMenuPosition()
        {
            var globMousePos = WindowPosition + ClientMousePosition;
            var position = globMousePos;

            var bounds = GetCurrentMonitorBounds();
            instantiatedPosition = new Vector2(0, 0);

            if (position.x + WindowSize.x > bounds.right)
            {
                instantiatedPosition.x = 1;
                position.x = globMousePos.x - WindowSize.x + _insideMargin;
            }
            else position.x -= _insideMargin;

            if (position.y + WindowSize.y > bounds.bottom)
            {
                instantiatedPosition.y = 1;
                position.y = globMousePos.y - WindowSize.y + _insideMargin;
            }
            else position.y -= _insideMargin;

            position.x -= 20;
            position.y -= 10;

            WindowPosition = position;
        }

        public void CalcContextSize()
        {
            Vector2 size = WindowSize * -1;
            size.y = 0;

            const float padding = 5f;

            if(layout != null) layout.Dispose();

            UiComponents.ForEach(x =>
            {
                if(x.Transform.Parent != mainPanel.Transform) return;
                if (!x.Transform.StretchHorizontal)
                    size.x = Math.Max(size.x, x.Transform.LocalBounds.Width);
                if (!x.Transform.StretchVertical)
                    size.y += x.Transform.LocalBounds.Height + padding;
            });

            var monitorBounds = GetCurrentMonitorBounds();

            var contentS = size;
            size = new Vector2(Math.Clamp(Math.Abs(size.x) + _insideMargin * 2, 0, monitorBounds.Height), Math.Clamp(Math.Abs(size.y) + _insideMargin * 2, 0, monitorBounds.Width));
            size.y = Math.Clamp(size.y, 0, monitorBounds.Height - (monitorBounds.bottom - GlobalHooks.MousePosition.y)) - 1 - padding;
            size += padding * 2;

            SetWindowSize(size);
            // Console.WriteLine(size);

            layout = new StackContentComponent(mainPanel, StackContentComponent.ContentStackType.Vertical, contentS.y > size.y ? StackContentComponent.ContentStackBehavior.Scroll : StackContentComponent.ContentStackBehavior.SizeToFit);
            layout.Pad = padding;
            layout.Gap = padding;

            SetMenuPosition();
        }

        protected RECT GetCurrentMonitorBounds()
        {
            var globMousePos = WindowPosition + ClientMousePosition;
            IntPtr monitor = MonitorFromPoint(new POINT() { x = (int)globMousePos.x, y = (int)globMousePos.y }, MONITOR_DEFAULTTONEAREST);

            MONITORINFO info = new MONITORINFO();
            info.cbSize = (uint)Marshal.SizeOf(typeof(MONITORINFO));
            GetMonitorInfo(monitor, ref info);

            RECT bounds = info.rcWork;
            return bounds;
        }

        public static void Create(Func<Window, List<UIComponent>> uiCompoents, Vector2? size = null)
        {
            if (_instance != null) _instance.DisposeAndDestroyWindow();
            _instance = null;

            Thread thread = new Thread(() =>
            {
                var ctx = new ContextMenu(size ?? new Vector2(120, 200));

                var list = uiCompoents?.Invoke(ctx);
                list?.ForEach(x => x.Transform.SetParent(ctx.mainPanel.Transform));

                ctx.CalcContextSize();

                ctx.SetWindowVisibility(true);
                ctx.BeginWindowLoop();
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }
    }
}