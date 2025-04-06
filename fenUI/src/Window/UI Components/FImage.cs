using FenUISharp.Themes;
using SkiaSharp;

namespace FenUISharp
{
    public class FImage : FPanel
    {
        public SKImage Image { get; set; }
        public SKBlendMode TintBlendMode { get; set; } = SKBlendMode.Multiply;

        public ThemeColor TintColor { get; set; }

        public enum ImageScaleMode { Stretch, Fit, Contain }
        public ImageScaleMode ScaleMode { get; set; } = ImageScaleMode.Fit;

        public FImage(Window root, Vector2 position, Vector2 size, SKImage image, float cornerRadius, bool drawBackground = false) : base(root, position, size, cornerRadius, new ThemeColor(new SKColor(255, 255, 255, 255)))
        {
            Image = image;
            _drawBasePanel = drawBackground;

            TintColor = new ThemeColor(SKColors.White);
        }

        protected override void DrawToSurface(SKCanvas canvas)
        {
            base.DrawToSurface(canvas);

            using (var roundRect = new SKRoundRect(Transform.LocalBounds, CornerRadius))
            {
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

                canvas.DrawImage(Image, bounds ?? Transform.LocalBounds, SkPaint);

                SkPaint.ImageFilter = null;
                SkPaint.Color = TintColor.Value;
                SkPaint.BlendMode = TintBlendMode;
                roundRect.Deflate(0.5f, 0.5f);
                canvas.DrawRoundRect(roundRect, SkPaint);
            }
        }
    }
}