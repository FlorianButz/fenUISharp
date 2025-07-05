using FenUISharp.Behavior;
using FenUISharp.Materials;
using FenUISharp.Mathematics;
using FenUISharp.Objects.Buttons;
using FenUISharp.Objects.Text;
using FenUISharp.Objects.Text.Model;
using FenUISharp.Objects.Text.Layout;
using FenUISharp.States;
using SkiaSharp;

namespace FenUISharp.Objects
{
    public class FColorPatch : Button, IStateListener
    {
        // TODO: Global saving of color patches
        // TODO: Better ux
        // TODO: Add FNumericScroller instead

        private SKColor pickedColor;
        public SKColor PickedColor { get => GetColor(); set => SetColor(value); }
        public Action<SKColor>? OnColorUpdated { get; set; }
        public Action<SKColor>? OnUserColorUpdated { get; set; }

        public FColorPatch(SKColor? initialColor = null, Func<Vector2>? position = null, Func<Vector2>? size = null) : base(position: position, size: size ?? (() => new(50, 25)))
        {
            PickedColor = initialColor ?? new(255, 255, 255, 0);
            Padding.SetStaticState(10);
        }

        private void SetColor(SKColor color, bool notifyPicker = true)
        {
            pickedColor = color;
            if (activePicker != null && notifyPicker) activePicker.PickedColor = pickedColor;

            OnColorUpdated?.Invoke(color);
            Invalidate(Invalidation.SurfaceDirty);
        }

        private SKColor GetColor()
        {
            return pickedColor;
        }

        protected override void OnInteract()
        {
            base.OnInteract();

            OpenPicker();
        }

        private FPopupPanel? activePickerPanel;
        private FColorPicker? activePicker;

        public void OpenPicker()
        {
            // Actually, it's better to create it at the start and just re-use it. This will mean every color patch already has a picker, however since they're disabled anyway it shouldn't matter much
            // if (activePickerPanel != null) activePickerPanel.Close(() => CreatePicker());
            // else CreatePicker();

            if(activePickerPanel == null)
                CreatePicker();
            activePickerPanel?.ToggleShow(() => Transform.LocalToGlobal(Transform.LocalPosition.CachedValue));
        }

        private void CreatePicker()
        {
            activePickerPanel = new FPopupPanel(() => new(225, 150), false);
            activePickerPanel.CornerRadius.SetStaticState(30);

            activePicker = new FColorPicker(pickedColor);
            activePicker.Layout.StretchHorizontal.SetStaticState(true);
            activePicker.Layout.StretchVertical.SetStaticState(true);

            activePicker.Transform.LocalPosition.SetStaticState(new(7.5f, 0f));
            activePicker.Layout.MarginHorizontal.SetStaticState(50);
            activePicker.Layout.MarginVertical.SetStaticState(7.5f);

            activePicker.Layout.AlignmentAnchor.SetStaticState(new(0f, 0.5f));
            activePicker.Layout.Alignment.SetStaticState(new(0f, 0.5f));

            activePicker.OnColorUpdated += (x) => SetColor(x, false);
            activePicker.OnUserColorUpdated += (x) => OnUserColorUpdated?.Invoke(x);
            activePicker.SetParent(activePickerPanel);

            // Buttons

            const float btnSize = 15f;

            var copyBtn = new FImageButton(new FImage(() => Resources.GetImage("fenui-builtin-copy")), size: () => new(btnSize, btnSize));
            copyBtn.Layout.Alignment.SetStaticState(new(0, 0));
            copyBtn.Layout.AlignmentAnchor.SetStaticState(new(0, 0));
            copyBtn.Transform.LocalPosition.SetStaticState(new(activePicker.Layout.GetSize(activePicker.Transform.Size.CachedValue).x, 7.5f));
            copyBtn.RenderMaterial.Value = FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.TransparentInteractableMaterial;
            copyBtn.SetParent(activePickerPanel);

            var pasteBtn = new FImageButton(new FImage(() => Resources.GetImage("fenui-builtin-paste")), size: () => new(btnSize, btnSize));
            pasteBtn.Layout.Alignment.SetStaticState(new(0, 0));
            pasteBtn.Layout.AlignmentAnchor.SetStaticState(new(0, 0));
            pasteBtn.RenderMaterial.Value = FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.TransparentInteractableMaterial;
            pasteBtn.Transform.LocalPosition.SetStaticState(new(activePicker.Layout.GetSize(activePicker.Transform.Size.CachedValue).x + btnSize + 5, 7.5f));
            pasteBtn.SetParent(activePickerPanel);

            // Info text

            var text = new FText(TextModelFactory.CreateBasic(""), size: () => new(65, 0));
            // text.Layout.StretchHorizontal.SetStaticState(true);
            text.Layout.StretchVertical.SetStaticState(true);

            text.Transform.LocalPosition.SetResponsiveState(() => new(activePicker.Layout.GetSize(activePicker.Transform.Size.CachedValue).x, btnSize + 10));
            // text.Layout.MarginHorizontal.SetStaticState(75);
            text.Layout.MarginVertical.SetStaticState(7.5f);

            text.Layout.AlignmentAnchor.SetStaticState(new(0f, 0.5f));
            text.Layout.Alignment.SetStaticState(new(0f, 0.5f));

            text.Quality.SetStaticState(0.85f);
            text.SetParent(activePickerPanel);

            activePicker.OnColorUpdated += (x) =>
            {
                text.Model = TextModelFactory.CreateBasic(
                    "R: " + x.Red + "\n" +
                    "G: " + x.Green + "\n" +
                    "B: " + x.Blue + "\n" +
                    "\n" +
                    x
                , align: new() { HorizontalAlign = TextAlign.AlignType.Start }, textSize: 12);
            };
        }

        public SKShader GetColorPatchShader(SKColor color, SKRect rect)
        {
            string sksl = @"
                uniform float2 iResolution;
                uniform float2 iOff;
                uniform float2 iGlobOff;
                uniform float4 iColor;

                half4 lerp(half4 a, half4 b, half t) {
                    return a + (b - a) * t;
                }

                half4 main(float2 fragCoord) {
                    float2 uv = (fragCoord - iOff) / iResolution;

                    half4 col = half4(iColor.rgb, 1);

                    float2 coord = fragCoord - (iGlobOff);
                    
                    float checker = mod(floor(coord.x / 5) + floor(coord.y / 5), 2.0);
                    checker = clamp(checker, 0.6, 0.9);

                    half4 colAlpha = lerp(half4(checker, checker, checker, 1), col, iColor.a);
                    if((uv.x > 0.5)) col = colAlpha;

                    return col;
                }
            ";

            SKRuntimeEffect effect = SKRuntimeEffect.CreateShader(sksl, out var err);
            if (effect == null) Console.WriteLine($"Shader compilation failed: {err}");

            var uniforms = new SKRuntimeEffectUniforms(effect);
            uniforms["iResolution"] = new float[] { rect.Width, rect.Height };
            uniforms["iOff"] = new float[] { rect.Left, rect.Top };
            uniforms["iGlobOff"] = new float[] {
                Transform.DrawLocalToGlobal(new Vector2(0, 0)).x,
                Transform.DrawLocalToGlobal(new Vector2(0, 0)).y
             };
            uniforms["iColor"] = new float[] {
                ((float)color.Red) / 255f,
                ((float)color.Green) / 255f,
                ((float)color.Blue) / 255f,
                ((float)color.Alpha) / 255f
             };

            return effect?.ToShader(uniforms) ?? SKShader.CreateEmpty();
        }

        public override void Render(SKCanvas canvas)
        {
            base.Render(canvas);

            using var paint = GetRenderPaint();

            var patchRect1 = Shape.LocalBounds;
            patchRect1.Inflate(-5f, -5f);
            using var patchPath1 = SKSquircle.CreateSquircle(patchRect1, CornerRadius.CachedValue / 2);

            using var shader = GetColorPatchShader(pickedColor, patchRect1);
            paint.Shader = shader;
            paint.Color = SKColors.White;
            canvas.DrawPath(patchPath1, paint);
        }
    }
}