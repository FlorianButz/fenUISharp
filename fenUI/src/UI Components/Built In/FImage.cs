using FenUISharp.Mathematics;
using FenUISharp.Themes;
using SkiaSharp;

namespace FenUISharp.Components
{
    public class FImage : FPanel
    {
        public SKImage Image { get; set; }
        public SKBlendMode TintBlendMode { get; set; } = SKBlendMode.Modulate;

        public ThemeColor TintColor { get; set; }

        public enum ImageScaleMode { Stretch, Fit, Contain }
        public ImageScaleMode ScaleMode { get; set; } = ImageScaleMode.Fit;

        public FImage(Window root, Vector2 position, Vector2 size, SKImage image, float cornerRadius, bool drawBackground = false) : base(root, position, size, cornerRadius, new ThemeColor(new SKColor(255, 255, 255, 255)))
        {
            Image = image;
            _drawBasePanel = drawBackground;

            TintColor = root.WindowThemeManager.GetColor(t => t.OnSurface);
            Transform.BoundsPadding.SetValue(this, 10, 15);
        }

        protected override void DrawToSurface(SKCanvas canvas)
        {
            base.DrawToSurface(canvas);

            if (Image == null) return;

            var rect = Transform.LocalBounds;
            rect.Inflate(0.5f, 0.5f);

            using (var roundRect = new SKRoundRect(Transform.LocalBounds, CornerRadius))
            {

                using (var dropShadow = SKImageFilter.CreateDropShadowOnly(0, 0, DropShadowRadius, DropShadowRadius, ShadowColor.Value))
                    SkPaint.ImageFilter = dropShadow;

                if (UseSquircle)
                    canvas.DrawPath(SKSquircle.CreateSquircle(rect, CornerRadius), SkPaint);
                else
                    canvas.DrawRoundRect(roundRect, SkPaint);

                SkPaint.ImageFilter = null;

                if (UseSquircle)
                    canvas.ClipPath(SKSquircle.CreateSquircle(rect, CornerRadius), antialias: true);
                else
                    canvas.ClipRoundRect(roundRect, antialias: true);

                SKRect? bounds = null;
                switch (ScaleMode)
                {
                    case ImageScaleMode.Stretch:
                        bounds = Transform.LocalBounds;
                        break;
                    case ImageScaleMode.Contain:
                        {
                            float scale = Math.Min(Transform.LocalBounds.Width / (float)Image.Width, Transform.LocalBounds.Height / (float)Image.Height);
                            float imageWidth = Image.Width * scale;
                            float imageHeight = Image.Height * scale;

                            float offsetX = Transform.LocalBounds.Left + (Transform.LocalBounds.Width - imageWidth) / 2;
                            float offsetY = Transform.LocalBounds.Top + (Transform.LocalBounds.Height - imageHeight) / 2;

                            bounds = SKRect.Create(
                                offsetX,
                                offsetY,
                                imageWidth,
                                imageHeight);
                            break;
                        }
                    case ImageScaleMode.Fit:
                        {
                            float scaleFit = Math.Max(Transform.LocalBounds.Width / Image.Width, Transform.LocalBounds.Height / Image.Height);

                            float fitWidth = Image.Width * scaleFit;
                            float fitHeight = Image.Height * scaleFit;

                            float fitOffsetX = Transform.LocalBounds.Left + (Transform.LocalBounds.Width - fitWidth) / 2;
                            float fitOffsetY = Transform.LocalBounds.Top + (Transform.LocalBounds.Height - fitHeight) / 2;

                            bounds = new SKRect(fitOffsetX, fitOffsetY, fitOffsetX + fitWidth, fitOffsetY + fitHeight);
                            break;
                        }
                }

                using (var cFilter = SKColorFilter.CreateBlendMode(TintColor.Value, TintBlendMode))
                    SkPaint.ColorFilter = cFilter;

                canvas.DrawImage(Image, bounds ?? Transform.LocalBounds, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear), SkPaint);

                // roundRect.Deflate(0.5f, 0.5f);
                // rect.Inflate(-1f, -1f);

                // if (UseSquircle)
                //     canvas.DrawPath(SKSquircle.CreateSquircle(rect, CornerRadius), SkPaint);
                // else
                //     canvas.DrawRoundRect(roundRect, SkPaint);
            }
        }
    }
}