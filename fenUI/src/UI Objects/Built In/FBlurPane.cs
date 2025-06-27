using FenUISharp.Mathematics;
using SkiaSharp;

namespace FenUISharp.Objects
{
    public class FBlurPane : FPanel
    {
        public bool HighQualityBlur { get; set; } = true;

        public FBlurPane(Func<Vector2>? position = null, Func<Vector2>? size = null) : base(position, size)
        {
            this.BorderColor.Value = () => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.OnSurface.WithAlpha(50);
        }

        protected override void Update()
        {
            base.Update();

            if (!FContext.GetCurrentWindow().IsNextFrameRendering()) return;
            Invalidate(Invalidation.SurfaceDirty);
        }

        public override void Render(SKCanvas canvas)
        {
            base.Render(canvas);

            var captureArea = Shape.SurfaceDrawRect;
            using var windowArea = this.Composition.GrabBehindPlusBuffer(Transform.DrawLocalToGlobal(captureArea), HighQualityBlur ? 0.3f : 0.02f);

            using (var panelPath = GetPanelPath())
            {
                if (windowArea == null) return;
                canvas.ClipPath(panelPath, antialias: true);

                using var paint = GetRenderPaint();

                paint.Color = PanelColor.CachedValue;
                canvas.DrawPath(panelPath, paint);

                using (var blur = SKImageFilter.CreateBlur(HighQualityBlur ? 15 : 5, HighQualityBlur ? 15 : 5))
                    paint.ImageFilter = blur;

                var displayArea = Shape.SurfaceDrawRect;

                canvas.DrawImage(windowArea, displayArea, sampling: new(SKFilterMode.Linear, SKMipmapMode.Linear), paint);
                paint.Dispose();
            }
        }
    }
}