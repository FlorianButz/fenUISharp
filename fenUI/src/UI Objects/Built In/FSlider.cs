using FenUISharp.Mathematics;
using FenUISharp.States;
using FenUISharp.Themes;
using SkiaSharp;

namespace FenUISharp.Objects
{
    public class FSlider : UIObject, IStateListener
    {
        protected float _value = 0f;
        public float Value
        {
            get => GetValue();
            set
            {
                var lastValue = GetValue();
                var val = GetValue(RMath.Remap(value, MinValue.CachedValue, MaxValue.CachedValue, 0, 1));
                if (lastValue != val)
                {
                    _value = val;

                    OnValueChanged?.Invoke(value);
                    Invalidate(Invalidation.SurfaceDirty);
                }
            }
        }

        public State<float> MaxValue { get; init; }
        public State<float> MinValue { get; init; }

        private List<float>? _snappingHotspots;

        private float _snappingInterval = 0;
        public float SnappingInterval { get => _snappingInterval; set { _snappingInterval = value; UpdateHostpots(); } }
        public int MaxSnapIndicators { get; set; } = 50;

        public List<float> ExtraHotspots { get; set; } = new();

        public State<SKColor> BarFill { get; init; }
        public State<SKColor> Bar { get; init; }
        public State<SKColor> BarBorder { get; init; }

        public State<SKColor> KnobFill { get; init; }
        public State<SKColor> KnobBorder { get; init; }
        public State<SKColor> KnobShadow { get; init; }

        public State<SKColor> SnappingHandle { get; init; }

        public Spring KnobPositionSpring { get; set; }
        private float _knobPosition;

        public Vector2 KnobSize { get; set; } = new(20, 20);
        public float KnobCornerRadius { get; set; } = 20;
        public float KnobBorderSize { get; set; } = 1;

        public Vector2 SnappingHandleSize { get; set; } = new(3, 10);
        public float SnappingHandleCornerRadius { get; set; } = 2;

        public float BarBorderSize { get; set; } = 1;
        public float BarCornerRadius { get; set; } = 5;
        public float BarHeight { get => Transform.Size.CachedValue.y; set => Transform.Size.SetStaticState(new(Transform.Size.CachedValue.x, value)); }

        public bool DisplayFill { get; set; } = true;

        public Action<float>? OnValueChanged { get; set; }
        public Action<float>? OnUserValueChanged { get; set; }

        public FSlider(Func<Vector2>? position = null, float width = 100) : base(position, () => new(width, 3))
        {
            InteractiveSurface.EnableMouseActions.SetStaticState(true);
            InteractiveSurface.OnDrag += OnDragSlider;

            MinValue = new(() => 0, this);
            MaxValue = new(() => 1, this);

            BarFill = new(() => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.Primary, this);
            Bar = new(() => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.Secondary, this);
            BarBorder = new(() => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.OnSecondary.WithAlpha(35), this);

            KnobFill = new(() => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.Secondary, this);
            KnobBorder = new(() => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.OnSecondary.WithAlpha(45), this);

            KnobShadow = new(() => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.Shadow, this);
            SnappingHandle = new(() => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.SecondaryVariant, this);

            Padding.SetStaticState(25);
            InteractiveSurface.ExtendInteractionRadius.SetStaticState(50);

            KnobPositionSpring = new(5f, 1.4f);
        }

        public override void OnInternalStateChanged<T>(T value)
        {
            base.OnInternalStateChanged(value);

            UpdateHostpots();
            Invalidate(Invalidation.SurfaceDirty);
        }

        public void SilentSetValue(float value)
        {
            Value = value;
            Invalidate(Invalidation.SurfaceDirty);

            OnValueChanged?.Invoke(Value);

            float _value = RMath.Remap(GetValue(), MinValue.CachedValue, MaxValue.CachedValue, 0, 1);
            KnobPositionSpring.ResetVector(new(_value * 100, 0));
        }

        public float GetValue(float? c = null)
        {
            if (SnappingInterval != 0)
            {
                List<float> hotspots = _snappingHotspots ?? new();
                hotspots.AddRange(ExtraHotspots);

                return hotspots.OrderBy(x => Math.Abs(x - RMath.Remap(c ?? _value, 0, 1, MinValue.CachedValue, MaxValue.CachedValue))).First();
            }
            else
                return RMath.Remap(c ?? _value, 0, 1, MinValue.CachedValue, MaxValue.CachedValue);
        }

        protected override void Update()
        {
            base.Update();

            float value = RMath.Remap(GetValue(), MinValue.CachedValue, MaxValue.CachedValue, 0, 1);

            var lastKnobPosition = _knobPosition;
            _knobPosition = Math.Clamp(KnobPositionSpring.Update(FContext.DeltaTime, new(value * 100, 0)).x / 100, 0, 1);
            if (Math.Round(lastKnobPosition * 100) != Math.Round(_knobPosition * 100)) Invalidate(Invalidation.SurfaceDirty);
        }

        private void OnDragSlider(Vector2 mousePos)
        {
            var lastValue = GetValue();

            Value = Math.Clamp(
                RMath.Remap(FContext.GetCurrentWindow().ClientMousePosition.x,
                    Shape.GlobalBounds.Left + Padding.CachedValue,
                    Shape.GlobalBounds.Right - Padding.CachedValue,
                MinValue.CachedValue, MaxValue.CachedValue),
                MinValue.CachedValue, MaxValue.CachedValue);

            var val = GetValue();

            if (lastValue != val)
            {
                OnValueChanged?.Invoke(Value);
                OnUserValueChanged?.Invoke(Value);
            }
        }

        public override void Render(SKCanvas canvas)
        {
            float knobPos = _knobPosition;

            using var SkPaint = GetRenderPaint();

            { // Unfilled Bar
                using var paint = SkPaint.Clone();

                paint.Color = Bar.CachedValue;

                SKRect barRect = Shape.LocalBounds;
                barRect.Offset(0.5f, 0.5f);

                var barRoundRect = new SKRoundRect(barRect, BarCornerRadius);
                canvas.DrawRoundRect(barRoundRect, paint);

                if (BarBorderSize > 0)
                {
                    paint.Color = BarBorder.CachedValue;
                    paint.IsStroke = true;
                    paint.StrokeWidth = BarBorderSize;

                    canvas.DrawRoundRect(barRoundRect, paint);
                }

                barRoundRect.Dispose();
            }

            if (DisplayFill)
            { // Filled Bar
                using var paint = SkPaint.Clone();

                paint.Color = BarFill.CachedValue;

                SKRect barRect = SKRect.Create(Shape.LocalBounds.Left, Shape.LocalBounds.MidY - Transform.Size.CachedValue.y / 2, Shape.LocalBounds.Width * knobPos, Transform.Size.CachedValue.y);
                barRect.Offset(0.5f, 0.5f);

                var barRoundRect = new SKRoundRect(barRect, BarCornerRadius);
                canvas.DrawRoundRect(barRoundRect, paint);

                barRoundRect.Dispose();
            }

            List<float> hotspots = new();
            if (_snappingHotspots != null) _snappingHotspots.ForEach(x => hotspots.Add(x));
            if (ExtraHotspots != null) ExtraHotspots.ForEach(x => hotspots.Add(x));

            if (hotspots != null)
            { // Snapping indicators and extra hotspots
                using var paint = SkPaint.Clone();

                canvas.Save();
                SKRect clip = Shape.LocalBounds;
                clip.Inflate(2, SnappingHandleSize.y);
                canvas.ClipRect(clip, antialias: true);

                for (int i = 0; i < hotspots.Count; i++)
                {
                    float value = RMath.Remap(hotspots[i], MinValue.CachedValue, MaxValue.CachedValue, 0, 1);

                    if (DisplayFill && _value > value)
                        paint.Color = BarFill.CachedValue;
                    else
                        paint.Color = SnappingHandle.CachedValue;

                    SKRect snapRect = SKRect.Create(
                        RMath.Lerp(Shape.LocalBounds.Left, Shape.LocalBounds.Right, value) - SnappingHandleSize.x / 2,
                        Shape.LocalBounds.MidY - SnappingHandleSize.y / 2, SnappingHandleSize.x, SnappingHandleSize.y);
                    snapRect.Offset(0.5f, 0.5f);

                    var snapRoundRect = new SKRoundRect(snapRect, SnappingHandleCornerRadius);
                    canvas.DrawRoundRect(snapRoundRect, paint);

                    snapRoundRect.Dispose();
                }

                canvas.Restore();
            }

            { // Knob
                using var paint = SkPaint.Clone();

                paint.Color = KnobFill.CachedValue;

                using (var shadow = SKImageFilter.CreateDropShadow(0, 1, 5, 5, KnobShadow.CachedValue))
                    paint.ImageFilter = shadow;

                SKRect knobRect = SKRect.Create(
                    RMath.Lerp(Shape.LocalBounds.Left, Shape.LocalBounds.Right, knobPos) - KnobSize.x / 2,
                    Shape.LocalBounds.MidY - KnobSize.y / 2, KnobSize.x, KnobSize.y);
                knobRect.Offset(0.5f, 0.5f);

                var knobRoundRect = new SKRoundRect(knobRect, KnobCornerRadius);
                canvas.DrawRoundRect(knobRoundRect, paint);

                SkPaint.ImageFilter = null;

                if (KnobBorderSize > 0)
                {
                    paint.Color = KnobBorder.CachedValue;
                    paint.IsStroke = true;
                    paint.StrokeWidth = KnobBorderSize;

                    canvas.DrawRoundRect(knobRoundRect, paint);
                }

                knobRoundRect.Dispose();
            }
        }

        private void UpdateHostpots()
        {
            if(SnappingInterval != 0)
                _snappingHotspots = GetSnapHotSpots(MinValue.CachedValue, MaxValue.CachedValue, SnappingInterval);
        }

        private List<float> GetSnapHotSpots(float min, float max, float interval)
        {
            var hotSpots = new List<float>();

            for (float i = min; i <= max; i += interval)
            {
                hotSpots.Add(i);
            }

            hotSpots.Sort();
            return hotSpots;
        }

        public override void Dispose()
        {
            base.Dispose();

            MinValue.Dispose();
            MaxValue.Dispose();

            BarFill.Dispose();
            Bar.Dispose();
            BarBorder.Dispose();

            KnobFill.Dispose();
            KnobBorder.Dispose();

            KnobShadow.Dispose();
            SnappingHandle.Dispose();
        }
    }
}