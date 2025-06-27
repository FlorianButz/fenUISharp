using FenUISharp.Mathematics;
using FenUISharp.States;
using FenUISharp.Themes;
using SkiaSharp;

namespace FenUISharp.Objects
{
    public class FProgressBar : UIObject, IStateListener
    {
        protected float _01value = 0f;
        public State<float> Value { get; init; }

        public State<float> MaxValue { get; init; }
        public State<float> MinValue {get;init;}

        public bool Indeterminate { get; set; } = false;
        public bool LeftToRight { get; set; } = true;

        public Action<float>? OnValueChanged { get; set; }

        public State<SKColor> BackgroundColor { get; set; }
        public State<SKColor> FillColor { get; set; }
        public State<SKColor> BorderColor { get; set; }

        public float IndeterminateSpeed { get; set; } = 0.6f;
        public float IndeterminateLinesRepeat { get; set; } = 1.5f;
        protected float time = 0;

        /// <summary>
        /// Will give a progress bar with the given value function
        /// </summary>
        /// <param name="value"></param>
        /// <param name="position"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public FProgressBar(Func<float> value, Func<Vector2>? position = null, float width = 100, float height = 5) : base(position, () => new(width, height))
        {
            BackgroundColor = new(() => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.Background, this);
            FillColor = new(() => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.Primary, this);
            BorderColor = new(() => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.Surface, this);

            Value = new(value, this);

            MinValue = new(() => 0, this);
            MaxValue = new(() => 1, this);

            Padding.SetStaticState(5);
        }

        /// <summary>
        /// Will give an indeterminate progress bar
        /// </summary>
        /// <param name="position"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public FProgressBar(Func<Vector2>? position = null, float width = 100, float height = 5) : base(position, () => new(width, height))
        {
            BackgroundColor = new(() => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.Background, this);
            FillColor = new(() => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.Primary, this);
            BorderColor = new(() => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.Surface, this);

            Value = new(() => 0, this);
            Indeterminate = true;

            MinValue = new(() => 0, this);
            MaxValue = new(() => 1, this);

            Padding.SetStaticState(5);
        }

        public override void Dispose()
        {
            base.Dispose();

            BackgroundColor.Dispose();
            FillColor.Dispose();
            BorderColor.Dispose();

            MinValue.Dispose();
            MaxValue.Dispose();
        }

        protected override void Update()
        {
            base.Update();

            if (Indeterminate && FContext.GetCurrentWindow().IsWindowFocused && FContext.GetCurrentWindow().IsWindowShown)
            {
                time -= (float)(IndeterminateSpeed * FContext.DeltaTime);
                time = time % 1;
                Invalidate(Invalidation.SurfaceDirty);
            }
        }

        public override void OnInternalStateChanged<T>(T value)
        {
            base.OnInternalStateChanged(value);

            var lastValue = _01value;
            _01value = RMath.Remap(Value.CachedValue, MinValue.CachedValue, MaxValue.CachedValue, 0, 1);

            if (lastValue != _01value)
            {
                OnValueChanged?.Invoke(Value.CachedValue);
            }

            Invalidate(Invalidation.SurfaceDirty);
        }

        public override void Render(SKCanvas canvas)
        {
            var bounds = Shape.LocalBounds;

            using (var paint = GetRenderPaint())
            using (var dropShadow = SKImageFilter.CreateDropShadow(0, 2, 2, 2, FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.Shadow))
            using (var path = SKSquircle.CreateSquircle(bounds, 10))
            {
                paint.Color = BackgroundColor.CachedValue;
                paint.ImageFilter = dropShadow;
                canvas.DrawPath(path, paint);

                paint.ImageFilter = null;
                paint.Color = BorderColor.CachedValue;
                paint.IsStroke = true;
                paint.StrokeWidth = 1;

                canvas.Translate(0.5f, 0.5f);
                canvas.DrawPath(path, paint);
                canvas.Translate(-0.5f, -0.5f);
            }

            var rect = bounds;

            if (Indeterminate)
            {
                using (var paint = GetRenderPaint())
                using (var path = SKSquircle.CreateSquircle(rect, 10))
                {
                    paint.Color = FillColor.CachedValue;
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
                    rect = new SKRect(bounds.Left, bounds.Top, bounds.Left + Transform.Size.CachedValue.x * RMath.Remap(_01value, 0f, 1f, 0.01f, 1f), bounds.Top + Transform.Size.CachedValue.y);
                else
                    rect = new SKRect(bounds.Right + Transform.Size.CachedValue.x * -RMath.Remap(_01value, 0f, 1f, 0.01f, 1f), bounds.Top, bounds.Right, bounds.Top + Transform.Size.CachedValue.y);

                using (var paint = GetRenderPaint())
                using (var path = SKSquircle.CreateSquircle(rect, 10))
                {
                    paint.Color = FillColor.CachedValue;
                    canvas.DrawPath(path, paint);
                }
            }

            using (var paint = GetRenderPaint())
            using (var path = SKSquircle.CreateSquircle(rect, 10))
            {
                SKPoint start = LeftToRight ? new(bounds.Left, bounds.MidY) : new(bounds.Right, bounds.MidY);
                SKPoint end = LeftToRight ? new(bounds.Right, bounds.MidY) : new(bounds.Left, bounds.MidY);

                paint.Color = FillColor.CachedValue;
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