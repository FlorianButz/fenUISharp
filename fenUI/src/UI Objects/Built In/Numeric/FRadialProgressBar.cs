using FenUISharp.Mathematics;
using SkiaSharp;

namespace FenUISharp.Objects
{
    public class FRadialProgressBar : FProgressBar
    {
        public float Thickness { get; set; } = 5;
        public float IndeterminateArc { get; set; } = 100;

        /// <summary>
        /// Will give a progress bar with the given value function
        /// </summary>
        /// <param name="value"></param>
        /// <param name="position"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public FRadialProgressBar(Func<float> value, Func<Vector2>? position = null, float radius = 100) : base(value, position, radius, radius)
        {
        }

        /// <summary>
        /// Will give an indeterminate radial progress bar
        /// </summary>
        /// <param name="position"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public FRadialProgressBar(Func<Vector2>? position = null, float radius = 100) : base(position, radius, radius)
        {

        }

        public override void Render(SKCanvas canvas)
        {
            var bounds = Shape.LocalBounds;
            float radius = Shape.LocalBounds.Height / 2;
            float value = RMath.Remap(_01value, 0f, 1f, 0.01f, 1f);

            float sweep = value * 360;
            if (!LeftToRight)
                sweep = 1 - sweep;

            using (var paint = GetRenderPaint())
            using (var dropShadow = SKImageFilter.CreateDropShadow(0, 2, 5, 5, FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.Shadow))
            {
                paint.IsStroke = true;
                paint.StrokeCap = SKStrokeCap.Round;
                paint.ImageFilter = dropShadow;

                paint.StrokeWidth = Thickness + 1;
                paint.Color = BorderColor.CachedValue;

                canvas.DrawArc(bounds,
                   startAngle: -90,
                   sweepAngle: 360,
                   useCenter: false,
                   paint);

                paint.ImageFilter = null;
                paint.StrokeWidth = Thickness;
                paint.Color = BackgroundColor.CachedValue;

                canvas.DrawArc(bounds,
                   startAngle: -90,
                   sweepAngle: 360,
                   useCenter: false,
                   paint);

                paint.Color = FillColor.CachedValue;

                if (Indeterminate)
                {
                    var t = time * 360;
                    var sweepAng = IndeterminateArc;

                    canvas.DrawArc(bounds,
                       startAngle: t,
                       sweepAngle: sweepAng,
                       useCenter: false,
                       paint);
                }
                else
                {
                    canvas.DrawArc(bounds,
                       startAngle: -90,
                       sweepAngle: sweep,
                       useCenter: false,
                       paint);
                }
            }

            using (var paint = GetRenderPaint())
            {
                paint.IsStroke = true;
                paint.StrokeWidth = Thickness;
                paint.StrokeCap = SKStrokeCap.Round;
                paint.Style = SKPaintStyle.Stroke;

                var colors = new SKColor[] { SKColors.Black.WithAlpha(65), SKColors.White.WithAlpha(85), SKColors.Black.WithAlpha(65) };
                var pos = new float[] { 0f, 0.5f, 1f };
                var mid = new SKPoint(bounds.MidX, bounds.MidY);

                paint.Shader = SKShader.CreateSweepGradient(
                    mid,
                    colors,
                    pos,
                    SKShaderTileMode.Clamp,
                    startAngle: 0,
                    endAngle: 360
                );
                paint.BlendMode = SKBlendMode.Screen;

                if (Indeterminate)
                {
                    var t = time * 360 + 90;
                    var sweepAng = IndeterminateArc;

                    canvas.RotateDegrees(-90, bounds.MidX, bounds.MidY);
                    canvas.DrawArc(bounds,
                       startAngle: t,
                       sweepAngle: sweepAng,
                       useCenter: false,
                       paint);
                }
                else
                {

                    canvas.RotateDegrees(-90, bounds.MidX, bounds.MidY);
                    canvas.DrawArc(bounds,
                       startAngle: 0,
                       sweepAngle: sweep,
                       useCenter: false,
                       paint);
                }
            }
        }
    }
}