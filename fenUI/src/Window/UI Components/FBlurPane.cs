using System.ComponentModel;
using SkiaSharp;

namespace FenUISharp
{
    public class FBlurPane : FPanel
    {
        private SKPaint dropShadowPaint;
        private SKPaint blurPaint;

        private Vector2 _blurAmount;
        public Vector2 BlurAmount { get => _blurAmount; set => _blurAmount = value; }

        private Vector2 _brightContrast;
        public float Brightness { get => _brightContrast.x; set => _brightContrast.x = value; }
        public float Contrast { get => _brightContrast.y; set => _brightContrast.y = value; }

        private bool _useDropShadow;

        public FBlurPane(Window root, Vector2 position, Vector2 size, float cornerRadius, Vector2 blurAmount, bool useDropShadow = false, float brightness = 0.4f, float contrast = 0.77f)
            : base(root, position, size, cornerRadius, null)
        {
            _blurAmount = blurAmount;
            Transform.BoundsPadding.SetValue(this, 60, 35);

            var useBrightContrast = !(brightness == contrast && brightness == 1);

            _brightContrast = new Vector2(brightness, contrast);

            _useDropShadow = useDropShadow;

            using (var blur = SKImageFilter.CreateBlur(RMath.Clamp(_blurAmount.x, 0.1f, 50), RMath.Clamp(_blurAmount.y, 0.1f, 50)))
            {
                float contrastFactor = RMath.Clamp(_brightContrast.y, 0, 1); // Less than 1 to reduce contrast
                float translate = (1f - contrastFactor) * 0.5f;

                float[] contrastMatrix = new float[]
                {
                    contrastFactor, 0, 0, 0, translate,  // Red
                    0, contrastFactor, 0, 0, translate,  // Green
                    0, 0, contrastFactor, 0, translate,  // Blue
                    0, 0, 0, 1, 0  // Alpha (unchanged)
                };

                if (useBrightContrast)
                {
                    using (var colorFilter = SKImageFilter.CreateColorFilter(
                    SKColorFilter.CreateCompose(SKColorFilter.CreateLighting(
                    SKColors.White, new SKColor(
                        (byte)(25 * RMath.Clamp((int)_brightContrast.x, 0, 1)), (byte)(25 * RMath.Clamp((int)_brightContrast.x, 0, 1)), (byte)(25 * RMath.Clamp((int)_brightContrast.x, 0, 1)))),
                        SKColorFilter.CreateColorMatrix(contrastMatrix))))
                    {
                        blurPaint = SkPaint.Clone();
                        blurPaint.ImageFilter = SKImageFilter.CreateCompose(blur, colorFilter);
                    }
                }
                else
                {
                    blurPaint = SkPaint.Clone();
                    blurPaint.ImageFilter = blur;
                }
            }
            using (var drop = SKImageFilter.CreateDropShadowOnly(2, 2, 15, 15, SKColors.Black.WithAlpha(165)))
            {
                dropShadowPaint = SkPaint.Clone();
                dropShadowPaint.ImageFilter = drop;
            }
        }

        protected override void ComponentDestroy()
        {
            base.ComponentDestroy();

            blurPaint.Dispose();
            dropShadowPaint.Dispose();
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            GloballyInvalidated = false;
            if (WindowRoot.IsNextFrameRendering())
            {
                Invalidate();
            }
        }

        protected override void DrawToSurface(SKCanvas canvas)
        {
            var bounds = Transform.FullBounds;
            var pad = 60;
            var rect = new SKRoundRect(Transform.LocalBounds, CornerRadius);
            var captureArea = new SKRect(bounds.Left - pad, bounds.Top - pad, bounds.Right + pad, bounds.Bottom + pad);

            float scaleFactor = 0.5f;

            using (var capture = WindowRoot.RenderContext.CaptureWindowRegion(captureArea, scaleFactor))
            {
                // Save for clipping
                int c = canvas.Save();
                canvas.ClipRoundRect(rect, antialias: true);

                canvas.Translate(-pad, -pad);
                canvas.Scale(1 / scaleFactor);
                canvas.DrawImage(capture, 0, 0, blurPaint);
                canvas.Scale(scaleFactor);
                canvas.Translate(pad, pad);

                canvas.RestoreToCount(c);

                if (_useDropShadow)
                {
                    canvas.Save();
                    canvas.ClipRoundRect(rect, SKClipOperation.Difference, true);
                    canvas.DrawRoundRect(rect, dropShadowPaint);
                    canvas.Restore();
                }
            }
            rect.Dispose();
        }
    }
}