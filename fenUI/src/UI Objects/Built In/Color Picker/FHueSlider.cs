using FenUISharp.Mathematics;
using SkiaSharp;

namespace FenUISharp.Objects
{
    public class FHueSlider : FSlider
    {
        public FHueSlider()
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
                uniform float2 iOffset;

                half4 main(float2 fragCoord) {
                    float2 uv = (fragCoord) / iResolution;

                    float hue = uv.x; // hue from 0 to 360

                    // Convert hue to radians and then to RGB
                    float c = 1;
                    float h = (hue * 360) / 60.0;
                    float x = c * (1.0 - abs(mod(h, 2.0) - 1.0));
                    
                    float3 rgb;
                    if (0.0 <= h && h < 1.0) rgb = float3(c, x, 0.0);
                    else if (1.0 <= h && h < 2.0) rgb = float3(x, c, 0.0);
                    else if (2.0 <= h && h < 3.0) rgb = float3(0.0, c, x);
                    else if (3.0 <= h && h < 4.0) rgb = float3(0.0, x, c);
                    else if (4.0 <= h && h < 5.0) rgb = float3(x, 0.0, c);
                    else rgb = float3(c, 0.0, x);

                    return half4(rgb, 1.0);
                }
            ";

            SKRuntimeEffect effect = SKRuntimeEffect.CreateShader(sksl, out var err);
            if (effect == null) Console.WriteLine($"Shader compilation failed: {err}");

            var uniforms = new SKRuntimeEffectUniforms(effect);
            uniforms["iResolution"] = new float[] { rect.Width, rect.Height };
            uniforms["iOffset"] = new float[] { rect.Left - Shape.SurfaceDrawRect.Left, rect.Top - Shape.SurfaceDrawRect.Top };

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

        protected override void RenderKnob(SKCanvas canvas, SKRect knobRect)
        {
            // base.RenderKnob(canvas, knobRect);

            using var paint = GetRenderPaint();
            paint.Color = SKColor.FromHsv(_value * 360, 100, 100);

            using (var shadow = SKImageFilter.CreateDropShadow(0, 1, 5, 5, KnobShadow.CachedValue))
                paint.ImageFilter = shadow;

            using var knobRoundRect = new SKRoundRect(knobRect, KnobCornerRadius);
            canvas.DrawRoundRect(knobRoundRect, paint);

            if (KnobBorderSize > 0)
            {
                paint.Color = KnobBorder.CachedValue;
                paint.IsStroke = true;
                paint.StrokeWidth = KnobBorderSize;

                canvas.DrawRoundRect(knobRoundRect, paint);
            }

            paint.IsStroke = true;
            paint.StrokeWidth = 2f;
            paint.Color = SKColors.White.WithAlpha(15);
            paint.BlendMode = SKBlendMode.Plus;
            paint.Shader = null;

            canvas.DrawRoundRect(knobRoundRect, paint);
        }
    }
}