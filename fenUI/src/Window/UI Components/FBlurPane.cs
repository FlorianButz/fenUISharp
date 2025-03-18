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
        public bool DesktopBlur { get => _blurDesktop; set => UpdateDoesCaptureDesktop(value); }

        private bool _useDropShadow;
        private bool _blurDesktop = false;

        public FBlurPane(Vector2 position, Vector2 size, float cornerRadius, Vector2 blurAmount, bool useDropShadow = false, float brightness = 0.4f, float contrast = 0.77f)
            : base(position, size, cornerRadius, SKColors.Black)
        {
            _blurAmount = blurAmount;
            transform.boundsPadding.SetValue(this, 60, 35);

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

                using (var colorFilter = SKImageFilter.CreateColorFilter(
                    SKColorFilter.CreateCompose(SKColorFilter.CreateLighting(
                    SKColors.White, new SKColor(
                        (byte)(25 * RMath.Clamp((int)_brightContrast.x, 0, 1)), (byte)(25 * RMath.Clamp((int)_brightContrast.x, 0, 1)), (byte)(25 * RMath.Clamp((int)_brightContrast.x, 0, 1)))),
                        SKColorFilter.CreateColorMatrix(contrastMatrix))))
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

            UpdateDoesCaptureDesktop(_blurDesktop, true);
        }

        void UpdateDoesCaptureDesktop(bool value, bool forced = false)
        {
            if (_blurDesktop == value && !forced) return;

            if (value)
            {
                DesktopCapture.instance.Begin();
            }
            else
            {
                DesktopCapture.instance.Stop();
            }
        }

        protected override void OnComponentDestroy()
        {
            base.OnComponentDestroy();

            blurPaint.Dispose();
            dropShadowPaint.Dispose();

            if (_blurDesktop)
            {
                DesktopCapture.instance.Stop();
            }
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            _isGloballyInvalidated = false;
            if (Window.IsNextFrameRendering() || _blurDesktop)
            {
                Invalidate();
            }
        }

        protected override void DrawToSurface(SKCanvas canvas)
        {
            var bounds = transform.fullBounds;
            var pad = 60;
            var rect = new SKRoundRect(transform.localBounds, _cornerRadius);
            var captureArea = new SKRect(bounds.Left - pad, bounds.Top - pad, bounds.Right + pad, bounds.Bottom + pad);

            float scaleFactor = 0.5f;

            using (var capture = Window.CaptureRegion(captureArea, scaleFactor))
            {
                // Save for clipping
                int c = canvas.Save();
                canvas.ClipRoundRect(rect, antialias: true);

                if (_blurDesktop)
                {
                    // Draw the desktop capture at reduced resolution
                    if (DesktopCapture.instance.lastCapture != null && DesktopCapture.instance.previousCapture != null)
                    {
                        int c2 = canvas.Save();

                        const float shrink = 0.5f;

                        // Avoid ugly borders
                        rect.Inflate(-shrink, -shrink);
                        canvas.ClipRoundRect(rect, antialias: true);
                        rect.Inflate(shrink, shrink);

                        var opacityLatest = ((float)DesktopCapture.instance.timeSinceLastCapture / (float)DesktopCapture.instance.CaptureInterval);

                        canvas.Translate(-transform.position.x, -transform.position.y);
                        canvas.DrawImage(DesktopCapture.instance.previousCapture, Window.bounds, Window.samplingOptions);

                        using(var paint = skPaint.Clone()){
                            paint.Color = blurPaint.Color.WithAlpha((byte)(opacityLatest * 255));
                            canvas.DrawImage(DesktopCapture.instance.lastCapture, Window.bounds, Window.samplingOptions, paint);
                        }
                        canvas.Translate(transform.position.x, transform.position.y);

                        canvas.RestoreToCount(c2);
                    }
                }

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