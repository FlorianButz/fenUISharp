using FenUISharp.Logging;
using FenUISharp.Mathematics;
using SkiaSharp;

namespace FenUISharp.Objects
{
    internal class FAlphaSlider : FSlider
    {
        public FAlphaSlider()
        {
            KnobPositionSpring.SetValues(3f, 1.5f);
            InteractiveSurface.ExtendInteractionRadius.SetStaticState(-6);
            ClampKnob = true;
        }

        protected override void RenderBackground(SKCanvas canvas, SKRect rect)
        {
            // base.RenderBackground(canvas, rect);

            string sksl = @"
                uniform float2 iResolution;
                uniform float2 iOff;

                half4 lerp(half4 a, half4 b, half t) {
                    return a + (b - a) * t;
                }

                half4 main(float2 fragCoord) {
                    float2 uv = (fragCoord - iOff) / iResolution;

                    float2 coord = fragCoord;
                    
                    float checker = mod(floor(coord.x / 5) + floor(coord.y / 5), 2.0);
                    checker = clamp(checker, 0.6, 0.9);
                    half4 colAlpha = lerp(half4(checker, checker, checker, 1), half4(1, 1, 1, 1), uv.x);
                    
                    return colAlpha;
                }
            ";

            SKRuntimeEffect effect = SKRuntimeEffect.CreateShader(sksl, out var err);
            if (effect == null) FLogger.Error($"Shader compilation failed: {err}");

            var uniforms = new SKRuntimeEffectUniforms(effect);
            uniforms["iResolution"] = new float[] { rect.Width, rect.Height };
            uniforms["iOff"] = new float[] { rect.Left, rect.Top };

            using var paint = GetRenderPaint();
            using var barRoundRect = new SKRoundRect(rect, BarCornerRadius);

            using var shadow = SKImageFilter.CreateDropShadow(0, 0, 2, 2, FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.Shadow.AddMix(new(0, 0, 0, 25)));
            paint.ImageFilter = shadow;
            canvas.DrawRoundRect(barRoundRect, paint);
            paint.ImageFilter = null;

            paint.Shader = effect?.ToShader(uniforms);

            canvas.DrawRoundRect(barRoundRect, paint);

            paint.IsStroke = true;
            paint.StrokeWidth = 2f;
            paint.Color = SKColors.White.WithAlpha(35);
            paint.BlendMode = SKBlendMode.Plus;
            paint.Shader = null;

            canvas.DrawRoundRect(barRoundRect, paint);
        }

        // protected override void RenderKnob(SKCanvas canvas, SKRect knobRect)
        // {
        //     // base.RenderKnob(canvas, knobRect);

        //     using var paint = GetRenderPaint();
        //     paint.Color = SKColor.FromHsv(_value * 360, 100, 100);

        //     using (var shadow = SKImageFilter.CreateDropShadow(0, 1, 5, 5, KnobShadow.CachedValue))
        //         paint.ImageFilter = shadow;

        //     using var knobRoundRect = new SKRoundRect(knobRect, KnobCornerRadius);
        //     canvas.DrawRoundRect(knobRoundRect, paint);

        //     if (KnobBorderSize > 0)
        //     {
        //         paint.Color = KnobBorder.CachedValue;
        //         paint.IsStroke = true;
        //         paint.StrokeWidth = KnobBorderSize;

        //         canvas.DrawRoundRect(knobRoundRect, paint);
        //     }

        //     paint.IsStroke = true;
        //     paint.StrokeWidth = 2f;
        //     paint.Color = SKColors.White.WithAlpha(15);
        //     paint.BlendMode = SKBlendMode.Plus;
        //     paint.Shader = null;

        //     canvas.DrawRoundRect(knobRoundRect, paint);
        // }
    }
}