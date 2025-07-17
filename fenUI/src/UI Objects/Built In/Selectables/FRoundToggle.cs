using FenUISharp.Behavior;
using FenUISharp.Mathematics;
using FenUISharp.States;
using SkiaSharp;

namespace FenUISharp.Objects.Buttons
{
    public class FRoundToggle : SelectableButton, IStateListener
    {
        public State<SKColor> KnobColor { get; set; }

        public Spring AnimationSpring { get; set; }

        const int WIDTH = 50;
        const int HEIGHT = 25;

        protected AnimatorComponent toggleAnimator;

        public FRoundToggle(Func<Vector2>? position = null) : base(null, null, position, () => new(WIDTH, HEIGHT))
        {
            KnobColor = new(() => SKColors.White, this);

            toggleAnimator = new(this, Easing.EaseOutBack);
            toggleAnimator.Duration = 0.5f;
            toggleAnimator.OnValueUpdate += AnimatorValueUpdate;

            Padding.SetStaticState(5);
            AnimationSpring = new(2f, 1.75f);

            Transform.Size.SetStaticState(new(WIDTH, HEIGHT));
            Transform.LocalPosition.SetResponsiveState(position ?? (() => new(0, 0)));

            OnSelectionChangedSilent += (x, y) => AnimationSpring.ResetVector(new(x ? 1 : 0, 0));
            OnSelectionChanged += (x, y) => toggleAnimator.Restart();
            OnUserSelectionChanged += (x, y) => toggleAnimator.Restart();

            InteractiveSurface.OnMouseExit += MouseExit;
            Transform.SnapPositionToPixelGrid.SetStaticState(true); // Technically good

            CornerRadius.SetResponsiveState(() => Layout.ClampSize(Transform.Size.CachedValue).y / 2);
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

            if (_lastAnimTime != _animTime)
                Invalidate(Invalidation.SurfaceDirty);

            _lastAnimTime = _animTime;
            _lastWidth = _width;
        }

        protected override void MouseExit()
        {
            base.MouseExit();
            InteractiveSurface.IsMouseDown = false;
        }

        public override void Render(SKCanvas canvas)
        {
            // base.Render(canvas);

            using var paint = GetRenderPaint();
            var bounds = Shape.LocalBounds;

            // Base background
            var colorBefore = RenderMaterial.CachedValue.GetProp<Func<SKColor>>("BaseColor", null);
            var colorBorderBefore = RenderMaterial.CachedValue.GetProp<Func<SKColor>>("BorderColor", null);

            float t = RMath.Clamp(1f - _animTime, 0, 1);
            SKColor currentBackground = RMath.Lerp(EnabledFillColor.CachedValue, (colorBefore?.Invoke() ?? FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.Secondary), t);
            SKColor currentBorder = RMath.Lerp(EnabledFillColor.CachedValue.AddMix(new(25, 25, 25)), (colorBorderBefore?.Invoke() ?? FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.SecondaryBorder), t);

            using var backgroundRect = new SKRoundRect(bounds, CornerRadius.CachedValue);
            RenderMaterial.CachedValue.WithOverride(new (){
                ["BaseColor"] = () => currentBackground,
                ["BorderColor"] = () => currentBorder
            }).DrawWithMaterial(canvas, backgroundRect, this, paint);

            // Knob
            float knobLeft = RMath.Lerp(bounds.Left, bounds.Right - _width, _animTime);
            float knobRight = RMath.Lerp(bounds.Left + _width, bounds.Right, _animTime);

            var knobRect = new SKRect(knobLeft, bounds.Top, knobRight, bounds.Bottom);
            knobRect.Inflate(-2, -2);
            using var knobRectRound = new SKRoundRect(knobRect, 20);

            RenderMaterial.CachedValue.WithOverride(new (){
                ["BaseColor"] = () => KnobColor.CachedValue,
                ["BorderColor"] = () => SKColors.Transparent
            }).DrawWithMaterial(canvas, knobRectRound, this, paint);
        }


        public override void AfterRender(SKCanvas canvas)
        {
            using (var rect = new SKRoundRect(Shape.LocalBounds, CornerRadius.CachedValue))
            {
                using var paint = GetRenderPaint();
                paint.Color = currentHoverMix;
                canvas.DrawRoundRect(rect, paint);
            }
        }
    }
}