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

            using (var windowArea = WindowRoot.RenderContext.CaptureWindowRegion(Transform.Bounds, HighQualityBlur ? 0.15f : 0.02f))
            using (var panelPath = GetPanelPath())
            {
                if (windowArea == null) return;

                using (var clearPaint = SkPaint.Clone())
                {
                    clearPaint.BlendMode = SKBlendMode.Src;
                    clearPaint.Color = new(1, 0, 0, 1);
                    WindowRoot.RenderContext.Surface?.Canvas?.DrawPath(panelPath, clearPaint);
                }

                var paint = SkPaint.Clone();
                paint.Color = SKColors.White;

                using(var blur = SKImageFilter.CreateBlur(HighQualityBlur ? 15 : 5, HighQualityBlur ? 15 : 5))
                    paint.ImageFilter = blur;

                canvas.ClipPath(panelPath, antialias: true);
                canvas.DrawImage(windowArea, Transform.LocalBounds, sampling: new(SKFilterMode.Linear, SKMipmapMode.Linear), paint);

                paint.Dispose();
            }
        }
    }
}