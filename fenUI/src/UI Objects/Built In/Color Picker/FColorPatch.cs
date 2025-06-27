using FenUISharp.Behavior;
using FenUISharp.Mathematics;
using FenUISharp.Objects.Buttons;
using FenUISharp.States;
using SkiaSharp;

namespace FenUISharp.Objects
{
    public class FColorPatch : Button, IStateListener
    {
        private SKColor pickedColor;
        public SKColor PickedColor { get => GetColor(); set => SetColor(value); }
        public Action<SKColor>? OnColorUpdated { get; set; }
        public Action<SKColor>? OnUserColorUpdated { get; set; }

        public State<SKColor> BackgroundColor { get; init; }
        public State<SKColor> BorderColor { get; init; }

        private State<SKColor> highlight;

        public float CornerRadius { get; set; } = 10f;

        private SKColor currenthighlight;
        private SKColor currentbackground;

        private AnimatorComponent toggleAnimator;

        public FColorPatch(SKColor? initialColor = null, Func<Vector2>? position = null, Func<Vector2>? size = null) : base(position: position, size: size ?? (() => new(50, 25)))
        {
            PickedColor = initialColor ?? new(255, 255, 255, 0);

            BackgroundColor = new(() => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.Secondary, this);
            BorderColor = new(() => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.SecondaryBorder, this);

            highlight = new(() => BackgroundColor.CachedValue.AddMix(new(65, 65, 65)), this);
            currenthighlight = BackgroundColor.CachedValue.AddMix(new(65, 65, 65));

            Transform.SnapPositionToPixelGrid.SetStaticState(true);

            toggleAnimator = new(this, Easing.EaseOutCubic, Easing.EaseOutCubic);
            toggleAnimator.Duration = 0.2f;
            toggleAnimator.OnValueUpdate += (t) =>
            {
                var hoveredMix = RMath.Lerp(BackgroundColor.CachedValue,
                    FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.HoveredMix, 0.2f);
                var hoveredHigh = RMath.Lerp(highlight.CachedValue, FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.HoveredMix, 0.2f);

                currentbackground = RMath.Lerp(BackgroundColor.CachedValue, hoveredMix, t);
                currenthighlight = RMath.Lerp(highlight.CachedValue, hoveredHigh, t);

                float pixelsAdd = 0.75f;
                float sx = (Transform.Size.CachedValue.x + pixelsAdd) / Transform.Size.CachedValue.x;
                float sy = (Transform.Size.CachedValue.y + pixelsAdd / 2) / Transform.Size.CachedValue.y;

                Transform.Scale.SetStaticState(Vector2.Lerp(new Vector2(1, 1), new Vector2(sx, sy), t));
                Invalidate(Invalidation.SurfaceDirty);
            };

            InteractiveSurface.OnMouseEnter += () =>
            {
                toggleAnimator.Inverse = false;
                toggleAnimator.Restart();
            };

            InteractiveSurface.OnMouseExit += () =>
            {
                toggleAnimator.Inverse = true;
                toggleAnimator.Restart();
            };

            UpdateColors();
        }

        public override void OnInternalStateChanged<T>(T value)
        {
            base.OnInternalStateChanged(value);
            UpdateColors();
        }

        public override void Dispose()
        {
            base.Dispose();

            BackgroundColor.Dispose();
            BorderColor.Dispose();
            highlight.Dispose();
        }

        
        void UpdateColors()
        {
            if (toggleAnimator.IsRunning) return;
            var baseCol = BackgroundColor.CachedValue;

            var hoveredMix = RMath.Lerp(baseCol, FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.HoveredMix, 0.2f);
            var hoveredHigh = RMath.Lerp(highlight.CachedValue, FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.HoveredMix, 0.2f);

            currentbackground = RMath.Lerp(baseCol, hoveredMix, InteractiveSurface.IsMouseHovering ? 1 : 0);
            currenthighlight = RMath.Lerp(highlight.CachedValue, hoveredHigh, InteractiveSurface.IsMouseHovering ? 1 : 0);
        }

        protected override void MouseAction(MouseInputCode inputCode)
        {
            base.MouseAction(inputCode);

            if (inputCode.button == MouseInputButton.Left && inputCode.state == MouseInputState.Down)
            {
                toggleAnimator.Inverse = true;
                toggleAnimator.Restart();
            }
            else if (inputCode.button == MouseInputButton.Left && inputCode.state == MouseInputState.Up)
            {
                OpenPicker();

                toggleAnimator.Inverse = false;
                toggleAnimator.Restart();
            }
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

        private FPopupPanel? activePickerPanel;
        private FColorPicker? activePicker;

        public void OpenPicker()
        {
            if (activePickerPanel != null) activePickerPanel.Close(() => CreatePicker());
            else CreatePicker();
        }

        private void CreatePicker()
        {
            activePickerPanel = new FPopupPanel(() => new(225, 150), false);
            activePickerPanel.CornerRadius.SetStaticState(20);

            activePicker = new FColorPicker(pickedColor);
            activePicker.Layout.StretchHorizontal.SetStaticState(true);
            activePicker.Layout.StretchVertical.SetStaticState(true);

            activePicker.Transform.LocalPosition.SetStaticState(new(7.5f, 0f));
            activePicker.Layout.MarginHorizontal.SetStaticState(50);
            activePicker.Layout.MarginVertical.SetStaticState(7.5f);

            activePicker.Layout.AlignmentAnchor.SetResponsiveState(() => new(0f, 0.5f));
            activePicker.Layout.Alignment.SetResponsiveState(() => new(0f, 0.5f));

            activePicker.OnColorUpdated += (x) => SetColor(x, false);
            activePicker.SetParent(activePickerPanel);

            activePickerPanel.Show(() => Transform.LocalToGlobal(Transform.LocalPosition.CachedValue));
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

                    half4 col = half4(iColor.r, iColor.g, iColor.b, 1);

                    float2 coord = fragCoord - (iGlobOff);
                    
                    float checker = mod(floor(coord.x / 5) + floor(coord.y / 5), 2.0);
                    checker = clamp(checker, 0.6, 0.9);

                    half4 colAlpha = lerp(half4(checker, checker, checker, 1), col, col.a);
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

            canvas.Translate(0.5f, 0.5f);

            using var paint = GetRenderPaint();
            using var baseRect = SKSquircle.CreateSquircle(Shape.LocalBounds, CornerRadius);

            var patchRect1 = Shape.LocalBounds;
            patchRect1.Inflate(-5f, -5f);
            using var patchPath1 = SKSquircle.CreateSquircle(patchRect1, CornerRadius / 2);

            using var shadow = SKImageFilter.CreateDropShadow(0, 0, 5, 5, FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.Shadow);
            paint.ImageFilter = shadow;

            paint.Color = currentbackground;
            canvas.DrawPath(baseRect, paint);

            paint.ImageFilter = null;

            paint.Color = BorderColor.CachedValue;
            paint.IsStroke = true;
            paint.StrokeWidth = 1;
            canvas.DrawPath(baseRect, paint);

            paint.IsStroke = false;

            using var shader = GetColorPatchShader(pickedColor, patchRect1);
            paint.Shader = shader;
            paint.Color = SKColors.White;
            canvas.DrawPath(patchPath1, paint);

            paint.Shader = null;

            // Highlight on Top Edge
            using (var highlightPaint = GetRenderPaint())
            {
                highlightPaint.IsAntialias = true;
                highlightPaint.Shader = SKShader.CreateLinearGradient(
                    new SKPoint(Shape.LocalBounds.Left, Shape.LocalBounds.Top),
                    new SKPoint(Shape.LocalBounds.Left, Shape.LocalBounds.Top + 4f),
                    new SKColor[] { currenthighlight, SKColors.Transparent },
                    new float[] { 0.0f, 0.4f },
                    SKShaderTileMode.Clamp
                );
                // canvas.DrawRoundRect(roundRect, paint);
                canvas.DrawPath(baseRect, highlightPaint);
            }
        }
    }
}