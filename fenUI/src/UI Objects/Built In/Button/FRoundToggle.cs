using FenUISharp.Behavior;
using FenUISharp.Mathematics;
using FenUISharp.States;
using SkiaSharp;

namespace FenUISharp.Objects.Buttons
{
    public class FRoundToggle : SelectableButton, IStateListener
    {
        public State<SKColor> BackgroundColor { get; set; }
        public State<SKColor> EnabledFillColor { get; set; }
        public State<SKColor> KnobColor { get; set; }
        public State<SKColor> BorderColor { get; set; }

        protected SKColor currentBackground;
        public Spring AnimationSpring { get; set; }

        const int WIDTH = 50;
        const int HEIGHT = 25;

        protected AnimatorComponent toggleAnimator;

        // TODO: For some reason not centered.

        public FRoundToggle(Func<Vector2>? position = null) : base(null, null, position, () => new(WIDTH, HEIGHT))
        {
            BackgroundColor = new(() => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.SurfaceVariant, this);
            KnobColor = new(() => SKColors.White, this);
            EnabledFillColor = new(() => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.Primary, this);
            BorderColor = new(() => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.SecondaryBorder, this);

            toggleAnimator = new(this, Easing.EaseOutBack);
            toggleAnimator.Duration = 0.5f;
            toggleAnimator.OnValueUpdate += AnimatorValueUpdate;

            Padding.SetStaticState(5);
            AnimationSpring = new(2f, 1.75f);

            Transform.Size.SetStaticState(new(WIDTH, HEIGHT));
            Transform.LocalPosition.SetResponsiveState(position ?? (() => new(0, 0)));

            OnSelectionChangedSilent += (x) => AnimationSpring.ResetVector(new(x ? 1 : 0, 0));
            OnSelectionChanged += (x) => toggleAnimator.Restart();
            OnUserSelectionChanged += (x) => toggleAnimator.Restart();

            InteractiveSurface.OnMouseExit += MouseExit;
            Transform.SnapPositionToPixelGrid.SetStaticState(true); // Technically good
        }

        float _width = HEIGHT;
        float _lastWidth = HEIGHT;

        float _animTime = 0;
        float _lastAnimTime = 0;

        void AnimatorValueUpdate(float t)
        {
            UpdateColors();
            Invalidate(Invalidation.SurfaceDirty);
        }

        protected override void Update()
        {
            base.Update();

            float uT = IsSelected ? 1 : 0;

            var t = AnimationSpring.Update(FContext.DeltaTime, new(uT, 0));
            _animTime = (float)(Math.Round(t.x * 100) / 100);

            _width = RMath.Lerp(_width, InteractiveSurface.IsMouseDown ? WIDTH / 2f + 5 : WIDTH / 2f, FContext.DeltaTime * 5f);

            if (_lastAnimTime != _animTime || Math.Round(_width) != Math.Round(_lastWidth) /*|| _lastSize != glassKnob.Transform.Size*/)
            {
                Invalidate(Invalidation.SurfaceDirty);
            }
            _lastAnimTime = _animTime;
            _lastWidth = _width;
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

        protected override void MouseAction(MouseInputCode inputCode)
        {
            base.MouseAction(inputCode);
        }

        protected void MouseExit()
        {
            InteractiveSurface.IsMouseDown = false;
        }

        public override void Render(SKCanvas canvas)
        {
            base.Render(canvas);

            UpdateColors();
            canvas.Translate(0.5f, 0.5f);

            using var paint = GetRenderPaint();
            var bounds = Shape.LocalBounds;
            using var backgroundRect = new SKRoundRect(bounds, 50);

            float knobLeft = RMath.Lerp(bounds.Left, bounds.Right - _width, _animTime);
            float knobRight = RMath.Lerp(bounds.Left + _width, bounds.Right, _animTime);

            var knobRect = new SKRect(knobLeft, bounds.Top, knobRight, bounds.Bottom);

            knobRect.Inflate(-2, -2);
            using var knobRectRound = new SKRoundRect(knobRect, 20);

            using var shadow = SKImageFilter.CreateDropShadow(0, 2, 5, 5, FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.Shadow);

            paint.Color = currentBackground;
            canvas.DrawRoundRect(backgroundRect, paint);
            canvas.ClipRoundRect(backgroundRect, antialias: true);

            paint.ImageFilter = shadow;
            paint.Color = KnobColor.CachedValue;
            canvas.DrawRoundRect(knobRectRound, paint);
            paint.ImageFilter = null;

            paint.Color = BorderColor.CachedValue;
            paint.IsStroke = true;
            paint.StrokeWidth = 1;
            canvas.DrawRoundRect(backgroundRect, paint);
            canvas.Translate(-0.5f, -0.5f);
        }
    }
}