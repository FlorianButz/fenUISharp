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

        public float CornerRadius { get; set; } = 5;
        
        private SKColor currentBackground;

        private AnimatorComponent toggleAnimator;

        public FToggle() : base(position: () => new(0, 0), size: () => new(20, 20))
        {
            BackgroundColor = new(() => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.SurfaceVariant, this);
            BorderColor = new(() => BackgroundColor.CachedValue.AddMix(new(25, 25, 25)), this);
            CheckColor = new(() => SKColors.White, this);
            EnabledFillColor = new(() => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.Primary, this);

            Padding.SetStaticState(2);

            Transform.Size.SetStaticState(new(20, 20));
            Transform.LocalPosition.SetResponsiveState((() => new(0, 0)));

            toggleAnimator = new(this, Easing.EaseOutCubic, Easing.EaseOutCubic);
            toggleAnimator.Duration = 0.2f;
            toggleAnimator.OnValueUpdate += (t) =>
            {
                var hoveredMix = RMath.Lerp((!IsSelected) ? BackgroundColor.CachedValue : EnabledFillColor.CachedValue,
                    FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.HoveredMix, 0.2f);

                currentBackground = RMath.Lerp((!IsSelected) ? BackgroundColor.CachedValue : EnabledFillColor.CachedValue, hoveredMix, t);

                float pixelsAdd = 0.75f;
                float sx = (Transform.Size.CachedValue.x + pixelsAdd) / Transform.Size.CachedValue.x;
                float sy = (Transform.Size.CachedValue.y + pixelsAdd / 2) / Transform.Size.CachedValue.y;

                Transform.Scale.SetStaticState(Vector2.Lerp(new Vector2(1, 1), new Vector2(sx, sy), t));
                Invalidate(Invalidation.SurfaceDirty);
            };

            InteractiveSurface.OnMouseEnter += () => {
                toggleAnimator.Inverse = false;
                toggleAnimator.Restart();
            };
            
            InteractiveSurface.OnMouseExit += () => {
                toggleAnimator.Inverse = true;
                toggleAnimator.Restart();
            };
        }

        public override void Dispose()
        {
            base.Dispose();

            BorderColor.Dispose();
            BackgroundColor.Dispose();
            CheckColor.Dispose();
            EnabledFillColor.Dispose();
        }

        void UpdateColors()
        {
            if (toggleAnimator.IsRunning)
            {
                float t = toggleAnimator.Time;
                if (!IsSelected) t = 1 - t;
                t = Math.Clamp(t, 0, 1);

                currentBackground = RMath.Lerp(BackgroundColor.CachedValue, EnabledFillColor.CachedValue, t);
            }
            else
            {
                currentBackground = RMath.Lerp(BackgroundColor.CachedValue, EnabledFillColor.CachedValue, IsSelected ? 1 : 0);
            }
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
            using var backgroundRect = new SKRoundRect(bounds, CornerRadius);

            using var shadow = SKImageFilter.CreateDropShadow(0, 2, 5, 5, FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.Shadow);

            paint.Color = currentBackground;
            canvas.DrawRoundRect(backgroundRect, paint);
            canvas.ClipRoundRect(backgroundRect, antialias: true);

            paint.Color = BorderColor.CachedValue;
            paint.IsStroke = true;
            paint.StrokeWidth = 1;
            canvas.DrawRoundRect(backgroundRect, paint);
            canvas.Translate(-0.5f, -0.5f);
        }
    }
}