using System.ComponentModel;
using FenUISharp.Mathematics;
using SkiaSharp;

namespace FenUISharp.Components
{
    public class FBlurPane : FPanel
    {
        private SKPaint dropShadowPaint;
        private SKPaint blurPaint;

        private float _blurAmount;
        public float BlurAmount { get => _blurAmount; set => _blurAmount = value; }

        public FBlurPane(Window root, Vector2 position, Vector2 size, float cornerRadius, float blurAmount)
            : base(root, position, size, cornerRadius, null)
        {
            _blurAmount = blurAmount;
            Transform.BoundsPadding.SetValue(this, 60, 35);
            this._drawBasePanel = false;

            using (var blur = SKImageFilter.CreateBlur(1f, 1f))
            {
                {
                    blurPaint = SkPaint.Clone();

                    float[] alphaMatrix = new float[]
                    {
                        1, 0, 0, 0, 0,  // Red
                        0, 1, 0, 0, 0,  // Green
                        0, 0, 1, 0, 0,  // Blue
                        0, 0, 0, 100, 0  // Alpha
                    };

                    using (var alphaOne = SKColorFilter.CreateColorMatrix(alphaMatrix))
                        blurPaint.ImageFilter = SKImageFilter.CreateCompose(SKImageFilter.CreateColorFilter(alphaOne), blur);
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
            base.DrawToSurface(canvas);

            var bounds = Transform.FullBounds;
            var pad = 60;
            var rect = new SKRoundRect(Transform.LocalBounds, CornerRadius);
            var captureArea = new SKRect(bounds.Left - pad, bounds.Top - pad, bounds.Right + pad, bounds.Bottom + pad);

            float scaleFactor = RMath.Clamp(1 / _blurAmount, 0.05f, 1f);

            using (var capture = WindowRoot.RenderContext.CaptureWindowRegion(captureArea, scaleFactor))
            {
                // Save for clipping
                int c = canvas.Save();

                if (UseSquircle)
                    canvas.ClipPath(SKSquircle.CreateSquircle(Transform.LocalBounds, CornerRadius), antialias: true);
                else
                    canvas.ClipRoundRect(rect, antialias: true);

                canvas.Translate(-pad + Transform.BoundsPadding.Value * 2, -pad + Transform.BoundsPadding.Value * 2);
                canvas.Scale(1 / scaleFactor);
                canvas.DrawImage(capture, 0, 0, new SKSamplingOptions(SKFilterMode.Linear), blurPaint);
                canvas.Scale(scaleFactor);
                canvas.Translate(pad - Transform.BoundsPadding.Value * 2, pad - Transform.BoundsPadding.Value * 2);

                canvas.RestoreToCount(c);
            }
            rect.Dispose();
        }
    }
}