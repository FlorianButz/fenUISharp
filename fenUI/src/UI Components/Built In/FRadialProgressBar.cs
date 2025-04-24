using FenUISharp.Mathematics;
using FenUISharp.Themes;
using SkiaSharp;

namespace FenUISharp.Components
{
    public class FRadialProgressBar : UIComponent
    {
        protected float _value = 0f;
        public float Value { get { return _value; } set { _value = RMath.Remap(value, _minValue, _maxValue, 0, 1); OnValueChanged?.Invoke(value); Invalidate(); } }

        protected float _maxValue = 1f;
        public float MaxValue { get { return _maxValue; } set { _maxValue = value; Invalidate(); } }
        protected float _minValue = 0f;
        public float MinValue { get { return _minValue; } set { _minValue = value; Invalidate(); } }

        public bool Indeterminate { get; set; } = false;
        public bool Clockwise { get; set; } = true;

        public float Thickness { get; set; } = 5;

        public Action<float>? OnValueChanged { get; set; }

        public ThemeColor BackgroundColor { get; set; }
        public ThemeColor FillColor { get; set; }
        public ThemeColor BorderColor { get; set; }

        public float IndeterminateSpeed { get; set; } = 0.6f;
        public float IndeterminateArc { get; set; } = 100;
        protected float time = 0;

        public FRadialProgressBar(Window rootWindow, Vector2 position, Vector2 size, float thickness = 5, ThemeColor? backgroundColor = null, ThemeColor? fillColor = null, ThemeColor? borderColor = null) : base(rootWindow, position, size)
        {
            BackgroundColor = backgroundColor ?? rootWindow.WindowThemeManager.GetColor(t => t.Background);
            FillColor = fillColor ?? rootWindow.WindowThemeManager.GetColor(t => t.Primary);
            BorderColor = borderColor ?? rootWindow.WindowThemeManager.GetColor(t => t.Surface);

            Thickness = thickness;
            Transform.BoundsPadding.SetValue(this, (int)(thickness * 2), 25);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (Indeterminate && WindowRoot.IsWindowFocused && WindowRoot.IsWindowShown)
            {
                time -= (float)(IndeterminateSpeed * WindowRoot.DeltaTime);
                time = time % 1;
                Invalidate();
            }
        }

        protected override void DrawToSurface(SKCanvas canvas)
        {
            var bounds = Transform.LocalBounds;
            float radius = Transform.LocalBounds.Height / 2;
            float value = RMath.Remap(_value, 0f, 1f, 0.01f, 1f);

            float sweep = value * 360;
            if (!Clockwise)
                sweep = 1 - sweep;

            using (var paint = SkPaint.Clone())
            using (var dropShadow = SKImageFilter.CreateDropShadow(0, 2, 5, 5, WindowRoot.WindowThemeManager.GetColor(t => t.Shadow).Value))
            {
                paint.IsStroke = true;
                paint.StrokeCap = SKStrokeCap.Round;
                paint.ImageFilter = dropShadow;

                paint.StrokeWidth = Thickness + 1;
                paint.Color = BorderColor.Value;

                canvas.DrawArc(bounds,
                   startAngle: -90,
                   sweepAngle: 360,
                   useCenter: false,
                   paint);

                paint.ImageFilter = null;
                paint.StrokeWidth = Thickness;
                paint.Color = BackgroundColor.Value;

                canvas.DrawArc(bounds,
                   startAngle: -90,
                   sweepAngle: 360,
                   useCenter: false,
                   paint);

                paint.Color = FillColor.Value;

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

            using (var paint = SkPaint.Clone())
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