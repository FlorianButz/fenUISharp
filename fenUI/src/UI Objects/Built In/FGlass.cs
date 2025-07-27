using FenUISharp;
using FenUISharp.Logging;
using FenUISharp.Mathematics;
using SkiaSharp;

namespace FenUISharp.Objects
{
    public class FGlass : FPanel
    {
        const string sksl = @"
            uniform float iOff;
            uniform shader contentShader; // Content behind the glass
            uniform float2 iResolution;
            uniform float2 iSize;

            uniform float iStrength; // 0..1, how strong the refraction is
            uniform float iFresnelStrength;
            uniform float iIOR; // Index of refraction (1.0 = no refraction, 1.45 = glass, 2.0 = diamond)
            uniform float iChromaticAberration; // 0 = none, higher = more color split

            uniform float iCornerRadius;

            float3 lerp(float3 a, float3 b, float t) {
                return a + (b - a) * t;
            }

            float sdRoundRect(float2 position, float2 halfSize, float cornerRadius) {
                position = abs(position) - halfSize + cornerRadius;
                return length(max(position, 0.0)) + min(max(position.x, position.y), 0.0) - cornerRadius;
            }

            // float3 calcNormalRoundRect(float2 p, float2 size, float radius, float distNormalized) {
            //     float eps = 0.3;
            //     // float eps = 0.3;

            //     float dx = sdRoundRect(p + float2(eps, 0.0), size, radius) - sdRoundRect(p - float2(eps, 0.0), size, radius);
            //     float dy = sdRoundRect(p + float2(0.0, eps), size, radius) - sdRoundRect(p - float2(0.0, eps), size, radius);
            //     float dz = eps * 2.0;

            //     // Applying some very very bad unheard of trickery.
            //     // float m = clamp(smoothstep(0, 1 - (((min(size.x, size.y) - radius) / min(size.x, size.y))), distNormalized), 0, 1);
            //     float m = 0;
            //     float rM = clamp(pow(smoothstep(0.25, 0.75, distNormalized), 1 / (radius / 35)), 0, 1);

            //     // return float3(m, m, m); // Debug

            //     float3 n = normalize(float3(dx * (1-m), dy * (1-m), dz * rM)); // Returning the abomination
            //     return n;
            // }

            // float3 calcNormalRoundRect(float2 p, float2 size, float radius) {
            //     // tweak this epsilon smaller for more precision / slower, larger for speed / blurrier
            //     // float eps = 1e-3;
            //     float eps = 400;

            //     // central‐difference in X and Y
            //     float dx = sdRoundRect(p + float2(eps, 0.0), size, radius)
            //             - sdRoundRect(p - float2(eps, 0.0), size, radius);
            //     float dy = sdRoundRect(p + float2(0.0, eps), size, radius)
            //             - sdRoundRect(p - float2(0.0, eps), size, radius);

            //     // form the 3D normal (Z is zero because this is a 2D shape in XY plane)
            //     float3 n = normalize(float3(dx, dy, 0.0));
            //     return n;
            // }

            float3 calcNormalRoundRect(float2 p, float2 size, float radius) {
                float esp = 0.3;
                float d = 0.9;
                float s = 6;
                float off = 6;

                float dx = 1-sdRoundRect(p + esp, size, radius);
                float dy = 1-sdRoundRect(p - esp, size, radius);

                float distNormalizedX = dx / (sqrt(iSize.x * iSize.x + iSize.y * iSize.y) / 2);
                float distNormalizedY = dy / (sqrt(iSize.x * iSize.x + iSize.y * iSize.y) / 2);

                float nx = clamp(1 - pow(smoothstep(0, d, distNormalizedX), s), 0, 1);
                float ny = clamp(1 - pow(smoothstep(0, d, distNormalizedY), s), 0, 1);

                // return float3(distNormalizedX, distNormalizedY, 0);
                // return float3(nx, ny, 0);

                return float3(nx * off, ny * off, 1);
            }


            half4 main(float2 fragCoord) {
                float2 uv = (fragCoord - iOff);

                // Calculate normalized center and radius
                float2 center = (iSize-iOff*2) * 0.5;
                
                float radius = min(iSize.x, iSize.y) * 0.5;
                // float cRadius = iCornerRadius * (radius / iCornerRadius);
                float cRadius = iCornerRadius * 2.75 * (iSize.x / iSize.y);
                
                float2 toCenter = uv - center;
                float dist = 1 - sdRoundRect((uv + iOff/2) - (iSize) / 2, (iSize - iOff) / 2, cRadius);
                float distNormalized = dist / (sqrt(iSize.x * iSize.x + iSize.y * iSize.y) / 2);

                // Mask: only process inside
                // if (dist < radius/1.5) {
                //     return half4(0,0,0,0); // Transparent outside
                // }

                radius = (sqrt(iSize.x * iSize.x + iSize.y * iSize.y) / 2);

                // Normalized direction from center
                float2 normDir = toCenter / radius;

                // Spherical normal (z points towards viewer)
                float z = sqrt(1.0 - clamp(dot(normDir, normDir), 0.0, 1));
                float3 normal = float3(normDir, z);

                normal = calcNormalRoundRect((uv + iOff/2) - (iSize) / 2, (iSize), cRadius);

                // View direction is (0,0,1)
                float3 view = float3(0, 0, 1);

                float3 refractDirR;
                float3 refractDirG;
                float3 refractDirB;

                float chroma = iChromaticAberration * 0.004 * radius;
                float baseEta = 1.0 / iIOR;
                float etaR = baseEta * (1.0 - chroma);
                float etaG = baseEta;
                float etaB = baseEta * (1.0 + chroma);

                // Snell's Law for refraction
                {
                    float cosi = -dot(normal, view);
                    float k = 1.0 - etaR*etaR*(1.0 - cosi*cosi);
                    refractDirR = etaR * view + (etaR * cosi - sqrt(abs(k))) * normal;
                }

                {
                    float cosi = -dot(normal, view);
                    float k = 1.0 - etaG*etaG*(1.0 - cosi*cosi);
                    refractDirG = etaG * view + (etaG * cosi - sqrt(abs(k))) * normal;
                }

                {
                    float cosi = -dot(normal, view);
                    float k = 1.0 - etaB*etaB*(1.0 - cosi*cosi);
                    refractDirB = etaB * view + (etaB * cosi - sqrt(abs(k))) * normal;
                }

                float dN = clamp((1-distNormalized) - (1-iStrength), 0, 1);

                // Project to 2D

                float2 offsetR = refractDirR.xy * radius * ((iIOR - 1.0) * dN);
                float2 offsetG = refractDirG.xy * radius * ((iIOR - 1.0) * dN);
                float2 offsetB = refractDirB.xy * radius * ((iIOR - 1.0) * dN);

                // Sample background with offsets (global coordinates!)
                half r = contentShader.eval((uv) + offsetR).r;
                half g = contentShader.eval((uv) + offsetG).g;
                half b = contentShader.eval((uv) + offsetB).b;
                half a = 1.0;

                // Optional: Add a subtle edge highlight (fake Fresnel)
                float fresnel = pow(1.0 - z, iIOR * 2) * iFresnelStrength;
                float addD = pow(1.0 - smoothstep(0, 0.2, distNormalized), 50) * (iFresnelStrength / 4);
                half3 color = half3(r, g, b) * (1 + clamp(fresnel * 0.5, 0, 1)) + clamp(fresnel * 0.5, 0, 1) + addD;

                // return half4(dist, dist, dist, 1); // Debug for dist
                // return half4(distNormalized,distNormalized, distNormalized, 1); // Debug for normalized dist

                // return half4(normal, a); // Debug for normals
                // return half4(refractDirR, a); // Debug for refractDir

                // return half4(offset, 0, a); // Debug for offset w.o. conversion
                // return half4(offset / 2 + 0.5, 0, a); // Debug for offset
                
                return half4(color, a);
            }
        ";

        const string MultiSamplingAntiAliasing = @"
            uniform shader inputShader;
                const float iterations = 2;

            half4 main(float2 fragCoord) {
                half4 color = half4(0);

                for (float y = 0; y < iterations; y++) {
                    for (float x = 0; x < iterations; x++) {
                        float2 p = float2((x - iterations / 2) * 0.5, (y - iterations / 2) * 0.5);
                        color += inputShader.eval(fragCoord + p);
                    }
                }

                return color * (1 / (iterations * 2));
            }
        ";

        public FGlass()
        {
            Padding.SetStaticState(50);
        }

        private float _strength = 1f;
        private float _fstrength = 0.25f;
        private float _ior = 1.55f;
        private float _chromaticAberration = 0.25f;
        private float _blurRadius = 5f;

        public float Strength
        {
            get => _strength;
            set => _strength = Math.Max(0f, Math.Min(1f, value));
        }

        public float FresnelStrength
        {
            get => _fstrength * _strength;
            set => _fstrength = Math.Max(0f, Math.Min(5f, value));
        }

        public float IOR
        {
            get => RMath.Lerp(1, _ior, _strength);
            set => _ior = RMath.Clamp(value, 1f, 10f);
        }

        public float ChromaticAberrationAmount
        {
            get => _chromaticAberration * _strength;
            set => _chromaticAberration = Math.Max(0f, Math.Min(10f, value));
        }

        public float BlurRadius
        {
            get => _blurRadius * _strength;
            set => _blurRadius = Math.Max(0f, Math.Min(10f, value));
        }

        private bool _enableAA = false;

        public FGlass(Func<Vector2>? position = null, Func<Vector2>? size = null) : base(position, size)
        {
            // this.PanelColor.Value = () => SKColors.Gray;
            this._drawBasePanel = true;
        }

        protected override void Update()
        {
            base.Update();
            UseSquircle.Value = () => false;
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();

            using var path = FContext.GetCurrentWindow().GetCurrentDirtyClipPath();
            if (FContext.GetCurrentWindow().IsNextFrameRendering() && RMath.IsRectPartiallyInside(Shape.GlobalBounds, path)) Invalidate(Invalidation.SurfaceDirty);
        }

        protected override void OnInvalidate(Invalidation invalidation)
        {
            base.OnInvalidate(invalidation);
            if (invalidation == Invalidation.TransformDirty) Invalidate(Invalidation.All);
        }

        private SKShader CreateShader(SKImage inputImage, float res) // The inputImage is the content behind the glass
        {
            var effect = SKRuntimeEffect.CreateShader(sksl, out var errorText);

            if (effect == null)
            {
                FLogger.Error($"Shader compilation failed: {errorText}");
                return SKShader.CreateEmpty();    
            }

            var uniforms = new SKRuntimeEffectUniforms(effect);
            // var bounds = Shape.SurfaceDrawRect;

            if (inputImage == null) return SKShader.CreateEmpty();

            uniforms["iResolution"] = new float[] { inputImage.Width, inputImage.Height };
            uniforms["iSize"] = new float[] { Shape.SurfaceDrawRect.Width, Shape.SurfaceDrawRect.Height };
            uniforms["iOff"] = (float)-Padding.CachedValue*2;

            uniforms["iIOR"] = IOR;
            uniforms["iStrength"] = 1f;
            uniforms["iChromaticAberration"] = ChromaticAberrationAmount;
            uniforms["iCornerRadius"] = RMath.Clamp(CornerRadius.CachedValue, 0f, Math.Min(Layout.ClampSize(Transform.Size.CachedValue).x / 2, Layout.ClampSize(Transform.Size.CachedValue).y / 2)) * res;
            uniforms["iFresnelStrength"] = FresnelStrength;

            var children = new SKRuntimeEffectChildren(effect); // Content behind the glass
            children["contentShader"] = inputImage.ToShader(SKShaderTileMode.Decal, SKShaderTileMode.Decal, new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.Nearest));

            if (_enableAA)
            {
                var effectAA = SKRuntimeEffect.CreateShader(MultiSamplingAntiAliasing, out var errorText2);
                if (effectAA == null) throw new Exception($"Shader compilation failed: {errorText2}");

                var uniformsAA = new SKRuntimeEffectUniforms(effectAA);

                var childrenAA = new SKRuntimeEffectChildren(effectAA);
                childrenAA["inputShader"] = effect.ToShader(uniforms, children);

                return effectAA.ToShader(uniformsAA, childrenAA);
            }

            return effect.ToShader(uniforms, children);
        }

        public override void Render(SKCanvas canvas)
        {
            if (Strength == 0) return;
            using var shaderPaint = GetRenderPaint();

            var resolution = 1;
            Quality.SetStaticState(resolution);

            var captureArea = Shape.LocalBounds;
            captureArea.Inflate(Padding.CachedValue, Padding.CachedValue);
            using var windowArea = this.Composition.GrabBehindPlusBuffer(Transform.DrawLocalToGlobal(captureArea), resolution);

            base.Render(canvas);

            // Create the shader with the captured content
            using var shader = CreateShader(windowArea, resolution);
            shaderPaint.Shader = shader;

            var panelBounds = Shape.LocalBounds;
            panelBounds.Inflate(1, 1);

            using (var blur = SKImageFilter.CreateBlur(BlurRadius, BlurRadius))
            using (var panelPath = GetPanelPath(panelBounds))
            {
                int s = canvas.Save();

                // canvas.SetMatrix(SKMatrix.Identity);
                var displayArea = Shape.LocalBounds;
                canvas.ClipPath(panelPath, antialias: true);

                canvas.Scale(1f / resolution, 1f / resolution, Shape.LocalBounds.Left, Shape.LocalBounds.Top);
                canvas.Scale(1.01f, 1.01f, Shape.LocalBounds.MidX, Shape.LocalBounds.MidY);

                shaderPaint.ImageFilter = blur;
                shaderPaint.Color = SKColors.White.WithAlpha((byte)((Strength) * 255));

                // Draw a rectangle with the shader applied
                canvas.DrawRect(displayArea, shaderPaint);

                canvas.RestoreToCount(s);

                using var strokePaint = new SKPaint() { Color = SKColors.White.WithAlpha(200), BlendMode = SKBlendMode.Plus, IsStroke = true, StrokeWidth = 1.5f, IsAntialias = true };
                using var gradient = SKShader.CreateLinearGradient(new SKPoint(Shape.LocalBounds.MidX - 15, Shape.LocalBounds.Top), new SKPoint(Shape.LocalBounds.MidX + 20, Shape.LocalBounds.Bottom),
                    new SKColor[] { SKColors.White, SKColors.Gray.WithAlpha(20) }, SKShaderTileMode.Clamp);
                strokePaint.Shader = gradient;

                canvas.DrawPath(panelPath, strokePaint);
            }
        }
    }
}