using FenUISharp.Themes;
using SkiaSharp;

namespace FenUISharp
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

        private Vector2 normalSize;
        private Vector2 hoverSize;
        private bool _isDragging;
        private SKRect lastThumbInteractionRect;

        public Action<float>? onPositionChanged;

        private float _scrollDragPosition;
        private float _lastScrollPos;
        private float _mouseStartScrollPos;
        private Vector2 _mouseStartDragPos;

        public FScrollBar(Window rootWindow, Vector2 position, Vector2 size, ThemeColor? areaColor = null, ThemeColor? positionColor = null) : base(rootWindow, position, size)
        {
            _scrollAreaColor = areaColor ?? WindowRoot.WindowThemeManager.GetColor(t => t.SurfaceVariant);
            _scrollPositionColor = positionColor ?? WindowRoot.WindowThemeManager.GetColor(t => t.OnSurface);

            normalSize = transform.size;
            hoverSize = normalSize * 2f;
            transform.interactionPadding = 5;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            lastThumbInteractionRect = GetThumbRect(transform.bounds);

            transform.size = RMath.Lerp(transform.size, (_isMouseHovering || _isDragging) ? hoverSize : normalSize, (float)WindowRoot.DeltaTime * 7.5f);
            if (Math.Round(transform.size.x) != Math.Round(normalSize.x) && Math.Round(transform.size.x) != Math.Round(hoverSize.x)) Invalidate();

            Alpha -= AlphaFadeSpeed * (float)WindowRoot.DeltaTime;
            if (Alpha < 0f) Alpha = 0;

            if (_isMouseHovering) Alpha = AlphaFadeTime;

            if (Alpha != _lastAlpha) Invalidate();
            _lastAlpha = Alpha;

            if (_isDragging)
            {
                var thumbSize = GetThumbRect(transform.localBounds);
                float availableTrackSize = HorizontalOrientation ? transform.size.x - thumbSize.Width : transform.size.y - thumbSize.Height;
                float mouseDelta = HorizontalOrientation
                    ? (WindowRoot.ClientMousePosition.x - _mouseStartDragPos.x)
                    : (WindowRoot.ClientMousePosition.y - _mouseStartDragPos.y);

                // Convert pixel movement to scroll range movement
                float deltaScroll = (-mouseDelta / availableTrackSize) * (ScrollMax - ScrollMin);

                _scrollDragPosition = RMath.Clamp(_mouseStartScrollPos + deltaScroll, ScrollMin, ScrollMax);

                if (_lastScrollPos != _scrollDragPosition)
                {
                    onPositionChanged?.Invoke(_scrollDragPosition);
                }

                _lastScrollPos = _scrollDragPosition;
            }

        }

        public void UpdateScrollbar()
        {
            Alpha = AlphaFadeTime;
            Invalidate();
        }

        protected override void MouseAction(MouseInputCode inputCode)
        {
            base.MouseAction(inputCode);

            if (inputCode.button == 0 && inputCode.state == 0)
            {
                if (RMath.ContainsPoint(lastThumbInteractionRect, WindowRoot.ClientMousePosition))
                {
                    _mouseStartDragPos = WindowRoot.ClientMousePosition;
                    _mouseStartScrollPos = ScrollPosition;
                    _isDragging = true;
                }
                else
                    _isDragging = false;
            }
            else if (inputCode.button == 0 && inputCode.state == 1)
            {
                _isDragging = false;
            }
        }

        protected override void DrawToSurface(SKCanvas canvas)
        {
            var scrollArea = transform.localBounds;

            // Draw the scroll track
            skPaint.Color = _scrollAreaColor.Value.WithAlpha((byte)(255 * RMath.Clamp(Alpha, 0, (_isMouseHovering || _isDragging) ? 1f : 0.4f)));
            canvas.DrawRoundRect(scrollArea, 5, 5, skPaint);

            canvas.ClipRoundRect(new SKRoundRect(scrollArea, 5, 5), antialias: true);

            SKRect thumbRect = GetThumbRect(scrollArea);

            // Draw the scroll thumb
            skPaint.Color = _scrollPositionColor.Value.WithAlpha((byte)(255 * RMath.Clamp(Alpha, 0, (_isMouseHovering || _isDragging) ? 1f : 0.4f)));
            canvas.DrawRoundRect(thumbRect, 5, 5, skPaint);
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