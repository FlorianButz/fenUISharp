using FenUISharp.Mathematics;
using FenUISharp.Objects;
using SkiaSharp;

namespace FenUISharp.Materials
{
    public class InteractableDefaultMaterial : Material
    {
        public Func<SKColor> BaseColor
        {
            get => GetProp<Func<SKColor>>("BaseColor", () => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.Secondary);
            set => SetProp("BaseColor", value);
        }

        public Func<SKColor> ShadowColor
        {
            get => GetProp<Func<SKColor>>("ShadowColor", () => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.Shadow);
            set => SetProp("ShadowColor", value);
        }

        public Func<SKColor> BorderColor
        {
            get => GetProp<Func<SKColor>>("BorderColor", () => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.SecondaryBorder);
            set => SetProp("BorderColor", value);
        }

        public Func<SKColor> HightlightColor
        {
            get => GetProp<Func<SKColor>>("HightlightColor", () => BaseColor().AddMix(new(25, 25, 25)).MultiplyMix(new(255, 255, 255, 100)));
            set => SetProp("HightlightColor", value);
        }
        
        public Func<float> DropShadowRadius
        {
            get => GetProp<Func<float>>("DropShadowRadius", () => 2);
            set => SetProp("DropShadowRadius", value);
        }

        protected override void Draw(SKCanvas targetCanvas, SKPath path, UIObject caller, SKPaint paint)
        {
            var bounds = path.Bounds;
            targetCanvas.Translate(0.5f, 0.5f);

            // Draw base rectangle
            {
                paint.IsAntialias = true;
                paint.Style = SKPaintStyle.Fill;
                paint.Color = BaseColor();

                var shadowRadius = DropShadowRadius();
                using (var shadow = SKImageFilter.CreateDropShadow(0, 2, shadowRadius, shadowRadius, ShadowColor()))
                    paint.ImageFilter = shadow;

                targetCanvas.DrawPath(path, paint);
            }

            // Highlight on Top Edge
            {
                paint.IsAntialias = true;
                paint.Shader = SKShader.CreateLinearGradient(
                    new SKPoint(bounds.Left, bounds.Top),
                    new SKPoint(bounds.Left, bounds.Top + 4f),
                    new SKColor[] { HightlightColor(), SKColors.Transparent },
                    new float[] { 0.0f, 0.4f },
                    SKShaderTileMode.Clamp
                );

                targetCanvas.DrawPath(path, paint);
            }

            // Border
            {
                paint.IsStroke = true;
                paint.Color = BorderColor();
                paint.StrokeWidth = 1;

                targetCanvas.DrawPath(path, paint);
            }

            // Inner Shadow
            {
                paint.IsAntialias = true;
                paint.Shader = SKShader.CreateLinearGradient(
                    new SKPoint(bounds.Left, bounds.Bottom),
                    new SKPoint(bounds.Left, bounds.Top + 4f),
                    new SKColor[] { FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.Shadow, SKColors.Transparent },
                    new float[] { 0f, 1f },
                    SKShaderTileMode.Clamp
                );

                targetCanvas.DrawPath(path, paint);
            }
            
            targetCanvas.Translate(-0.5f, -0.5f);
        }
    }
}