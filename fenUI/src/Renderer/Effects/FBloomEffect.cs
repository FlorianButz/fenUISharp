using FenUISharp.Logging;
using FenUISharp.Mathematics;
using SkiaSharp;

namespace FenUISharp.RuntimeEffects
{
    public class FBloomEffect : IPostProcessEffect
    {
        public float BloomSpread { get; set; } = 10f;
        public float BloomIntensity { get; set; } = 1f;
        public float BloomThreshold { get; set; } = 0.75f;
        public int Downsampling { get; set; } = 2;

        public void OnAfterRender(PPInfo info)
        {
            var grContext = FContext.GetCurrentWindow().SkiaDirectCompositionContext?.grContext;

            var snap = info.source.Snapshot();
            int downscale = RMath.Clamp(Downsampling, 1, 12); int downWidth = snap.Width / downscale; int downHeight = snap.Height / downscale;

            var downInfo = new SKImageInfo(downWidth, downHeight);
            var downscaledSurface = FContext.GetCurrentWindow().SkiaDirectCompositionContext?.CreateAdditional(downInfo);
            if (downscaledSurface == null) return;

            var downCanvas = downscaledSurface.SkiaSurface.Canvas;
            downCanvas.Clear(SKColors.Transparent);
            downCanvas.Scale(1f / downscale);

            var thresholdIntensityShader = CreateShader(info, snap);
            var blurFilter = SKImageFilter.CreateBlur(RMath.Clamp(BloomSpread, 0f, 35f) / downscale, RMath.Clamp(BloomSpread, 0f, 35f) / downscale);
            var bloomPaint = new SKPaint { Shader = thresholdIntensityShader };

            downCanvas.DrawRect(0, 0, info.sourceInfo.Width, info.sourceInfo.Height, bloomPaint);
            var downscaledImage = downscaledSurface.SkiaSurface.Snapshot();

            int save = info.target.Canvas.Save();
            var drawPaint = new SKPaint { BlendMode = SKBlendMode.Plus, ImageFilter = blurFilter };

            info.target.Canvas.ResetMatrix();
            info.target.Canvas.Scale(downscale);
            info.target.Canvas.DrawImage(downscaledImage, 0, 0, drawPaint);
            info.target.Canvas.RestoreToCount(save);

            // Force all queued GPU work to finish before disposing
            info.target.Canvas.Flush();
            grContext?.Flush();
            grContext?.Submit(true);
            FContext.GetCurrentWindow().SkiaDirectCompositionContext?.WaitForGPU();

            // Dispose manually after GPU finish
            downscaledImage.Dispose();
            downscaledSurface.Dispose();
            bloomPaint.Dispose();
            blurFilter.Dispose();
            thresholdIntensityShader.Dispose();
            snap.Dispose();
        }

        private SKShader CreateShader(PPInfo info, SKImage snapshot)
        {
            string sksl = @"
                uniform shader contentShader;

                uniform float downsampling;
                uniform float intensity;
                uniform float threshold;
                uniform float2 iResolution;

                half4 main(float2 fragCoord) {
                    float2 uv = (fragCoord);

                    float4 color = contentShader.eval(uv);
                    float brightness = dot(color.rgb, vec3(0.2126, 0.7152, 0.0722));
                    if(brightness < threshold) color = float4(0, 0, 0, 0);

                    return color * intensity;
                }
            ";

            SKRuntimeEffect effect = SKRuntimeEffect.CreateShader(sksl, out var err);
            if (effect == null) FLogger.Error($"Shader compilation failed: {err}");

            var uniforms = new SKRuntimeEffectUniforms(effect);
            uniforms["iResolution"] = new float[] { info.sourceInfo.Width, info.sourceInfo.Height };
            uniforms["intensity"] = RMath.Clamp(BloomIntensity, 0, 10);
            uniforms["threshold"] = RMath.Clamp(BloomThreshold, 0, 1);
            uniforms["downsampling"] = (float)RMath.Clamp(Downsampling, 1, 12);

            var children = new SKRuntimeEffectChildren(effect);
            children["contentShader"] = snapshot.ToShader(SKShaderTileMode.Decal, SKShaderTileMode.Decal, new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.Nearest));

            return effect?.ToShader(uniforms, children) ?? throw new Exception("Unable to create shader.");
        }

        public void OnBeforeRender(PPInfo info)
        {

        }

        public void OnLateAfterRender(PPInfo info)
        {

        }
    }
}