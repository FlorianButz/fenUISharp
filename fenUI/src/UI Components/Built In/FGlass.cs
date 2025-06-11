using FenUISharp;
using FenUISharp.Components;
using FenUISharp.Mathematics;
using SkiaSharp;

namespace FenUISharp
{
    public class FGlass : FPanel
    {
        const string sksl = @"
            uniform float iOff;
            uniform shader contentShader; // Content behind the glass
            uniform float2 iResolution;

            uniform float iStrength; // 0..1, how strong the refraction is
            uniform float iFresnelStrength;
            uniform float iIOR; // Index of refraction (1.0 = no refraction, 1.45 = glass, 2.0 = diamond)
            uniform float iChromaticAberration; // 0 = none, higher = more color split

            uniform float iCornerRadius;

            float3 lerp(float3 a, float3 b, float t) {
                return a + (b - a) * t;
            }

            float sdRoundRect(float2 p, float2 size, float radius) {
                float2 q = abs(p) - size + radius;
                return length(max(q, 0.0)) - radius + min(max(q.x, q.y), 0.0);
            }

            float3 calcNormalRoundRect(float2 p, float2 size, float radius, float distNormalized) {
                float eps = 400;
                // float eps = 0.3;

                float dx = sdRoundRect(p + float2(eps, 0.0), size, radius) - sdRoundRect(p - float2(eps, 0.0), size, radius);
                float dy = sdRoundRect(p + float2(0.0, eps), size, radius) - sdRoundRect(p - float2(0.0, eps), size, radius);
                float dz = eps * 2.0;

                // Applying some very very bad unheard of trickery.
                // float m = clamp(smoothstep(0, 1 - (((min(size.x, size.y) - radius) / min(size.x, size.y))), distNormalized), 0, 1);
                float m = 0;
                float rM = clamp(pow(smoothstep(0, 0.5, distNormalized), 0.33), 0, 1);

                // return float3(m, m, m); // Debug
                // return float3(distNormalized, distNormalized, distNormalized); // Debug

                float3 n = normalize(float3(dx * (1-m), dy * (1-m), dz * rM)); // Returning the abomination
                return n;
            }

            half4 main(float2 fragCoord) {
                float2 uv = (fragCoord - iOff);

                // Calculate normalized center and radius
                float2 center = iResolution * 0.5;
                float radius = min(iResolution.x, iResolution.y) * 0.5;
                
                float2 toCenter = uv - center;
                // float dist = length(toCenter); // Old from sphere
                float dist = 1 - sdRoundRect(uv - iResolution / 2, iResolution / 2, iCornerRadius);

                // Mask: only process inside
                if (dist < 1) {
                    return half4(0,0,0,0); // Transparent outside
                }

                radius = (sqrt(iResolution.x * iResolution.x + iResolution.y * iResolution.y) / 2);

                // Normalized direction from center
                float2 normDir = toCenter / radius;

                // Spherical normal (z points towards viewer)
                float z = sqrt(1.0 - clamp(dot(normDir, normDir), 0.0, 1.0));
                float3 normal = float3(normDir, z);

                float distNormalized = dist / (sqrt(iResolution.x * iResolution.x + iResolution.y * iResolution.y) / 2);
                normal = calcNormalRoundRect(uv - iResolution / 2, iResolution / 2, iCornerRadius, distNormalized);

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

                // Project to 2D
                float2 offsetR = refractDirR.xy * radius * iStrength * (iIOR - 1.0);
                float2 offsetG = refractDirG.xy * radius * iStrength * (iIOR - 1.0);
                float2 offsetB = refractDirB.xy * radius * iStrength * (iIOR - 1.0);

                // Sample background with offsets (global coordinates!)
                half r = contentShader.eval(uv + offsetR).r;
                half g = contentShader.eval(uv + offsetG).g;
                half b = contentShader.eval(uv + offsetB).b;
                half a = 1.0;

                // Optional: Add a subtle edge highlight (fake Fresnel)
                float fresnel = pow(1.0 - z, iIOR * 2) * iFresnelStrength;
                float addD = pow(1.0 - smoothstep(0, 0.2, distNormalized), 50) * (iFresnelStrength / 4);
                half3 color = half3(r, g, b) * (1 + clamp(fresnel * 0.5, 0, 1)) + clamp(fresnel * 0.5, 0, 1) + addD;

                // return half4(dist, dist, dist, 1); // Debug for dist
                // return half4(distNormalized,distNormalized, distNormalized, 1); // Debug for normalized dist

                // return half4(normal, a); // Debug for normal
                // return half4(refractDir, a); // Debug for refractDir

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


        private float _strength = 1f;
        private float _fstrength = 0.4f;
        private float _ior = 1.4f;
        private float _chromaticAberration = 0.25f;
        private float _blurRadius = 0.5f;

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

        public FGlass(Window rootWindow, Vector2 position, Vector2 size, float cornerRadius) : base(rootWindow, position, size, cornerRadius, new(SKColors.White))
        {
            this._drawBasePanel = true;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            UseSquircle = false;

            if (!WindowRoot.IsNextFrameRendering() || Strength < 0.05f) return;
            Invalidate();
        }

        private SKShader CreateShader(SKImage inputImage, float res) // The inputImage is the content behind the glass
        {
            var effect = SKRuntimeEffect.CreateShader(sksl, out var errorText);
            if (effect == null) throw new Exception($"Shader compilation failed: {errorText}");

            var uniforms = new SKRuntimeEffectUniforms(effect);
            var bounds = Transform.LocalBounds;

            uniforms["iResolution"] = new float[] { inputImage.Width, inputImage.Height };
            uniforms["iOff"] = (float)Transform.BoundsPadding.Value;

            uniforms["iIOR"] = IOR;
            uniforms["iStrength"] = 1f;
            uniforms["iChromaticAberration"] = ChromaticAberrationAmount;
            uniforms["iCornerRadius"] = RMath.Clamp(CornerRadius, 0f, Math.Min(Transform.Size.x / 2, Transform.Size.y / 2)) * res;
            uniforms["iFresnelStrength"] = FresnelStrength;

            var children = new SKRuntimeEffectChildren(effect); // Content behind the glass
            children["contentShader"] = inputImage.ToShader(SKShaderTileMode.Decal, SKShaderTileMode.Decal, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));

            if (_enableAA)
            {
                var effectAA = SKRuntimeEffect.CreateShader(MultiSamplingAntiAliasing, out var errorText2);
                if (effectAA == null) throw new Exception($"Shader compilation failed: {errorText2}");

                var uniformsAA = new SKRuntimeEffectUniforms(effectAA); // use effectAA here!

                var childrenAA = new SKRuntimeEffectChildren(effectAA);
                childrenAA["inputShader"] = effect.ToShader(uniforms, children);

                return effectAA.ToShader(uniformsAA, childrenAA);
            }

            return effect.ToShader(uniforms, children);
        }

        protected override void DrawToSurface(SKCanvas canvas)
        {
            base.DrawToSurface(canvas);

            if (Strength == 0) return;

            var shaderPaint = SkPaint.Clone();

            float res = 1f;
            // RenderQuality.SetValue(this, res, 50);

            var pos = new Vector2(Transform.Position.x + Transform.BoundsPadding.Value, Transform.Position.y + Transform.BoundsPadding.Value);
            var captureArea = new SKRect(pos.x, pos.y, pos.x + Transform.Size.x, pos.y + Transform.Size.y); // fix this shit

            var windowArea = WindowRoot.RenderContext.CaptureWindowRegion(captureArea, res);
            if (windowArea == null) return;

            using (var clearPaint = new SKPaint { BlendMode = SKBlendMode.Clear, IsAntialias = true })
            {
                var bounds = this.Transform.Bounds;
                bounds.Inflate(-2, -2);
                WindowRoot.RenderContext.Surface?.Canvas?.DrawPath(this.GetPanelPath(bounds), clearPaint);
            }

            // Create the shader with the captured content
            using var shader = CreateShader(windowArea, res);
            shaderPaint.Shader = shader;

            var panelBounds = Transform.LocalBounds;
            panelBounds.Inflate(1, 1);

            using (var blur = SKImageFilter.CreateBlur(BlurRadius, BlurRadius))
            using (var panelPath = GetPanelPath(panelBounds))
            {
                var displayArea = Transform.LocalBounds;
                canvas.ClipPath(panelPath, antialias: true);

                canvas.Scale(1f / res, 1f / res, Transform.LocalPosition.x, Transform.LocalPosition.y);
                canvas.Scale(1.01f, 1.01f, Transform.LocalBounds.MidX,Transform.LocalBounds.MidY);

                shaderPaint.ImageFilter = blur;
                shaderPaint.Color = SKColors.White.WithAlpha((byte)((Strength) * 255));

                // Draw a rectangle with the shader applied
                canvas.DrawRect(displayArea, shaderPaint);
            }

            shaderPaint.Dispose();
        }
    }
}