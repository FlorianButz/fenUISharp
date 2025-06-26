using FenUISharp.Mathematics;
using SkiaSharp;

namespace FenUISharp.Objects
{
    public class FColorPicker : FPanel
    {
        public SKColor PickedColor { get => GetColor(); set => SetColor(value); }
        public Action<SKColor>? OnColorUpdated { get; set; }

        private FHueSlider hueSlider;
        private SKRect pickerInnerBounds;

        private InteractiveSurface pickerSurface;

        private Vector2 pickerKnobPos;
        private Vector2 pickerKnobDisplayPos;

        private Spring knobSpring;
        private Spring knobSizeSpring;

        public FColorPicker() : base(size: () => new(150, 150))
        {
            _drawBasePanel = false;

            knobSpring = new(3.5f, 2f);
            knobSizeSpring = new(2.7f, 2f);

            pickerSurface = new(this, FContext.GetCurrentDispatcher(), () => Transform.DrawLocalToGlobal(pickerInnerBounds));
            pickerSurface.EnableMouseActions.SetStaticState(true);
            pickerSurface.OnMouseAction += MouseAction;
            pickerSurface.OnDrag += DragPicker;

            hueSlider = new();
            hueSlider.Layout.StretchHorizontal.SetStaticState(true);
            hueSlider.Layout.Alignment.SetStaticState(new(0.5f, 1f));
            hueSlider.Layout.AlignmentAnchor.SetStaticState(new(0.5f, 1f));

            // Making it look better
            // hueSlider.KnobSize = new(5, hueSlider.KnobSize.y);
            hueSlider.KnobSize = new(7.5f, hueSlider.KnobSize.y - 5);
            hueSlider.Transform.Size.SetStaticState(new(0, hueSlider.KnobSize.y - 10));
            hueSlider.Transform.LocalPosition.SetStaticState(new(0, -5));
            hueSlider.DisplayFill = false;

            hueSlider.OnValueChanged += (x) => Invalidate(Invalidation.SurfaceDirty);

            Padding.SetStaticState(15);

            hueSlider.SetParent(this);
        }

        private SKColor GetColor()
        {
            return SKColor.FromHsv(hueSlider.Value * 360, (pickerKnobPos.x / pickerInnerBounds.Width) * 100, (1f - (pickerKnobPos.y / pickerInnerBounds.Height)) * 100);
        }

        private void SetColor(SKColor color)
        {
            color.ToHsv(out float hue, out float sat, out float val);
            hueSlider.Value = hue / 360f;

            pickerKnobPos = new((sat / 100f) * pickerInnerBounds.Width, (1f - (val / 100f)) * pickerInnerBounds.Height);
            OnColorUpdated?.Invoke(PickedColor);
        }

        private void MouseAction(MouseInputCode code)
        {
            if (code.button == MouseInputButton.Left && code.state == MouseInputState.Down)
            {
                SetToPoint(FContext.GetCurrentWindow().ClientMousePosition);
            }
        }

        private void DragPicker(Vector2 vector)
        {
            SetToPoint(FContext.GetCurrentWindow().ClientMousePosition);
        }

        private void SetToPoint(Vector2 point)
        {
            pickerKnobPos = Transform.GlobalToDrawLocal(point);
            pickerKnobPos = Vector2.Clamp(pickerKnobPos, Vector2.Zero, new(pickerInnerBounds.Width, pickerInnerBounds.Height));

            OnColorUpdated?.Invoke(PickedColor);
        }

        public override void Dispose()
        {
            base.Dispose();

            pickerSurface.Dispose();
        }

        protected override void Update()
        {
            base.Update();

            pickerInnerBounds = Shape.LocalBounds;
            pickerInnerBounds.Offset(0, -(hueSlider.Transform.Size.CachedValue.y / 2 + 10));
            pickerInnerBounds.Inflate(0, -(hueSlider.Transform.Size.CachedValue.y / 2 + 10));

            var lastVal = pickerKnobPos + knobSizeSpring.GetLastValue().x;

            knobSizeSpring.Update(FContext.DeltaTime, new(pickerSurface.IsMouseDown ? 12.5f : 5, 0f));
            pickerKnobDisplayPos = knobSpring.Update(FContext.DeltaTime, pickerKnobPos);
            
            if (lastVal != (pickerKnobDisplayPos + knobSizeSpring.GetLastValue().x)) Invalidate(Invalidation.SurfaceDirty);
        }

        public override void Render(SKCanvas canvas)
        {
            // base.Render(canvas);

            RenderBasePanel(canvas, pickerInnerBounds);

            string sksl = @"
                uniform float2 iResolution;
                uniform float hue;

                half4 main(float2 fragCoord) {
                    float2 uv = fragCoord / iResolution;

                    // Calculate brightness (1.0 at top, 0.0 at bottom)
                    float brightness = 1.0 - uv.y;

                    // Saturation (0.0 at left, 1.0 at right)
                    float saturation = uv.x;

                    // Convert hue to radians and then to RGB
                    float c = brightness * saturation;
                    float h = hue / 60.0;
                    float x = c * (1.0 - abs(mod(h, 2.0) - 1.0));
                    
                    float3 rgb;
                    if (0.0 <= h && h < 1.0) rgb = float3(c, x, 0.0);
                    else if (1.0 <= h && h < 2.0) rgb = float3(x, c, 0.0);
                    else if (2.0 <= h && h < 3.0) rgb = float3(0.0, c, x);
                    else if (3.0 <= h && h < 4.0) rgb = float3(0.0, x, c);
                    else if (4.0 <= h && h < 5.0) rgb = float3(x, 0.0, c);
                    else rgb = float3(c, 0.0, x);

                    // Mix with white (for left side) and black (for bottom)
                    float3 white = float3(1.0);
                    float3 color = mix(white, rgb, saturation); // Horizontal gradient
                    color *= brightness; // Vertical gradient

                    return half4(color, 1.0);
                }
            ";

            SKRuntimeEffect effect = SKRuntimeEffect.CreateShader(sksl, out var err);
            if (effect == null) Console.WriteLine($"Shader compilation failed: {err}");

            var uniforms = new SKRuntimeEffectUniforms(effect);
            uniforms["iResolution"] = new float[] { pickerInnerBounds.Width, pickerInnerBounds.Height };
            uniforms["hue"] = hueSlider.Value * 360f;

            using var paint = GetRenderPaint();
            paint.Shader = effect?.ToShader(uniforms);

            using var panelPath = GetPanelPath(pickerInnerBounds);
            canvas.DrawPath(panelPath, paint);

            paint.Shader = null;
            paint.IsStroke = true;
            paint.StrokeWidth = 1f;
            paint.BlendMode = SKBlendMode.Plus;
            paint.Color = new(150, 150, 150);

            canvas.DrawCircle(new SKPoint(pickerKnobDisplayPos.x, pickerKnobDisplayPos.y), knobSizeSpring.GetLastValue().x, paint);

            paint.IsStroke = false;
            paint.Color = new(65, 65, 65);
            canvas.DrawCircle(new SKPoint(pickerKnobDisplayPos.x, pickerKnobDisplayPos.y), knobSizeSpring.GetLastValue().x, paint);
        }
    }
}