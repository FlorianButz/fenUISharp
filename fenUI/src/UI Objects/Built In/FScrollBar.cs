using FenUISharp.Mathematics;
using FenUISharp.States;
using FenUISharp.Themes;
using SkiaSharp;

namespace FenUISharp.Objects
{
    public class FScrollBar : UIObject, IStateListener
    {
        public float ScrollPosition { get; set; }
        public float ScrollMin { get; set; }
        public float ScrollMax { get; set; }

        public float PageSize { get; set; }
        public float ContentSize { get; set; }

        public float MinThumbSize { get; set; } = 15f;

        public State<bool> HorizontalOrientation { get; set; }

        public State<SKColor> ScrollAreaColor { get; init; }
        public State<SKColor> ScrollPositionColor { get; init; }

        private float _lastAlpha = 0f;
        public float Alpha { get; set; } = 0f;
        public float AlphaFadeSpeed { get; set; } = 0.75f;
        public float AlphaFadeTime { get; set; } = 2f;

        private SKRect lastThumbInteractionRect;

        public Action<float>? onPositionChanged;

        private float _scrollDragPosition;
        private float _lastScrollPos;
        private float _mouseStartScrollPos;

        public FScrollBar(Func<Vector2>? position = null, Func<Vector2>? size = null) : base(position, size)
        {
            ScrollAreaColor = new(() => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.SurfaceVariant, this);
            ScrollPositionColor = new(() => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.OnSurface, this);

            HorizontalOrientation = new(() => false, this);
            InteractiveSurface.EnableMouseActions.Value = () => true;
            InteractiveSurface.ExtendInteractionRadius.Value = () => 5;

            InteractiveSurface.OnDrag += OnDrag;
            InteractiveSurface.OnDragEnd += OnDragEnd;
            InteractiveSurface.OnDragStart += OnDragStart;

            InteractiveSurface.OnMouseEnter += MouseEnter;
            InteractiveSurface.OnMouseExit += MouseExit;

            Visible.Value = () => ContentSize > PageSize;
        }

        public override void OnInternalStateChanged<T>(T value)
        {
            base.OnInternalStateChanged(value);

            Invalidate(Invalidation.All);
        }

        public override void Dispose()
        {
            base.Dispose();

            ScrollAreaColor.Dispose();
            ScrollPositionColor.Dispose();
            HorizontalOrientation.Dispose();

            InteractiveSurface.OnMouseEnter -= MouseEnter;
            InteractiveSurface.OnMouseExit -= MouseExit;
        }

        protected override void Update()
        {
            base.Update();

            lastThumbInteractionRect = GetThumbRect(Shape.LocalBounds);

            Alpha -= AlphaFadeSpeed * (float)FContext.GetCurrentWindow().DeltaTime;
            if (Alpha < 0f) Alpha = 0;

            if (InteractiveSurface.IsMouseHovering) Alpha = AlphaFadeTime;

            if (RMath.Clamp(Alpha, 0, 1) != RMath.Clamp(_lastAlpha, 0, 1)) Invalidate(Invalidation.SurfaceDirty);
            _lastAlpha = Alpha;
        }

        void OnDrag(Vector2 delta)
        {
            var thumbSize = GetThumbRect(Shape.LocalBounds);
            float availableTrackSize = HorizontalOrientation.CachedValue ? Transform.Size.CachedValue.x - thumbSize.Width : Transform.Size.CachedValue.y - thumbSize.Height;
            float mouseDelta = HorizontalOrientation.CachedValue
                ? (delta.x)
                : (delta.y);

            // Calculate the ratio of page size to content size for proper sensitivity
            float contentRatio = ContentSize > 0 ? PageSize / ContentSize : 1.0f;
            contentRatio = RMath.Clamp(contentRatio, 0.01f, 1.0f); // Prevent division issues

            // Convert pixel movement to scroll range movement, adjusted by content ratio
            float deltaScroll = (mouseDelta / availableTrackSize) * (ScrollMax - ScrollMin) * contentRatio;

            _scrollDragPosition = RMath.Clamp(_mouseStartScrollPos + deltaScroll, ScrollMin, ScrollMax);

            if (_lastScrollPos != _scrollDragPosition)
            {
                onPositionChanged?.Invoke(_scrollDragPosition);
            }

            _lastScrollPos = _scrollDragPosition;
        }

        void OnDragStart()
        {
            Invalidate(Invalidation.SurfaceDirty);

            var interactionRect = lastThumbInteractionRect;
            interactionRect.Inflate(InteractiveSurface.ExtendInteractionRadius.CachedValue, InteractiveSurface.ExtendInteractionRadius.CachedValue);
                    
            _mouseStartScrollPos = ScrollPosition;
        }

        void OnDragEnd()
        {
            Invalidate(Invalidation.SurfaceDirty);
        }

        public void UpdateScrollbar()
        {
            Alpha = AlphaFadeTime;
            Invalidate(Invalidation.SurfaceDirty);
        }

        protected void MouseEnter()
        {
            Invalidate(Invalidation.SurfaceDirty);
        }

        protected void MouseExit()
        {
            Invalidate(Invalidation.SurfaceDirty);
        }


        public override void Render(SKCanvas canvas)
        {
            base.Render(canvas);

            using var SkPaint = GetRenderPaint();
            var scrollArea = Shape.LocalBounds;

            // Draw the scroll track
            SkPaint.Color = ScrollAreaColor.CachedValue.WithAlpha((byte)(255 * RMath.Clamp(Alpha, 0, (InteractiveSurface.IsMouseHovering || InteractiveSurface.IsDragging) ? 1f : 0.4f)));
            canvas.DrawRoundRect(scrollArea, 5, 5, SkPaint);

            canvas.ClipRoundRect(new SKRoundRect(scrollArea, 5, 5), antialias: true);

            SKRect thumbRect = GetThumbRect(scrollArea);

            // Draw the scroll thumb
            SkPaint.Color = ScrollPositionColor.CachedValue.WithAlpha((byte)(255 * RMath.Clamp(Alpha, 0, (InteractiveSurface.IsMouseHovering || InteractiveSurface.IsDragging) ? 1f : 0.4f)));
            canvas.DrawRoundRect(thumbRect, 5, 5, SkPaint);
        }

        private SKRect GetThumbRect(SKRect scrollArea)
        {
            SKRect thumbRect;

            if (HorizontalOrientation.CachedValue)
            {
                float trackLength = scrollArea.Width;
                float thumbLength = ContentSize > 0 ? (PageSize / ContentSize) * trackLength : trackLength;
                thumbLength = Math.Max(thumbLength, MinThumbSize);

                float availableLength = trackLength - thumbLength;
                float fraction = (ScrollMax - ScrollMin) > 0
                    ? (ScrollPosition - ScrollMin) / (ScrollMax - ScrollMin)
                    : 0;

                float thumbPos = scrollArea.Left + (availableLength * (1 - fraction));
                thumbRect = new SKRect(thumbPos, scrollArea.Top, thumbPos + thumbLength, scrollArea.Bottom);
            }
            else
            {
                float trackLength = scrollArea.Height;
                float thumbLength = ContentSize > 0 ? (PageSize / ContentSize) * trackLength : trackLength;
                thumbLength = Math.Max(thumbLength, MinThumbSize);

                float availableLength = trackLength - thumbLength;
                float fraction = (ScrollMax - ScrollMin) > 0
                    ? (ScrollPosition - ScrollMin) / (ScrollMax - ScrollMin)
                    : 0;

                float thumbTop = scrollArea.Top + (availableLength * (1 - fraction));
                thumbRect = new SKRect(scrollArea.Left, thumbTop, scrollArea.Right, thumbTop + thumbLength);
            }

            return thumbRect;
        }
    }
}