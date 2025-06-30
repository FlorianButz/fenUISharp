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
            get => RMath.Remap(_value, 0, 1, MinValue.CachedValue, MaxValue.CachedValue);
            set
            {
                float clamped = Math.Clamp(value, MinValue.CachedValue, MaxValue.CachedValue);
                float newNormalized = RMath.Remap(clamped, MinValue.CachedValue, MaxValue.CachedValue, 0, 1);

                if (!RMath.Approximately(_value, newNormalized))
                {
                    _value = SnapIfNeeded(newNormalized);
                    OnValueChanged?.Invoke(Value);
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
        public bool ClampKnob { get; set; } = false;

        public Action<float>? OnValueChanged { get; set; }
        public Action<float>? OnUserValueChanged { get; set; }

        public FSlider(Func<Vector2>? position = null, float width = 100) : base(position, () => new(width, 3))
        {
            InteractiveSurface.EnableMouseActions.SetStaticState(true);
            InteractiveSurface.OnMouseAction += MouseAction;
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

            Padding.SetStaticState(15);
            InteractiveSurface.ExtendInteractionRadius.SetStaticState(2);

            KnobPositionSpring = new(5f, 1.4f);
        }

        private void MouseAction(MouseInputCode code)
        {
            if (code.button == MouseInputButton.Left && code.state == MouseInputState.Down)
            {
                SetToPoint(FContext.GetCurrentWindow().ClientMousePosition);
            }
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

        private float SnapIfNeeded(float normalized)
        {
            if (SnappingInterval <= 0) return normalized;

            float actualValue = RMath.Remap(normalized, 0, 1, MinValue.CachedValue, MaxValue.CachedValue);
            var allHotspots = new List<float>(_snappingHotspots ?? new());
            allHotspots.AddRange(ExtraHotspots);

            if (allHotspots.Count == 0) return normalized;

            float snapped = allHotspots.OrderBy(x => Math.Abs(x - actualValue)).First();
            return RMath.Remap(snapped, MinValue.CachedValue, MaxValue.CachedValue, 0, 1);
        }

        protected override void Update()
        {
            base.Update();

            var lastKnobPosition = _knobPosition;
            _knobPosition = KnobPositionSpring.Update(FContext.DeltaTime, new(_value * 100, 0)).x / 100;

            if (Math.Round(lastKnobPosition * 100) != Math.Round(_knobPosition * 100))
                Invalidate(Invalidation.SurfaceDirty);
        }

        private void OnDragSlider(Vector2 mousePos)
        {
            SetToPoint(FContext.GetCurrentWindow().ClientMousePosition);
        }

        private void SetToPoint(Vector2 pos)
        {
            var lastValue = GetValue();

            Value = Math.Clamp(
                RMath.Remap(pos.x,
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

            var bounds = Shape.LocalBounds;
            bounds.Offset(0.5f, 0.5f);

            RenderBackground(canvas, bounds);

            SKRect barRect = SKRect.Create(Shape.LocalBounds.Left, Shape.LocalBounds.MidY - Transform.Size.CachedValue.y / 2, Shape.LocalBounds.Width * knobPos, Transform.Size.CachedValue.y);
            barRect.Offset(0.5f, 0.5f);
            RenderFilledBackground(canvas, barRect);

            List<float> hotspots = new();
            if (_snappingHotspots != null) _snappingHotspots.ForEach(x => hotspots.Add(x));
            if (ExtraHotspots != null) ExtraHotspots.ForEach(x => hotspots.Add(x));

            if (hotspots != null)
            { // Snapping indicators and extra hotspots
                RenderHotspots(canvas, hotspots);
            }

            SKRect knobRect = SKRect.Create(
                RMath.Lerp(Shape.LocalBounds.Left, Shape.LocalBounds.Right, knobPos) - KnobSize.x / 2,
                Shape.LocalBounds.MidY - KnobSize.y / 2, KnobSize.x, KnobSize.y);

            if (ClampKnob)
            {
                knobRect = SKRect.Create(
                    RMath.Clamp(RMath.Lerp(Shape.LocalBounds.Left, Shape.LocalBounds.Right, knobPos) - KnobSize.x / 2, Shape.LocalBounds.Left, Shape.LocalBounds.Right - knobRect.Width),
                    Shape.LocalBounds.MidY - KnobSize.y / 2, KnobSize.x, KnobSize.y
                );
            }

            knobRect.Offset(0.5f, 0.5f);
            RenderKnob(canvas, knobRect);
        }

        protected virtual void RenderKnob(SKCanvas canvas, SKRect knobRect)
        {
            using var paint = GetRenderPaint();

            paint.Color = KnobFill.CachedValue;

            using (var shadow = SKImageFilter.CreateDropShadow(0, 1, 5, 5, KnobShadow.CachedValue))
                paint.ImageFilter = shadow;

            using var knobRoundRect = new SKRoundRect(knobRect, KnobCornerRadius);
            canvas.DrawRoundRect(knobRoundRect, paint);

            if (KnobBorderSize > 0)
            {
                paint.Color = KnobBorder.CachedValue;
                paint.IsStroke = true;
                paint.StrokeWidth = KnobBorderSize;

                canvas.DrawRoundRect(knobRoundRect, paint);
            }

            knobRoundRect.Dispose();
        }

        protected virtual void RenderBackground(SKCanvas canvas, SKRect rect)
        {
            // Unfilled Bar
            using var paint = GetRenderPaint();

            paint.Color = Bar.CachedValue;

            using var barRoundRect = new SKRoundRect(rect, BarCornerRadius);
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

        protected virtual void RenderFilledBackground(SKCanvas canvas, SKRect rect)
        {
            if (DisplayFill)
            { // Filled Bar
                using var paint = GetRenderPaint();

                paint.Color = BarFill.CachedValue;

                using var barRoundRect = new SKRoundRect(rect, BarCornerRadius);
                canvas.DrawRoundRect(barRoundRect, paint);

                barRoundRect.Dispose();
            }
        }

        protected virtual void RenderHotspots(SKCanvas canvas, List<float> hotspots)
        {
            if (hotspots.Count > MaxSnapIndicators) return; // If too many, don't render them at all
            
            using var paint = GetRenderPaint();

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

                using var snapRoundRect = new SKRoundRect(snapRect, SnappingHandleCornerRadius);
                canvas.DrawRoundRect(snapRoundRect, paint);

                snapRoundRect.Dispose();
            }

            canvas.Restore();
        }

        private void UpdateHostpots()
        {
            if (SnappingInterval != 0)
                _snappingHotspots = GetSnapHotSpots(MinValue.CachedValue, MaxValue.CachedValue, SnappingInterval);
        }

        private List<float> GetSnapHotSpots(float min, float max, float interval)
        {
            var hotSpots = new List<float>();

            for (float i = min; i < max; i += interval)
            {
                hotSpots.Add(i);
            }

            if (!hotSpots.Contains(min)) hotSpots.Add(min);
            if (!hotSpots.Contains(max)) hotSpots.Add(max);

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