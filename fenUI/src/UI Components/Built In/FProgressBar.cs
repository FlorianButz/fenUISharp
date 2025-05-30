using FenUISharp.Mathematics;
using FenUISharp.Themes;
using SkiaSharp;

namespace FenUISharp.Components
{
    public class FProgressBar : UIComponent
    {
        protected float _value = 0f;
        public float Value { get { return _value; } set { var lastValue = _value; _value = RMath.Remap(value, _minValue, _maxValue, 0, 1); if(lastValue != _value) { OnValueChanged?.Invoke(value); Invalidate(); } } }

        protected float _maxValue = 1f;
        public float MaxValue { get { return _maxValue; } set { _maxValue = value; Invalidate(); } }
        protected float _minValue = 0f;
        public float MinValue { get { return _minValue; } set { _minValue = value; Invalidate(); } }

        public bool Indeterminate { get; set; } = false;
        public bool LeftToRight { get; set; } = true;

        public Action<float>? OnValueChanged { get; set; }

        public ThemeColor BackgroundColor { get; set; }
        public ThemeColor FillColor { get; set; }
        public ThemeColor BorderColor { get; set; }

        public float IndeterminateSpeed { get; set; } = 0.6f;
        public float IndeterminateLinesRepeat { get; set; } = 1.5f;
        protected float time = 0;

        public FProgressBar(Window rootWindow, Vector2 position, float width, float height = 5, ThemeColor? backgroundColor = null, ThemeColor? fillColor = null, ThemeColor? borderColor = null) : base(rootWindow, position, new(width, height))
        {
            BackgroundColor = backgroundColor ?? rootWindow.WindowThemeManager.GetColor(t => t.Background);
            FillColor = fillColor ?? rootWindow.WindowThemeManager.GetColor(t => t.Primary);
            BorderColor = borderColor ?? rootWindow.WindowThemeManager.GetColor(t => t.Surface);

            Transform.BoundsPadding.SetValue(this, 5, 25);
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

            using (var paint = SkPaint.Clone())
            using (var dropShadow = SKImageFilter.CreateDropShadow(0, 2, 5, 5, WindowRoot.WindowThemeManager.GetColor(t => t.Shadow).Value))
            using (var path = SKSquircle.CreateSquircle(bounds, 10))
            {
                paint.Color = BackgroundColor.Value;
                paint.ImageFilter = dropShadow;
                canvas.DrawPath(path, paint);

                paint.ImageFilter = null;
                paint.Color = BorderColor.Value;
                paint.IsStroke = true;
                paint.StrokeWidth = 1;

                canvas.Translate(0.5f, 0.5f);
                canvas.DrawPath(path, paint);
                canvas.Translate(-0.5f, -0.5f);
            }

            var rect = bounds;

            if (Indeterminate)
            {
                using (var paint = SkPaint.Clone())
                using (var path = SKSquircle.CreateSquircle(rect, 10))
                {
                    paint.Color = FillColor.Value;
                    canvas.DrawPath(path, paint);

                    const string sksl = @"
                        uniform float2      iResolution;
                        uniform float       iTime;
                        uniform float       iRepeat;

                        half4 main(float2 fragCoord) {
                            float2 uv = fragCoord / iResolution;
                            uv.x += iTime * 0.2;          // slide over time
                            uv.x *= iRepeat;

                            float saw = fract(uv.x * 10.0);
                            float mask = step(0.5, saw);  // 0 or 1

                            // unpremultiplied colors, full-alpha
                            half4 white = half4(1,1,1,0.25);
                            half4 blue  = half4(0,0,0,0);

                            // pick white or blue
                            half4 colUP = mix(white, blue, mask);

                            colUP.rgb *= colUP.a;
                            return colUP;
                        }
                    ";

                    var effect = SKRuntimeEffect.CreateShader(sksl, out var errorText);
                    if (effect == null) throw new Exception(errorText);

                    var uniforms = new SKRuntimeEffectUniforms(effect);
                    uniforms["iResolution"] = new SKPoint(rect.Width, rect.Height);
                    uniforms["iTime"] = (float)time;
                    uniforms["iRepeat"] = (float)IndeterminateLinesRepeat;

                    using var shader = effect.ToShader(uniforms);

                    var skewMat = SKMatrix.CreateSkew(-1, 0);
                    var offset = SKMatrix.CreateTranslation(6, 0);
                    var matrix = SKMatrix.Concat(skewMat, offset);

                    using var matrixF = SKImageFilter.CreateMatrix(matrix);

                    paint.ImageFilter = matrixF;
                    paint.Shader = shader;
                    paint.BlendMode = SKBlendMode.Screen;

                    canvas.ClipPath(path, antialias: true);
                    canvas.DrawPath(path, paint);
                }
            }
            else
            {
                if (LeftToRight)
                    rect = new SKRect(bounds.Left, bounds.Top, bounds.Left + Transform.Size.x * RMath.Remap(_value, 0f, 1f, 0.01f, 1f), bounds.Top + Transform.Size.y);
                else
                    rect = new SKRect(bounds.Right + Transform.Size.x * -RMath.Remap(_value, 0f, 1f, 0.01f, 1f), bounds.Top, bounds.Right, bounds.Top + Transform.Size.y);

                using (var paint = SkPaint.Clone())
                using (var path = SKSquircle.CreateSquircle(rect, 10))
                {
                    paint.Color = FillColor.Value;
                    canvas.DrawPath(path, paint);
                }
            }

            using (var paint = SkPaint.Clone())
            using (var path = SKSquircle.CreateSquircle(rect, 10))
            {
                SKPoint start = LeftToRight ? new(bounds.Left, bounds.MidY) : new(bounds.Right, bounds.MidY);
                SKPoint end = LeftToRight ? new(bounds.Right, bounds.MidY) : new(bounds.Left, bounds.MidY);

                paint.Color = FillColor.Value;
                paint.Shader = SKShader.CreateLinearGradient(
                    start,
                    end,
                    new SKColor[] { SKColors.Black.WithAlpha(65), SKColors.White.WithAlpha(85) },
                    new float[] { 0f, 1f },
                    SKShaderTileMode.Clamp
                );
                paint.BlendMode = SKBlendMode.Screen;

                canvas.DrawPath(path, paint);
            }
        }
    }
}