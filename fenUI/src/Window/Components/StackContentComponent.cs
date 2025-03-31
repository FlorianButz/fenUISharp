using System.Diagnostics;
using SkiaSharp;

namespace FenUISharp
{
    public class StackContentComponent : Component
    {
        public enum ContentStackType { Horizontal, Vertical }
        public enum ContentStackBehavior { Overflow, SizeToFit, SizeToFitAll, Scroll }

        public Vector2 StartAlignment { get; set; }
        public float Gap { get; set; } = 10;
        public float Pad { get; set; } = 15;

        public bool AllowScrollOverflow { get; set; } = true;

        public ContentStackType StackType { get; set; }
        public ContentStackBehavior StackBehavior { get; set; }

        private float _scrollSpeed = 0.25f;
        private float _scrollPosition = 0f;
        private float _scrollDisplayPosition = 0f;
        private float _lastScrollDisplayPosition = 0f;

        private float _scrollMin = 0;
        private float _scrollMax = 0;

        private float _pageSize = 0;
        private float _contentSize = 0;

        public Spring? ScrollSpring { get; set; }
        private UserScrollComponent? scrollComponent;
        private FScrollBar scrollBar;

        public StackContentComponent(UIComponent parent, ContentStackType type, ContentStackBehavior behavior, Vector2? startAlign = null) : base(parent)
        {
            StackType = type;
            StackBehavior = behavior;

            scrollComponent = new UserScrollComponent(parent);
            scrollComponent.MouseScroll += OnScroll;
            parent.Components.Add(scrollComponent);

            ScrollSpring = new Spring(new Vector2(0, 0), 2, 0.85f, 0.1f);

            if (startAlign == null)
            {
                if (type == ContentStackType.Horizontal) StartAlignment = new Vector2(0f, 0.5f);
                else if (type == ContentStackType.Vertical) StartAlignment = new Vector2(0.5f, 0f);
            }
            else
            {
                StartAlignment = startAlign.Value;
            }
        }

        public override void ComponentSetup()
        {
            base.ComponentSetup();

            scrollBar = new FScrollBar(parent.WindowRoot, new Vector2(0, 0), new Vector2(4f, 4f));
            scrollBar.Transform.MarginHorizontal = 8;
            scrollBar.Transform.MarginVertical = 8;

            scrollBar.Transform.ParentIgnoreLayout = true;
            scrollBar.Transform.IgnoreParentOffset = true;
            scrollBar.Visible = false;
            scrollBar.onPositionChanged += OnScrollbarUpdate;
            scrollBar.Transform.SetParent(parent.Transform);
            parent.WindowRoot.AddUIComponent(scrollBar);

            UpdateScrollbar();
        }

        private void OnScroll(float delta)
        {
            if (StackBehavior == ContentStackBehavior.Scroll)
                _scrollPosition += delta * _scrollSpeed;
        }

        private void OnScrollbarUpdate(float value)
        {
            if (StackBehavior == ContentStackBehavior.Scroll)
                _scrollPosition = value;
        }

        public void UpdateScrollbar()
        {
            scrollBar.Visible = StackBehavior == ContentStackBehavior.Scroll;
            if (StackBehavior == ContentStackBehavior.Scroll)
            {
                if (StackType == ContentStackType.Vertical)
                {
                    scrollBar.Transform.LocalPosition = new Vector2(-8, 0);
                    scrollBar.Transform.Alignment = new Vector2(1, 0.5f);
                    scrollBar.Transform.StretchHorizontal = false;
                    scrollBar.Transform.StretchVertical = true;
                    scrollBar.HorizontalOrientation = false;
                }
                else if (StackType == ContentStackType.Horizontal)
                {
                    scrollBar.Transform.LocalPosition = new Vector2(0, -8);
                    scrollBar.Transform.Alignment = new Vector2(0.5f, 1);
                    scrollBar.Transform.StretchVertical = false;
                    scrollBar.Transform.StretchHorizontal = true;
                    scrollBar.HorizontalOrientation = true;
                }
            }
        }

        public override void ComponentUpdate()
        {
            base.ComponentUpdate();


            if (!AllowScrollOverflow)
                _scrollPosition = RMath.Clamp(_scrollPosition, -_scrollMax, _scrollMin);

            if(_contentSize > _pageSize)
                _scrollDisplayPosition = ScrollSpring.Update((float)parent.WindowRoot.DeltaTime, new Vector2(_scrollPosition, 0)).x;
            else
                _scrollDisplayPosition = _pageSize / 2 - _contentSize / 2;

            if (AllowScrollOverflow)
                _scrollPosition = RMath.Clamp(_scrollPosition, -_scrollMax, _scrollMin);

            if (StackBehavior == ContentStackBehavior.Scroll)
            {
                if (StackType == ContentStackType.Horizontal)
                    parent.Transform.ChildOffset = new Vector2(_scrollDisplayPosition, 0);
                else if (StackType == ContentStackType.Vertical)
                    parent.Transform.ChildOffset = new Vector2(0, _scrollDisplayPosition);
            }

            if (Math.Round(_lastScrollDisplayPosition) != Math.Round(_scrollDisplayPosition))
            {
                scrollBar.UpdateScrollbar();
                parent.Invalidate();
            }

            _lastScrollDisplayPosition = _scrollDisplayPosition;

            scrollBar.ScrollMin = -_scrollMax;
            scrollBar.ScrollMax = _scrollMin;

            scrollBar.PageSize = _pageSize;
            scrollBar.ContentSize = _contentSize;

            scrollBar.ScrollPosition = _scrollDisplayPosition;
        }

        public override void OnBeforeRenderChildren(SKCanvas canvas)
        {
            base.OnBeforeRenderChildren(canvas);

            if (StackBehavior == ContentStackBehavior.Scroll)
            {
                canvas.ClipRect(parent.Transform.Bounds);
            }
        }

        public void UpdatePosition()
        {
            var childList = parent.Transform.Children;

            float currentPos = 0;
            float contentSize = 0;
            float contentSizePerpendicular = 0;

            float lastItemSize = 0;

            for (int c = 0; c < childList.Count; c++)
            {
                if (childList[c].ParentIgnoreLayout) continue; // Make sure to ignore some transforms

                lastItemSize = StackType == ContentStackType.Horizontal ? childList[c].LocalBounds.Width : childList[c].LocalBounds.Height;

                currentPos += lastItemSize / 2;
                contentSize += lastItemSize / 2;

                if (c == 0)
                {
                    currentPos += Pad;
                    contentSize += Pad;
                }
                else
                {
                    currentPos += Gap;
                    contentSize += Gap;
                }

                contentSizePerpendicular = Math.Max(StackType == ContentStackType.Horizontal ? childList[c].LocalBounds.Height : childList[c].LocalBounds.Width, contentSizePerpendicular);

                childList[c].Alignment = StartAlignment;
                if (StackType == ContentStackType.Horizontal)
                    childList[c].LocalPosition = new Vector2(currentPos, 0);
                else
                    childList[c].LocalPosition = new Vector2(0, currentPos);

                currentPos += lastItemSize / 2;
                contentSize += lastItemSize / 2;
            }

            contentSize = Math.Abs(contentSize + Pad);
            contentSizePerpendicular += Pad * 2;

            switch (StackBehavior)
            {
                case ContentStackBehavior.SizeToFitAll:
                    if (StackType == ContentStackType.Horizontal) parent.Transform.Size = new Vector2(contentSize, contentSizePerpendicular);
                    if (StackType == ContentStackType.Vertical) parent.Transform.Size = new Vector2(contentSizePerpendicular, contentSize);
                    break;
                case ContentStackBehavior.SizeToFit:
                    if (StackType == ContentStackType.Horizontal) parent.Transform.Size = new Vector2(contentSize, parent.Transform.Size.y);
                    if (StackType == ContentStackType.Vertical) parent.Transform.Size = new Vector2(parent.Transform.Size.x, contentSize);
                    break;
            }

            _pageSize = StackType == ContentStackType.Horizontal ? parent.Transform.Bounds.Width : parent.Transform.Bounds.Height;
            _scrollMax = contentSize - _pageSize;
            _contentSize = contentSize;
        }

        public void FullUpdateLayout()
        {
            UpdatePosition();
            parent.Invalidate();
        }
    }
}