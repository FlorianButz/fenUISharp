using FenUISharp.Logging;
using FenUISharp.Mathematics;
using FenUISharp.Objects;
using SkiaSharp;

namespace FenUISharp.Materials
{
    public class GlassMaterial : Material
    {
        public Func<SKColor> BaseColor
        {
            get => GetProp<Func<SKColor>>("BaseColor", () => SKColors.White);
            set => SetProp("BaseColor", value);
        }

        public Func<SKColor> Highlight
        {
            get => GetProp<Func<SKColor>>("Highlight", () => BaseColor().Multiply(2f).WithAlpha(225));
            set => SetProp("Highlight", value);
        }

        public Func<SKImage?> GrabPassFunction
        {
            get => GetProp<Func<SKImage?>>("GrabPassFunction", () => null);
            set => SetProp("GrabPassFunction", value);
        }

        public Func<float> BlurRadius
        {
            get => GetProp<Func<float>>("BlurRadius", () => 5);
            set => SetProp("BlurRadius", value);
        }

        public Func<float> Displacement
        {
            get => GetProp<Func<float>>("Displacement", () => 65);
            set => SetProp("Displacement", value);
        }

        public Func<int> Distance
        {
            get => GetProp<Func<int>>("Distance", () => 8);
            set => SetProp("Distance", value);
        }

        public Func<float> Brightness
        {
            get => GetProp<Func<float>>("Brightness", () => 1.3f);
            set => SetProp("Brightness", value);
        }

        public Func<float> GrabQuality
        {
            get => GetProp<Func<float>>("GrabQuality", () => 1f);
            set => SetProp("GrabQuality", value);
        }

        public Func<float> FalloffPower
        {
            get => GetProp<Func<float>>("FalloffPower", () => 3f);
            set => SetProp("FalloffPower", value);
        }

        public Func<Vector2> ScaleInput
        {
            get => GetProp<Func<Vector2>>("ScaleInput", () => Vector2.One);
            set => SetProp("ScaleInput", value);
        }

        private int DisplacementMapDownscale = 1;

        public GlassMaterial(Func<SKImage?> grabPassFunction)
        {
            GrabPassFunction = grabPassFunction;
        }

        protected override void Draw(SKCanvas targetCanvas, SKPath path, UIObject caller, SKPaint paint)
        {
            using var windowArea = GrabPassFunction();
            if (windowArea == null) return;

            float quality = Math.Clamp(GrabQuality(), 0.1f, 1f);

            int w = (int)(windowArea.Width * quality);
            int h = (int)(windowArea.Height * quality);

            using var scaledSurface = FContext.GetCurrentWindow()
                .RenderResources?
                .CreateAdditional(new SKImageInfo(w, h));

            if (scaledSurface == null) return;

            using (var c = scaledSurface.SkiaSurface.Canvas)
            {
                c.Clear(SKColors.Transparent);

                c.Scale(quality, quality);

                c.DrawImage(windowArea, 0, 0);
            }

            using var lowResWindowArea = scaledSurface.SkiaSurface.Snapshot();

            var grabQuality = GrabQuality();
            path.GetBounds(out SKRect pathBounds);
            if (pathBounds.Width == 0 || pathBounds.Height == 0) return;

            caller.Padding.SetStaticState(35, 1);

            using var blurSurf = FContext.GetCurrentWindow().RenderResources?.CreateAdditional(windowArea.Info);
            if (blurSurf == null) return;

            {
                using var blurCanv = blurSurf.SkiaSurface.Canvas;
                var bRadius = BlurRadius();

                if (!FenUI.Flags.Contains("disable_blureffects"))
                {
                    using var blur = SKImageFilter.CreateBlur(bRadius / 1.25f, bRadius / 1.25f);
                    using var blurPaint = new SKPaint { IsAntialias = true, ImageFilter = blur };
                    blurCanv.DrawImage(lowResWindowArea, 0, 0, blurPaint);
                }
                else
                    blurCanv.DrawImage(lowResWindowArea, 0, 0);
            }

            using var blurredWindowArea = blurSurf.SkiaSurface.Snapshot();

            SKImageInfo skImageInfo = new((int)MathF.Ceiling(pathBounds.Width / DisplacementMapDownscale), (int)MathF.Ceiling(pathBounds.Height / DisplacementMapDownscale));
            using var displacementSurface = FContext.GetCurrentWindow().RenderResources?.CreateAdditional(skImageInfo);
            if (displacementSurface == null) return;

            using var displacementMapCanvas = displacementSurface.SkiaSurface.Canvas;
            int falloff = Distance();

            Vector2 pathOffsetAdjustment = new(-pathBounds.Left, -pathBounds.Top);
            displacementMapCanvas.Translate(pathOffsetAdjustment.x, pathOffsetAdjustment.y);

            using var displacementPaint = new SKPaint
            {
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Round,
                StrokeJoin = SKStrokeJoin.Round,
            };

            displacementPaint.Style = SKPaintStyle.Fill;
            displacementPaint.Color = SKColors.White;
            displacementMapCanvas.DrawPath(path, displacementPaint);
            displacementPaint.Style = SKPaintStyle.Stroke;

            for (int i = falloff; i >= 1; i--)
            {
                float alpha = (float)i / (float)falloff;
                displacementPaint.Color = new SKColor((byte)(alpha * 255), (byte)(alpha * 255), (byte)(alpha * 255), 255);
                displacementPaint.StrokeWidth = i * 2;
                displacementMapCanvas.DrawPath(path, displacementPaint);
            }

            using var dispImage = displacementSurface.SkiaSurface.Snapshot();
            using SKShader displacementMap = dispImage.ToShader(
                SKShaderTileMode.Decal, SKShaderTileMode.Decal,
                new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.Nearest));

            SKMatrix scale = SKMatrix.CreateScale(
                1f / grabQuality,
                1f / grabQuality);

            using var grabPassShader = blurredWindowArea.ToShader(
                SKShaderTileMode.Decal,
                SKShaderTileMode.Decal,
                new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear),
                scale);

            string sksl = @"
                uniform shader contentShader;
                uniform shader grabPass;

                uniform float3 iBaseMix;
                uniform float2 iResolution;
                uniform float2 iOff;
                uniform float2 iPathCorrection;
                uniform float iBright;

                uniform float iDisplacement;
                uniform float iDownscale;
                uniform float iGrabQuality;
                uniform float iFalloffPower;
                uniform float2 iScale;

                half4 main(float2 fragCoord) {
                    float2 uv = (fragCoord - iOff) * iScale;
                    float2 dispUV = (fragCoord + iPathCorrection) * iDownscale;

                    float offsetAmount = contentShader.eval(dispUV).r;
                    float edgeDist = 1.0 - offsetAmount;

                    float eps = 2.0;
                    float2 grad;
                    grad.x = contentShader.eval(dispUV + float2(eps, 0)).r
                           - contentShader.eval(dispUV - float2(eps, 0)).r;
                    grad.y = contentShader.eval(dispUV + float2(0, eps)).r
                           - contentShader.eval(dispUV - float2(0, eps)).r;

                    float gradLen = length(grad);
                    float2 dir = gradLen > 0.001 ? -grad / gradLen : float2(0, 0);

                    float displacement = mix(iDisplacement * 0.05, iDisplacement, pow(edgeDist, iFalloffPower));
                    float2 displacedUV = (uv + dir * displacement);
                    float2 sampledUV = displacedUV;
                    float4 backColor = grabPass.eval(sampledUV);

                    float4 returnColor = (backColor * iBright + ((iBright - 1) / 2)) * float4(iBaseMix, 1);
                    returnColor.a = 1;
                    return returnColor;
                }
            ";

            var effect = SKRuntimeEffect.CreateShader(sksl, out var err);
            if (effect == null)
            {
                FLogger.Error($"Shader compilation failed: {err}");
                return;
            }

            var uniforms = new SKRuntimeEffectUniforms(effect);
            uniforms["iBaseMix"] = new float[] { (float)BaseColor().Red / 255f, (float)BaseColor().Green / 255f, (float)BaseColor().Blue / 255f };
            uniforms["iResolution"] = new float[] { pathBounds.Width, pathBounds.Height };
            uniforms["iOff"] = new float[] { (float)-caller.Padding.CachedValue, (float)-caller.Padding.CachedValue };
            uniforms["iPathCorrection"] = new float[] { (float)pathOffsetAdjustment.x, (float)pathOffsetAdjustment.y };
            uniforms["iDisplacement"] = (float)Displacement();
            uniforms["iBright"] = (float)Brightness();
            uniforms["iDownscale"] = (float)DisplacementMapDownscale;
            uniforms["iGrabQuality"] = grabQuality;
            uniforms["iFalloffPower"] = (float)FalloffPower();
            uniforms["iScale"] = new float[] { (float)ScaleInput().x, (float)ScaleInput().y };

            var children = new SKRuntimeEffectChildren(effect);
            children["contentShader"] = displacementMap;
            children["grabPass"] = grabPassShader;

            using var masterShader = effect.ToShader(uniforms, children);

            int unmodified = targetCanvas.Save();
            targetCanvas.ClipPath(path, antialias: true);

            paint.Shader = masterShader;

            var mainBRadius = BlurRadius();
            using var mainBlur = SKImageFilter.CreateBlur(mainBRadius / 4, mainBRadius / 4);
            paint.ImageFilter = mainBlur;

            targetCanvas.DrawPath(path, paint);

            using var highlightPaint = new SKPaint
            {
                Color = paint.Color,
                IsStroke = true,
                StrokeWidth = 1.5f,
                IsAntialias = true
            };
            using (var gradientShader = SKShader.CreateLinearGradient(
                                new SKPoint(RMath.Lerp(pathBounds.Left, pathBounds.MidX, 0.4f), pathBounds.Top),
                                new SKPoint(RMath.Lerp(pathBounds.Right, pathBounds.MidX, 0.4f), pathBounds.Bottom),
                                new[] { Highlight(), SKColors.White.WithAlpha(0), Highlight() },
                                null,
                                SKShaderTileMode.Clamp))
                highlightPaint.Shader = gradientShader;
            targetCanvas.DrawPath(path, highlightPaint);

            targetCanvas.RestoreToCount(unmodified);

            // Flush and wait for GPU before disposing GPU-backed resources
            targetCanvas.Flush();
            var res = FContext.GetCurrentWindow().RenderResources;
            if (res?.grContext != null)
            {
                res.grContext.Flush();
                res.grContext.Submit(true);
                res.WaitForGpu();
            }

            caller.Invalidate(UIObject.Invalidation.SurfaceDirty);
        }
    }
}