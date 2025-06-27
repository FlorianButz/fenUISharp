using FenUISharp.Behavior;
using FenUISharp.Mathematics;
using FenUISharp.Objects.Buttons;
using FenUISharp.States;
using SkiaSharp;

namespace FenUISharp.Objects
{
    public class FToggle : SelectableButton, IStateListener
    {
        public State<SKColor> BackgroundColor { get; set; }
        public State<SKColor> BorderColor { get; set; }
        public State<SKColor> EnabledFillColor { get; set; }
        public State<SKColor> CheckColor { get; set; }

        private State<SKColor> highlight;

        public float CornerRadius { get; set; } = 5;

        private SKColor currenthighlight;
        private SKColor currentbackground;

        private AnimatorComponent toggleAnimator;

        public FToggle() : base(position: () => new(0, 0), size: () => new(20, 20))
        {
            BackgroundColor = new(() => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.Secondary, this);
            BorderColor = new(() => BackgroundColor.CachedValue.AddMix(new(25, 25, 25)), this);
            CheckColor = new(() => SKColors.White, this);
            EnabledFillColor = new(() => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.Primary, this);

            highlight = new(() => BackgroundColor.CachedValue.AddMix(new(65, 65, 65)), this);
            currenthighlight = BackgroundColor.CachedValue.AddMix(new(65, 65, 65));

            Padding.SetStaticState(2);

            Transform.Size.SetStaticState(new(20, 20));
            Transform.LocalPosition.SetResponsiveState((() => new(0, 0)));

            toggleAnimator = new(this, Easing.EaseOutCubic, Easing.EaseOutCubic);
            toggleAnimator.Duration = 0.2f;
            toggleAnimator.OnValueUpdate += (t) =>
            {
                var hoveredMix = RMath.Lerp((!IsSelected) ? BackgroundColor.CachedValue : EnabledFillColor.CachedValue,
                    FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.HoveredMix, 0.2f);
                var hoveredHigh = RMath.Lerp(highlight.CachedValue, FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.HoveredMix, 0.2f);

                currentbackground = RMath.Lerp((!IsSelected) ? BackgroundColor.CachedValue : EnabledFillColor.CachedValue, hoveredMix, t);
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

            Transform.SnapPositionToPixelGrid.SetStaticState(true);

            // Creating checkmark

            FImage image = new(() => Resources.GetImage("fenui-builtin-check"));
            image.Transform.Size.SetResponsiveState(() => new(Transform.Size.CachedValue.x - 4, Transform.Size.CachedValue.y - 4));
            image.Transform.LocalPosition.SetStaticState(new(0.5f, 1f));
            image.Enabled.SetResponsiveState(() => IsSelected);
            image.SetParent(this);

            UpdateColors();
        }

        public override void Dispose()
        {
            base.Dispose();

            BorderColor.Dispose();
            BackgroundColor.Dispose();
            CheckColor.Dispose();
            EnabledFillColor.Dispose();

            highlight.Dispose();
        }

        void UpdateColors()
        {
            if (toggleAnimator.IsRunning) return;
            var baseCol = IsSelected ? EnabledFillColor.CachedValue : BackgroundColor.CachedValue;

            var hoveredMix = RMath.Lerp(baseCol, FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.HoveredMix, 0.2f);
            var hoveredHigh = RMath.Lerp(highlight.CachedValue, FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.HoveredMix, 0.2f);

            currentbackground = RMath.Lerp(baseCol, hoveredMix, InteractiveSurface.IsMouseHovering ? 1 : 0);
            currenthighlight = RMath.Lerp(highlight.CachedValue, hoveredHigh, InteractiveSurface.IsMouseHovering ? 1 : 0);
        }

        public override void OnInternalStateChanged<T>(T value)
        {
            base.OnInternalStateChanged(value);

            UpdateColors();
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
                toggleAnimator.Inverse = false;
                toggleAnimator.Restart();
            }
        }

        public override void Render(SKCanvas canvas)
        {
            base.Render(canvas);

            canvas.Translate(0.5f, 0.5f);

            using var paint = GetRenderPaint();
            var bounds = Shape.LocalBounds;
            using var backgroundPath = SKSquircle.CreateSquircle(Shape.LocalBounds, CornerRadius);

            using var shadow = SKImageFilter.CreateDropShadow(0, 2, 5, 5, FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.Shadow);

            paint.Color = currentbackground;
            canvas.DrawPath(backgroundPath, paint);
            canvas.ClipPath(backgroundPath, antialias: true);

            paint.Color = BorderColor.CachedValue;
            paint.IsStroke = true;
            paint.StrokeWidth = 1;
            canvas.DrawPath(backgroundPath, paint);
            canvas.Translate(-0.5f, -0.5f);

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
                canvas.DrawPath(backgroundPath, highlightPaint);
            }
        }
    }
}