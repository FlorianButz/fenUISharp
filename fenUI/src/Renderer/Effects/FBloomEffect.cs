using FenUISharp.Logging;
using FenUISharp.Mathematics;
using SkiaSharp;

namespace FenUISharp.RuntimeEffects
{
    public class FBloomEffect : IPostProcessEffect
    {
        private const string BLOOM_SKSL = @"
            uniform shader contentShader;
            uniform float downsampling;
            uniform float intensity;
            uniform float threshold;
            uniform float2 iResolution;

            half4 main(float2 fragCoord) {
                float2 uv = fragCoord;
                float4 color = contentShader.eval(uv);
                float brightness = dot(color.rgb, float3(0.2126, 0.7152, 0.0722));
                if (brightness < threshold) color = float4(0,0,0,0);
                return color * intensity;
            }";

        private SKRuntimeEffect _bloomEffect;
        private FAdditionalSurface _downscaledSurface;
        private int _cachedDownWidth, _cachedDownHeight;

        public float BloomSpread { get; set; } = 10f;
        public float BloomIntensity { get; set; } = 1f;
        public float BloomThreshold { get; set; } = 0.75f;
        public int Downsampling { get; set; } = 2;

        public FBloomEffect()
        {
            _bloomEffect = SKRuntimeEffect.CreateShader(BLOOM_SKSL, out var err);
            if (_bloomEffect == null)
            {
                FLogger.Error($"Bloom shader compilation failed: {err}");
            }
        }

        public void OnAfterRender(PPInfo info)
        {
            if (info.source == null || info.target == null) return;

            var grContext = FContext.GetCurrentWindow().RenderResources?.grContext;
            if (grContext == null) return;

            var snap = info.source.Snapshot();
            int downscale = RMath.Clamp(Downsampling, 1, 12);
            int downWidth = snap.Width / downscale;
            int downHeight = snap.Height / downscale;

            if (_downscaledSurface == null || _cachedDownWidth != downWidth || _cachedDownHeight != downHeight)
            {
                _downscaledSurface?.Dispose();
                var downInfo = new SKImageInfo(downWidth, downHeight);
                _downscaledSurface = FContext.GetCurrentWindow().RenderResources?.CreateAdditional(downInfo);
                _cachedDownWidth = downWidth;
                _cachedDownHeight = downHeight;
            }

            if (_downscaledSurface == null) { snap.Dispose(); return; }

            var downCanvas = _downscaledSurface.SkiaSurface.Canvas;
            downCanvas.Clear(SKColors.Transparent);
            downCanvas.Scale(1f / downscale);

            var thresholdIntensityShader = CreateShader(info, snap);
            var blurFilter = SKImageFilter.CreateBlur(RMath.Clamp(BloomSpread, 0f, 35f) / downscale, RMath.Clamp(BloomSpread, 0f, 35f) / downscale);
            var bloomPaint = new SKPaint { Shader = thresholdIntensityShader };

            downCanvas.DrawRect(0, 0, info.sourceInfo.Width, info.sourceInfo.Height, bloomPaint);
            var downscaledImage = _downscaledSurface.SkiaSurface.Snapshot();

            int save = info.target.Canvas.Save();
            var drawPaint = new SKPaint { BlendMode = SKBlendMode.Plus, ImageFilter = blurFilter };

            info.target.Canvas.ResetMatrix();
            info.target.Canvas.Scale(downscale);
            info.target.Canvas.DrawImage(downscaledImage, 0, 0, drawPaint);
            info.target.Canvas.RestoreToCount(save);

            // Dispose per-frame resources
            downscaledImage.Dispose();
            bloomPaint.Dispose();
            blurFilter.Dispose();
            thresholdIntensityShader?.Dispose();
            snap.Dispose();
        }

        private SKShader CreateShader(PPInfo info, SKImage snapshot)
        {
            if (_bloomEffect == null) return null;
            var uniforms = new SKRuntimeEffectUniforms(_bloomEffect);
            uniforms["iResolution"] = new float[] { info.sourceInfo.Width, info.sourceInfo.Height };
            uniforms["intensity"] = RMath.Clamp(BloomIntensity, 0, 10);
            uniforms["threshold"] = RMath.Clamp(BloomThreshold, 0, 1);
            uniforms["downsampling"] = (float)RMath.Clamp(Downsampling, 1, 12);

            var children = new SKRuntimeEffectChildren(_bloomEffect);
            children["contentShader"] = snapshot.ToShader(SKShaderTileMode.Decal, SKShaderTileMode.Decal, new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.Nearest));

            return _bloomEffect.ToShader(uniforms, children) ?? throw new Exception("Unable to create shader.");
        }

        public void OnBeforeRender(PPInfo info)
        {

        }

        public void OnLateAfterRender(PPInfo info)
        {

        }
    }
}
