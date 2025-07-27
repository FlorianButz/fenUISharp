using FenUISharp.Mathematics;
using FenUISharp.States;
using FenUISharp.Themes;
using SkiaSharp;

namespace FenUISharp.Objects
{
    public class FImage : FPanel, IStateListener
    {
        public State<SKImage> Image { get; private init; }
        public State<SKBlendMode> TintBlendMode { get; private init; }

        public State<SKColor> TintColor { get; private init; }

        public enum ImageScaleMode { Stretch, Fit, Contain }
        public State<ImageScaleMode> ScaleMode { get; private init; }

        public FImage(Func<SKImage> image, bool drawBackground = false, bool dynamicColor = false, Func<Vector2>? position = null, Func<Vector2>? size = null) : base(position, size)
        {
            _drawBasePanel = drawBackground;

            Image = new(image, this, this);
            TintBlendMode = new(() => SKBlendMode.Modulate, this, this);
            ScaleMode = new(() => ImageScaleMode.Fit, this, this);

            CornerRadius.SetResponsiveState(() => Layout.ClampSize(Transform.Size.CachedValue).y / 1.5f);

            TintColor = new(() => dynamicColor ? FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.OnSurface : SKColors.White, this, this);
            Padding.Value = () => 10;
        }

        public override void Render(SKCanvas canvas)
        {
            base.Render(canvas);

            if (Image.CachedValue == null) return;

            using var paint = GetRenderPaint();

            var rect = Shape.LocalBounds;
            using var panelPath = GetPanelPath(rect);
            canvas.ClipPath(panelPath, antialias: true);

            SKRect? bounds = null;
            switch (ScaleMode.CachedValue)
            {
                case ImageScaleMode.Stretch:
                    bounds = Shape.LocalBounds;
                    break;
                case ImageScaleMode.Contain:
                    {
                        float scale = Math.Min(Shape.LocalBounds.Width / (float)Image.CachedValue.Width, Shape.LocalBounds.Height / (float)Image.CachedValue.Height);
                        float imageWidth = Image.CachedValue.Width * scale;
                        float imageHeight = Image.CachedValue.Height * scale;

                        float offsetX = Shape.LocalBounds.Left + (Shape.LocalBounds.Width - imageWidth) / 2;
                        float offsetY = Shape.LocalBounds.Top + (Shape.LocalBounds.Height - imageHeight) / 2;

                        bounds = SKRect.Create(
                            offsetX,
                            offsetY,
                            imageWidth,
                            imageHeight);
                        break;
                    }
                case ImageScaleMode.Fit:
                    {
                        float scaleFit = Math.Max(Shape.LocalBounds.Width / Image.CachedValue.Width, Shape.LocalBounds.Height / Image.CachedValue.Height);

                        float fitWidth = Image.CachedValue.Width * scaleFit;
                        float fitHeight = Image.CachedValue.Height * scaleFit;

                        float fitOffsetX = Shape.LocalBounds.Left + (Shape.LocalBounds.Width - fitWidth) / 2;
                        float fitOffsetY = Shape.LocalBounds.Top + (Shape.LocalBounds.Height - fitHeight) / 2;

                        bounds = new SKRect(fitOffsetX, fitOffsetY, fitOffsetX + fitWidth, fitOffsetY + fitHeight);
                        break;
                    }
            }

            using (var cFilter = SKColorFilter.CreateBlendMode(TintColor.CachedValue, TintBlendMode.CachedValue))
                paint.ColorFilter = cFilter;

            canvas.DrawImage(Image.CachedValue, bounds ?? Shape.LocalBounds, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear), paint);
        }
    }
}