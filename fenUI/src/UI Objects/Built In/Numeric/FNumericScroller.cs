using System.Globalization;
using FenUISharp.Mathematics;
using FenUISharp.Objects.Buttons;
using FenUISharp.Objects.Text;
using FenUISharp.Objects.Text.Model;
using FenUISharp.States;
using SkiaSharp;

namespace FenUISharp.Objects
{
    public class FNumericScroller : Button, IStateListener
    {
        private float _value = 0f;
        public float Value { get => GetValue(); set => SetValue(value); }

        public State<float> MinValue { get; init; } 
        public State<float> MaxValue { get; init; }
        public State<float> Step { get; init; }

        public State<string> Suffix { get; set; }

        /// <summary>
        /// A format provider which will be passed in to the ToString method. See available formats on: https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings
        /// </summary>
        public State<string> FormatProvider { get; init; }
        public State<CultureInfo> Culture { get; init; }

        public FText Label { get; protected set; }

        public State<SKColor> HoverColor { get; init; }
        private SKColor currentBackground;

        public Action<float>? OnValueChanged { get; set; }
        public Action<float>? OnUserValueChanged { get; set; }

        public FNumericScroller(FText label, Func<Vector2>? position = null, Func<Vector2>? size = null) : base(position: position, size: size ?? (() => new(30, 25)))
        {
            Label = label;
            label.SetParent(this);

            HoverColor = new(() => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.Secondary.MultiplyMix(new(200, 200, 200)).WithAlpha(65), this);
            currentBackground = SKColors.Transparent;

            label.Layout.StretchHorizontal.SetStaticState(true);
            label.Layout.StretchVertical.SetStaticState(true);

            MinValue = new(() => 0f, this);
            MaxValue = new(() => 1f, this);
            Step = new(() => 0.1f, this);

            Suffix = new(() => "", this);
            FormatProvider = new(() => "", this);
            Culture = new(() => System.Globalization.CultureInfo.CurrentCulture, this);

            UpdateText();
        }

        public override void OnInternalStateChanged<T>(T value)
        {
            base.OnInternalStateChanged(value);

            UpdateText();
        }

        public override void Dispose()
        {
            base.Dispose();

            MinValue.Dispose();
            MaxValue.Dispose();
            Step.Dispose();
            Suffix.Dispose();
            FormatProvider.Dispose();
            Culture.Dispose();
        }

        private float GetValue()
        {
            return RMath.Clamp(_value, MinValue.CachedValue, MaxValue.CachedValue);
        }

        private void SetValue(float value)
        {
            _value = RMath.Clamp(value, MinValue.CachedValue, MaxValue.CachedValue);
            UpdateText();

            OnValueChanged?.Invoke(Value);
        }

        void UpdateText()
        {
            string formatted = Value.ToString(FormatProvider.CachedValue, Culture.CachedValue);
            Label.Model = TextModelFactory.CopyBasicNew(formatted + Suffix.CachedValue, Label.Model);

            var cage = Shape.LocalBounds;
            cage.Inflate(50, 25);

            var bounds = Label.LayoutModel.GetBoundingRect(Label.Model, cage);
            Transform.Size.SetStaticState(new(bounds.Width + 6 * (bounds.Width / 30), bounds.Height + 4 * (bounds.Width / 50)));

            FContext.GetCurrentDispatcher().InvokeLater(() => Label.Invalidate(Invalidation.SurfaceDirty | Invalidation.LayoutDirty), 1L);
            Invalidate(Invalidation.SurfaceDirty | Invalidation.LayoutDirty);
        }

        protected override void Update()
        {
            base.Update();

            var lastCol = currentBackground;

            currentBackground = RMath.Lerp(currentBackground,
                InteractiveSurface.IsMouseDown ? HoverColor.CachedValue : (InteractiveSurface.IsMouseHovering ? HoverColor.CachedValue : SKColors.Transparent),
            FContext.DeltaTime * 10f);

            if (lastCol != currentBackground)
                Invalidate(Invalidation.SurfaceDirty);
        }

        protected override void MouseAction(MouseInputCode inputCode)
        {
            base.MouseAction(inputCode);

            if (inputCode.button == MouseInputButton.Left && inputCode.state == MouseInputState.Up)
            {
                OpenPopup();
            }
        }

        private FPopupPanel? activePopup;

        public void OpenPopup()
        {
            if (activePopup != null) activePopup.Close(() => CreatePopup());
            else CreatePopup();
        }

        private void CreatePopup()
        {
            activePopup = new FPopupPanel(() => new(70, 30), false);
            activePopup.CornerRadius.SetStaticState(30);
            activePopup.DistanceToTarget = (int)(Transform.Size.CachedValue.y / 2 - 5);

            activePopup.InteractiveSurface.EnableMouseScrolling.SetStaticState(true);
            activePopup.InteractiveSurface.OnMouseScroll += OnPopupScroll;

            new NumericScrollerPopupScrollDisplay(() => Value, () => Step.CachedValue, Decrement, Increment).SetParent(activePopup);

            activePopup.OnObjectDisposed += () =>
            {
                activePopup.InteractiveSurface.OnMouseScroll -= OnPopupScroll;
            };

            activePopup.Show(() => Transform.LocalToGlobal(Transform.LocalPosition.CachedValue));
        }

        void Increment()
        {
            Value += Step.CachedValue;

            OnValueChanged?.Invoke(Value);
            OnUserValueChanged?.Invoke(Value);
        }

        void Decrement()
        {
            Value -= Step.CachedValue;

            OnValueChanged?.Invoke(Value);
            OnUserValueChanged?.Invoke(Value);
        }

        void OnPopupScroll(float x)
        {
            if (MathF.Abs(x) < 120) return;
            x = RMath.Clamp(x, -1, 1);

            Value += Step.CachedValue * x;

            OnValueChanged?.Invoke(Value);
            OnUserValueChanged?.Invoke(Value);
        }

        public override void Render(SKCanvas canvas)
        {
            base.Render(canvas);

            using var panel = SKSquircle.CreateSquircle(Shape.LocalBounds, Shape.LocalBounds.Height / 1.25f);
            using var paint = GetRenderPaint();

            paint.Color = currentBackground;
            canvas.DrawPath(panel, paint);
        }
    }
}