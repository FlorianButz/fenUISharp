using FenUISharp.Components;
using FenUISharp.Mathematics;
using FenUISharp.Themes;
using OpenTK.Graphics.OpenGL;
using SkiaSharp;

namespace FenUISharp
{
    public class FSlider : UIComponent
    {
        protected float _value = 0f;
        public float Value
        {
            get { return GetValue(); }
            set { var lastValue = RMath.Remap(value, MinValue, MaxValue, 0, 1); _value = RMath.Remap(value, MinValue, MaxValue, 0, 1); if (lastValue != _value) { OnValueChanged?.Invoke(value); Invalidate(); } }
        }

        protected float _maxValue = 1f;
        public float MaxValue { get { return _maxValue; } set { _maxValue = value; Invalidate(); UpdateHostpots(); } }
        protected float _minValue = 0f;
        public float MinValue { get { return _minValue; } set { _minValue = value; Invalidate(); UpdateHostpots(); } }

        private List<float>? _snappingHotspots;
        private Func<float, float>? _snappingProvider;
        public Func<float, float>? SnappingProvider { get => _snappingProvider; set { _snappingProvider = value; UpdateHostpots(); } }
        public bool Use01RangeForSnappingProvider { get; set; } = false;
        public int MaxSnapIndicators { get; set; } = 50;

        public List<float> ExtraHotspots { get; set; } = new();

        public ThemeColor BarFill { get; set; }
        public ThemeColor Bar { get; set; }
        public ThemeColor BarBorder { get; set; }

        public ThemeColor KnobFill { get; set; }
        public ThemeColor KnobBorder { get; set; }
        public ThemeColor KnobShadow { get; set; }

        public ThemeColor SnappingHandle { get; set; }

        public Spring KnobPositionSpring { get; set; }
        private float _knobPosition;

        public Vector2 KnobSize { get; set; } = new(20, 20);
        public float KnobCornerRadius { get; set; } = 20;
        public float KnobBorderSize { get; set; } = 1;

        public Vector2 SnappingHandleSize { get; set; } = new(3, 10);
        public float SnappingHandleCornerRadius { get; set; } = 2;

        public float BarBorderSize { get; set; } = 1;
        public float BarCornerRadius { get; set; } = 5;
        public float BarHeight { get => Transform.Size.y; set => Transform.Size = new(Transform.Size.x, value); }

        public bool DisplayFill { get; set; } = true;

        public Action<float>? OnValueChanged { get; set; }
        public Action<float>? OnUserValueChanged { get; set; }

        private UserDragComponent _userDragComponent;

        public FSlider(Window rootWindow, Vector2 position, float width) : base(rootWindow, position, new(width, 3))
        {
            CanInteractVisualIndicator = true;

            _userDragComponent = new(this);
            _userDragComponent.OnDrag += OnDragSlider;

            BarFill = rootWindow.WindowThemeManager.GetColor(t => t.Primary);
            Bar = rootWindow.WindowThemeManager.GetColor(t => t.Secondary);
            BarBorder = rootWindow.WindowThemeManager.GetColor(t => t.OnSecondary.WithAlpha(35));

            KnobFill = rootWindow.WindowThemeManager.GetColor(t => t.Secondary);
            KnobBorder = rootWindow.WindowThemeManager.GetColor(t => t.OnSecondary.WithAlpha(45));

            KnobShadow = rootWindow.WindowThemeManager.GetColor(t => t.Shadow);
            SnappingHandle = rootWindow.WindowThemeManager.GetColor(t => t.SecondaryVariant);

            Transform.InteractionPadding = 25;
            Transform.BoundsPadding.SetValue(this, 50, 50);

            KnobPositionSpring = new(5f, 1.4f);
        }

        public void SilentSetValue(float value)
        {
            Value = value;
            Invalidate();

            OnValueChanged?.Invoke(Value);

            float _value = RMath.Remap(GetValue(), MinValue, MaxValue, 0, 1);
            KnobPositionSpring.ResetVector(new(_value * 100, 0));
        }

        private float GetValue()
        {
            if (SnappingProvider != null)
            {
                if (Use01RangeForSnappingProvider)
                    return RMath.Remap(SnappingProvider(_value), 0, 1, MinValue, MaxValue);
                else
                    return SnappingProvider(RMath.Remap(_value, 0, 1, MinValue, MaxValue));
            }
            else
                return RMath.Remap(_value, 0, 1, MinValue, MaxValue);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            float value = RMath.Remap(GetValue(), MinValue, MaxValue, 0, 1);

            var lastKnobPosition = _knobPosition;
            _knobPosition = Math.Clamp(KnobPositionSpring.Update((float)WindowRoot.DeltaTime, new(value * 100, 0)).x / 100, 0, 1);
            if (Math.Round(lastKnobPosition * 100) != Math.Round(_knobPosition * 100)) Invalidate();
        }

        private void OnDragSlider(Vector2 mousePos)
        {
            Value = Math.Clamp(RMath.Remap(WindowRoot.ClientMousePosition.x, Transform.Bounds.Left, Transform.Bounds.Right, MinValue, MaxValue), MinValue, MaxValue);
            
            OnValueChanged?.Invoke(Value);
            OnUserValueChanged?.Invoke(Value);
        }

        protected override void DrawToSurface(SKCanvas canvas)
        {
            float knobPos = _knobPosition;

            { // Unfilled Bar
                var paint = SkPaint.Clone();

                paint.Color = Bar.Value;

                SKRect barRect = Transform.LocalBounds;
                barRect.Offset(0.5f, 0.5f);

                var barRoundRect = new SKRoundRect(barRect, BarCornerRadius);
                canvas.DrawRoundRect(barRoundRect, paint);

                if (BarBorderSize > 0)
                {
                    paint.Color = BarBorder.Value;
                    paint.IsStroke = true;
                    paint.StrokeWidth = BarBorderSize;

                    canvas.DrawRoundRect(barRoundRect, paint);
                }

                barRoundRect.Dispose();
            }

            if (DisplayFill)
            { // Filled Bar
                var paint = SkPaint.Clone();

                paint.Color = BarFill.Value;

                SKRect barRect = SKRect.Create(Transform.LocalBounds.Left, Transform.LocalBounds.MidY - Transform.Size.y / 2, Transform.LocalBounds.Width * knobPos, Transform.Size.y);
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
                var paint = SkPaint.Clone();

                canvas.Save();
                SKRect clip = Transform.LocalBounds;
                clip.Inflate(2, SnappingHandleSize.y);
                canvas.ClipRect(clip, antialias: true);

                for (int i = 0; i < hotspots.Count; i++)
                {
                    float value = RMath.Remap(hotspots[i], MinValue, MaxValue, 0, 1);

                    if (DisplayFill && _value > value)
                        paint.Color = BarFill.Value;
                    else
                        paint.Color = SnappingHandle.Value;

                    SKRect snapRect = SKRect.Create(
                        RMath.Lerp(Transform.LocalBounds.Left, Transform.LocalBounds.Right, value) - SnappingHandleSize.x / 2,
                        Transform.LocalBounds.MidY - SnappingHandleSize.y / 2, SnappingHandleSize.x, SnappingHandleSize.y);
                    snapRect.Offset(0.5f, 0.5f);

                    var snapRoundRect = new SKRoundRect(snapRect, SnappingHandleCornerRadius);
                    canvas.DrawRoundRect(snapRoundRect, paint);

                    snapRoundRect.Dispose();
                }

                canvas.Restore();
            }

            { // Knob
                var paint = SkPaint.Clone();

                paint.Color = KnobFill.Value;

                using (var shadow = SKImageFilter.CreateDropShadow(0, 1, 5, 5, KnobShadow.Value))
                    paint.ImageFilter = shadow;

                SKRect knobRect = SKRect.Create(
                    RMath.Lerp(Transform.LocalBounds.Left, Transform.LocalBounds.Right, knobPos) - KnobSize.x / 2,
                    Transform.LocalBounds.MidY - KnobSize.y / 2, KnobSize.x, KnobSize.y);
                knobRect.Offset(0.5f, 0.5f);

                var knobRoundRect = new SKRoundRect(knobRect, KnobCornerRadius);
                canvas.DrawRoundRect(knobRoundRect, paint);

                SkPaint.ImageFilter = null;

                if (KnobBorderSize > 0)
                {
                    paint.Color = KnobBorder.Value;
                    paint.IsStroke = true;
                    paint.StrokeWidth = KnobBorderSize;

                    canvas.DrawRoundRect(knobRoundRect, paint);
                }

                knobRoundRect.Dispose();
            }
        }

        private void UpdateHostpots()
        {
            if (_snappingProvider != null)
                _snappingHotspots = GetSnapHotSpots(MinValue, MaxValue, _snappingProvider);
        }

        private List<float> GetSnapHotSpots(float min, float max, Func<float, float> snapFunc, float sampleStep = 0.1f, float tolerance = 0.01f)
        {
            var hotSpots = new List<float>();

            int count = 0;
            for (float x = min; x <= max; x += sampleStep)
            {
                count++;
                float snapped = snapFunc(x);

                if (!hotSpots.Any(h => Math.Abs(h - snapped) < tolerance))
                    hotSpots.Add(snapped);
                
                if (count > MaxSnapIndicators) break;
            }

            hotSpots.Sort();
            return hotSpots;
        }
    }
}