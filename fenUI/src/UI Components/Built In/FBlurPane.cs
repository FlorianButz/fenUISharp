using FenUISharp.Mathematics;
using FenUISharp.Themes;
using SkiaSharp;

namespace FenUISharp.Components
{
    public class FBlurPane : FPanel
    {
        public bool HighQualityBlur { get; set; } = true;

        public FBlurPane(Window root, Vector2 position, Vector2 size, float cornerRadius = 10, ThemeColor? backgroundColor = null) : base(root, position, size, cornerRadius, backgroundColor ?? new(SKColors.Transparent))
        {
            this.BorderColor = root.WindowThemeManager.GetColor(t => t.OnSurface.WithAlpha(50));
            this.BorderSize = 1;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (!WindowRoot.IsNextFrameRendering()) return;
            Invalidate();
        }

        protected override void DrawToSurface(SKCanvas canvas)
        {
            base.DrawToSurface(canvas);

            int padding = 25;
            var captureArea = Transform.Bounds;
            captureArea.Inflate(padding, padding);

            using (var windowArea = WindowRoot.RenderContext.CaptureWindowRegion(captureArea, HighQualityBlur ? 0.15f : 0.02f))
            using (var panelPath = GetPanelPath())
            {
                if (windowArea == null) return;

                using (var clearPaint = new SKPaint { BlendMode = SKBlendMode.Clear, IsAntialias = true })
                {
                    WindowRoot.RenderContext.Surface?.Canvas?.DrawPath(this.GetPanelPath(Transform.Bounds), clearPaint);
                }

                var paint = SkPaint.Clone();

                SkPaint.Color = PanelColor.Value;
                canvas.DrawPath(panelPath, SkPaint);

                paint.Color = SKColors.White;

                using (var blur = SKImageFilter.CreateBlur(HighQualityBlur ? 15 : 5, HighQualityBlur ? 15 : 5))
                    paint.ImageFilter = blur;

                var displayArea = Transform.LocalBounds;
                displayArea.Inflate(padding, padding);

                canvas.ClipPath(panelPath, antialias: true);
                canvas.DrawImage(windowArea, displayArea, sampling: new(SKFilterMode.Linear, SKMipmapMode.Linear), paint);

                canvas.DrawPath(panelPath, SkPaint);

                paint.Dispose();
            }
        }
    }
}