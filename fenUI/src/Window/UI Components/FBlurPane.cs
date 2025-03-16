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

        float q = 0.5f;

        public FBlurPane(Vector2 position, Vector2 size, float cornerRadius, Vector2 blurAmount, bool useDropShadow = false, float brightness = 0.4f, float contrast = 0.77f)
            : base(position, size, cornerRadius, SKColors.Black)
        {
            _blurAmount = blurAmount;
            // renderQuality.SetValue(this, 0.1f, 25);
            transform.boundsPadding.SetValue(this, 60, 35);

            _brightContrast = new Vector2(brightness, contrast);

            _useDropShadow = useDropShadow;

            using (var blur = SKImageFilter.CreateBlur(FMath.Clamp(_blurAmount.x, 0.1f, 50), FMath.Clamp(_blurAmount.y, 0.1f, 50)))
            {
                float contrastFactor = FMath.Clamp(_brightContrast.y, 0, 1); // Less than 1 to reduce contrast
                float translate = (1f - contrastFactor) * 0.5f;

                float[] contrastMatrix = new float[]
                {
                    contrastFactor, 0, 0, 0, translate,  // Red
                    0, contrastFactor, 0, 0, translate,  // Green
                    0, 0, contrastFactor, 0, translate,  // Blue
                    0, 0, 0, 1, 0  // Alpha (unchanged)
                };

                using (var colorFilter = SKImageFilter.CreateColorFilter(SKColorFilter.CreateCompose(SKColorFilter.CreateLighting(
                    SKColors.White, new SKColor((byte)(25 * FMath.Clamp((int)_brightContrast.x, 0, 1)), (byte)(25 * FMath.Clamp((int)_brightContrast.x, 0, 1)), (byte)(25 * FMath.Clamp((int)_brightContrast.x, 0, 1)))), SKColorFilter.CreateColorMatrix(contrastMatrix))))
                {
                    blurPaint = skPaint.Clone();
                    blurPaint.ImageFilter = SKImageFilter.CreateCompose(blur, colorFilter);
                }
            }
            using (var drop = SKImageFilter.CreateDropShadowOnly(2, 2, 15, 15, SKColors.Black.WithAlpha(165)))
            {
                dropShadowPaint = skPaint.Clone();
                dropShadowPaint.ImageFilter = drop;
            }
        }

        protected override void OnComponentDestroy()
        {
            base.OnComponentDestroy();

            blurPaint.Dispose();
            dropShadowPaint.Dispose();
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            _isGloballyInvalidated = false;
            if (FWindow.IsNextFrameRendering())
            {
                Invalidate();
            }
        }

        protected override void DrawToSurface(SKCanvas canvas)
        {
            var bounds = transform.fullBounds;
            var pad = 60; // Required for larger blur scales. Should not be a number that isn't dividable through 2, otherwise visual issues will start appear because of integer rounding at lower blur scales

            var rect = new SKRoundRect(transform.localBounds, _cornerRadius);
            var captureArea = new SKRect(bounds.Left - pad, bounds.Top - pad, bounds.Right + pad, bounds.Bottom + pad);

            using (var capture = FWindow.CaptureRegion(captureArea))
            {
                canvas.Save();
                canvas.ClipRoundRect(rect, antialias: true);
                canvas.Translate(-pad, -pad);

                canvas.DrawImage(capture, 0, 0, blurPaint);
                
                canvas.Translate(pad, pad);
                canvas.Restore();

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