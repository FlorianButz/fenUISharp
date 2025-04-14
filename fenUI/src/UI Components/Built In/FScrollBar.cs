using FenUISharp.Mathematics;
using FenUISharp.Themes;
using SkiaSharp;

namespace FenUISharp.Components
{
    public class FScrollBar : UIComponent
    {
        public float ScrollPosition { get; set; }
        public float ScrollMin { get; set; }
        public float ScrollMax { get; set; }

        public float PageSize { get; set; }
        public float ContentSize { get; set; }

        public float MinThumbSize { get; set; } = 15f;

        public bool HorizontalOrientation { get; set; }

        private ThemeColor _scrollAreaColor;
        public ThemeColor ScrollAreaColor { get => _scrollAreaColor; set { _scrollAreaColor = value; Invalidate(); } }

        private ThemeColor _scrollPositionColor;
        public ThemeColor ScrollPositionColor { get => _scrollPositionColor; set { _scrollPositionColor = value; Invalidate(); } }

        private float _lastAlpha = 0f;
        public float Alpha { get; set; } = 0f;
        public float AlphaFadeSpeed { get; set; } = 0.75f;
        public float AlphaFadeTime { get; set; } = 2f;

        // private Vector2 normalSize;
        // private Vector2 hoverSize;
        private SKRect lastThumbInteractionRect;

        public Action<float>? onPositionChanged;

        private float _scrollDragPosition;
        private float _lastScrollPos;
        private float _mouseStartScrollPos;

        private UserDragComponent dragComponent;

        public FScrollBar(Window rootWindow, Vector2 position, Vector2 size, ThemeColor? areaColor = null, ThemeColor? positionColor = null) : base(rootWindow, position, size)
        {
            _scrollAreaColor = areaColor ?? WindowRoot.WindowThemeManager.GetColor(t => t.SurfaceVariant);
            _scrollPositionColor = positionColor ?? WindowRoot.WindowThemeManager.GetColor(t => t.OnSurface);

            Transform.InteractionPadding = 5;

            dragComponent = new(this);
            dragComponent.OnDrag += OnDrag;
            dragComponent.OnDragEnd += OnDragEnd;
            dragComponent.OnDragStart += OnDragStart;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            lastThumbInteractionRect = GetThumbRect(Transform.Bounds);

            Alpha -= AlphaFadeSpeed * (float)WindowRoot.DeltaTime;
            if (Alpha < 0f) Alpha = 0;

            if (_isMouseHovering) Alpha = AlphaFadeTime;

            if (RMath.Clamp(Alpha, 0, 1) != RMath.Clamp(_lastAlpha, 0, 1)) Invalidate();
            _lastAlpha = Alpha;

            Visible = ContentSize > PageSize;
        }

        void OnDrag(Vector2 delta)
        {
            var thumbSize = GetThumbRect(Transform.LocalBounds);
            float availableTrackSize = HorizontalOrientation ? Transform.Size.x - thumbSize.Width : Transform.Size.y - thumbSize.Height;
            float mouseDelta = HorizontalOrientation
                ? (delta.x)
                : (delta.y);

            // Convert pixel movement to scroll range movement
            float deltaScroll = (-mouseDelta / availableTrackSize) * (ScrollMax - ScrollMin);

            _scrollDragPosition = RMath.Clamp(_mouseStartScrollPos + deltaScroll, ScrollMin, ScrollMax);

            if (_lastScrollPos != _scrollDragPosition)
            {
                onPositionChanged?.Invoke(_scrollDragPosition);
            }

            _lastScrollPos = _scrollDragPosition;
        }

        void OnDragStart()
        {
            Invalidate();

            var interactionRect = lastThumbInteractionRect;
            interactionRect.Inflate(Transform.InteractionPadding, Transform.InteractionPadding);
                    
                    _mouseStartScrollPos = ScrollPosition;
        }

        void OnDragEnd()
        {
            Invalidate();
        }

        public void UpdateScrollbar()
        {
            Alpha = AlphaFadeTime;
            Invalidate();
        }

        protected override void MouseEnter()
        {
            base.MouseEnter();
            Invalidate();
        }

        protected override void MouseExit()
        {
            base.MouseExit();
            Invalidate();
        }

        protected override void DrawToSurface(SKCanvas canvas)
        {
            var scrollArea = Transform.LocalBounds;

            // Draw the scroll track
            SkPaint.Color = _scrollAreaColor.Value.WithAlpha((byte)(255 * RMath.Clamp(Alpha, 0, (_isMouseHovering || dragComponent.IsDragging) ? 1f : 0.4f)));
            canvas.DrawRoundRect(scrollArea, 5, 5, SkPaint);

            canvas.ClipRoundRect(new SKRoundRect(scrollArea, 5, 5), antialias: true);

            SKRect thumbRect = GetThumbRect(scrollArea);

            // Draw the scroll thumb
            SkPaint.Color = _scrollPositionColor.Value.WithAlpha((byte)(255 * RMath.Clamp(Alpha, 0, (_isMouseHovering || dragComponent.IsDragging) ? 1f : 0.4f)));
            canvas.DrawRoundRect(thumbRect, 5, 5, SkPaint);
        }

        private SKRect GetThumbRect(SKRect scrollArea)
        {
            SKRect thumbRect;

            if (HorizontalOrientation)
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