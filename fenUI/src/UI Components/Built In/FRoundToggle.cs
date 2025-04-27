using FenUISharp.Mathematics;
using FenUISharp.Themes;
using SkiaSharp;

namespace FenUISharp.Components
{
    public class FRoundToggle : UIComponent
    {
        public bool IsOn { get; set; } = false;

        public ThemeColor BackgroundColor { get; set; }
        public ThemeColor EnabledFillColor { get; set; }
        public ThemeColor KnobColor { get; set; }
        public ThemeColor BorderColor { get; set; }

        protected SKColor currentBackground;
        public Spring AnimationSpring { get; set; }

        const int WIDTH = 50;
        const int HEIGHT = 30;

        protected AnimatorComponent toggleAnimator;

        public Action<bool>? OnStateChanged { get; set; }

        public FRoundToggle(Window rootWindow, Vector2 position) : base(rootWindow, position, new(WIDTH, HEIGHT))
        {
            BackgroundColor = rootWindow.WindowThemeManager.GetColor(t => t.SurfaceVariant);
            KnobColor = new(SKColors.White);
            EnabledFillColor = rootWindow.WindowThemeManager.GetColor(t => t.Primary);
            BorderColor = rootWindow.WindowThemeManager.GetColor(t => t.SecondaryBorder);

            toggleAnimator = new(this, Easing.EaseOutBack);
            toggleAnimator.Duration = 0.5f;
            toggleAnimator.onValueUpdate += AnimatorValueUpdate;

            Transform.BoundsPadding.SetValue(this, 5, 25);
            AnimationSpring = new(2f, 1.75f);
        }

        float _width = HEIGHT;
        float _lastWidth = HEIGHT;

        bool _isMouseDown = false;

        float _animTime = 0;
        float _lastAnimTime = 0;
        void AnimatorValueUpdate(float t)
        {
            UpdateColors();
            MarkInvalidated();
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            float uT = IsOn ? 1 : 0;

            var t = AnimationSpring.Update((float)WindowRoot.DeltaTime, new(uT, 0));
            _animTime = (float)(Math.Round(t.x * 100) / 100);

            _width = RMath.Lerp(_width, _isMouseDown ? HEIGHT + 5 : HEIGHT, (float)WindowRoot.DeltaTime * 5f);
            _width = (float)(Math.Round(_width * 10) / 10);

            if (_lastAnimTime != _animTime || _width != _lastWidth) Invalidate();
            _lastAnimTime = _animTime;
            _lastWidth = _width;
        }

        void UpdateColors()
        {
            if (toggleAnimator.IsRunning)
            {
                float t = toggleAnimator.Time;
                if (!IsOn) t = 1 - t;
                t = Math.Clamp(t, 0, 1);

                currentBackground = RMath.Lerp(BackgroundColor.Value, EnabledFillColor.Value, t);
            }
            else
            {
                currentBackground = RMath.Lerp(BackgroundColor.Value, EnabledFillColor.Value, IsOn ? 1 : 0);
            }
        }

        protected override void MouseAction(MouseInputCode inputCode)
        {
            base.MouseAction(inputCode);

            if (inputCode.button == (int)MouseInputButton.Left && inputCode.state == (int)MouseInputState.Up)
            {
                IsOn = !IsOn;
                toggleAnimator.Restart();

                _isMouseDown = false;

                OnStateChanged?.Invoke(IsOn);
            }
            else if (inputCode.button == (int)MouseInputButton.Left && inputCode.state == (int)MouseInputState.Down)
                _isMouseDown = true;
        }

        protected override void MouseExit()
        {
            base.MouseExit();
            _isMouseDown = false;
        }

        protected override void DrawToSurface(SKCanvas canvas)
        {
            UpdateColors();
            canvas.Translate(0.5f, 0.5f);

            using var paint = SkPaint.Clone();
            var bounds = Transform.LocalBounds;
            using var backgroundRect = new SKRoundRect(bounds, 50);

            float knobLeft = RMath.Lerp(bounds.Left, bounds.Right - _width, _animTime);
            float knobRight = RMath.Lerp(bounds.Left + _width, bounds.Right, _animTime);

            var knobRect = new SKRect(knobLeft, bounds.Top, knobRight, bounds.Bottom);

            knobRect.Inflate(-2, -2);
            using var knobRectRound = new SKRoundRect(knobRect, 20);
            using var shadow = SKImageFilter.CreateDropShadow(0, 2, 5, 5, WindowRoot.WindowThemeManager.GetColor(t => t.Shadow).Value);

            paint.Color = currentBackground;
            canvas.DrawRoundRect(backgroundRect, paint);
            canvas.ClipRoundRect(backgroundRect, antialias: true);

            paint.ImageFilter = shadow;
            paint.Color = KnobColor.Value;
            canvas.DrawRoundRect(knobRectRound, paint);
            paint.ImageFilter = null;

            paint.Color = BorderColor.Value;
            paint.IsStroke = true;
            paint.StrokeWidth = 1;
            canvas.DrawRoundRect(backgroundRect, paint);
            canvas.Translate(-0.5f, -0.5f);
        }
    }
}