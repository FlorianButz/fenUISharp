using FenUISharp.Mathematics;
using FenUISharp.Objects;
using SkiaSharp;

namespace FenUISharp.Materials
{
    public class DefaultPanelMaterial : Material
    {
        public Func<SKColor> BaseColor
        {
            get => GetProp<Func<SKColor>>("BaseColor", () => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.Surface);
            set => SetProp("BaseColor", value);
        }

        public Func<SKColor> BorderColor
        {
            get => GetProp<Func<SKColor>>("BorderColor", () => BaseColor().AddMix(new SKColor(50, 50, 50, 0)));
            set => SetProp("BorderColor", value);
        }

        public Func<SKColor> DarkBorderColor
        {
            get => GetProp<Func<SKColor>>("DarkBorderColor", () => BorderColor().Multiply(0.1f));
            set => SetProp("DarkBorderColor", value);
        }

        public Func<SKColor> ShadowColor
        {
            get => GetProp<Func<SKColor>>("ShadowColor", () => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.Shadow);
            set => SetProp("ShadowColor", value);
        }

        public Func<float> DropShadowRadius
        {
            get => GetProp<Func<float>>("DropShadowRadius", () => 6);
            set => SetProp("DropShadowRadius", value);
        }

        public Func<float> BorderSize
        {
            get => GetProp<Func<float>>("BorderSize", () => 2);
            set => SetProp("BorderSize", value);
        }

        protected override void Draw(SKCanvas targetCanvas, SKPath path, UIObject caller, SKPaint paint)
        {
            if (BorderSize() % 2 == 1)
                targetCanvas.Translate(0.5f, 0.5f);

            paint.Color = BaseColor();

            using (var dropShadow = SKImageFilter.CreateDropShadow(0, 2, DropShadowRadius(), DropShadowRadius(), ShadowColor()))
                paint.ImageFilter = dropShadow;

            using (var strokePaint = paint.Clone())
            {
                strokePaint.IsStroke = true;

                strokePaint.StrokeCap = SKStrokeCap.Round;
                strokePaint.StrokeJoin = SKStrokeJoin.Round;

                strokePaint.ImageFilter = null; // Remove shadow

                strokePaint.Color = DarkBorderColor();
                strokePaint.StrokeWidth = BorderSize() + 2f * (BorderSize() / 2);
                targetCanvas.DrawPath(path, strokePaint);

                strokePaint.Color = BorderColor();
                strokePaint.StrokeWidth = BorderSize();
                targetCanvas.DrawPath(path, strokePaint);
            }

            targetCanvas.DrawPath(path, paint);
                
            if(BorderSize() % 2 == 1)
                targetCanvas.Translate(-0.5f, -0.5f);
        }
    }
}