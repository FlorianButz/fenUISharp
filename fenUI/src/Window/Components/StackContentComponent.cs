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
            parent.components.Add(scrollComponent);

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
            scrollBar.transform.marginHorizontal = 8;
            scrollBar.transform.marginVertical = 8;

            scrollBar.transform.parentIgnoreLayout = true;
            scrollBar.transform.ignoreParentOffset = true;
            scrollBar.visible = false;
            scrollBar.onPositionChanged += OnScrollbarUpdate;
            scrollBar.transform.SetParent(parent.transform);
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
            scrollBar.visible = StackBehavior == ContentStackBehavior.Scroll;
            if (StackBehavior == ContentStackBehavior.Scroll)
            {
                if (StackType == ContentStackType.Vertical)
                {
                    scrollBar.transform.localPosition = new Vector2(-8, 0);
                    scrollBar.transform.alignment = new Vector2(1, 0.5f);
                    scrollBar.transform.stretchHorizontal = false;
                    scrollBar.transform.stretchVertical = true;
                    scrollBar.HorizontalOrientation = false;
                }
                else if (StackType == ContentStackType.Horizontal)
                {
                    scrollBar.transform.localPosition = new Vector2(0, -8);
                    scrollBar.transform.alignment = new Vector2(0.5f, 1);
                    scrollBar.transform.stretchVertical = false;
                    scrollBar.transform.stretchHorizontal = true;
                    scrollBar.HorizontalOrientation = true;
                }
            }
        }

        public override void ComponentUpdate()
        {
            base.ComponentUpdate();

            if (true)
            {
                if (!AllowScrollOverflow)
                    _scrollPosition = RMath.Clamp(_scrollPosition, -_scrollMax, _scrollMin);

                _scrollDisplayPosition = ScrollSpring.Update((float)parent.WindowRoot.DeltaTime, new Vector2(_scrollPosition, 0)).x;

                if (AllowScrollOverflow)
                    _scrollPosition = RMath.Clamp(_scrollPosition, -_scrollMax, _scrollMin);

                if (StackBehavior == ContentStackBehavior.Scroll)
                {
                    if (StackType == ContentStackType.Horizontal)
                        parent.transform.childOffset = new Vector2(_scrollDisplayPosition, 0);
                    else if (StackType == ContentStackType.Vertical)
                        parent.transform.childOffset = new Vector2(0, _scrollDisplayPosition);
                }

                scrollBar.ScrollMin = -_scrollMax;
                scrollBar.ScrollMax = _scrollMin;

                scrollBar.PageSize = _pageSize;
                scrollBar.ContentSize = _contentSize;

                scrollBar.ScrollPosition = _scrollDisplayPosition;

                if (Math.Round(_lastScrollDisplayPosition) != Math.Round(_scrollDisplayPosition))
                {
                    scrollBar.UpdateScrollbar();
                    parent.Invalidate();
                }

                _lastScrollDisplayPosition = _scrollDisplayPosition;
            }
        }

        public override void OnBeforeRenderChildren(SKCanvas canvas)
        {
            base.OnBeforeRenderChildren(canvas);

            if (StackBehavior == ContentStackBehavior.Scroll)
            {
                canvas.ClipRect(parent.transform.bounds);
            }
        }

        public void UpdatePosition()
        {
            var childList = parent.transform.childs;

            float currentPos = 0;
            float contentSize = 0;
            float contentSizePerpendicular = 0;

            float lastItemSize = 0;

            for (int c = 0; c < childList.Count; c++)
            {
                if (childList[c].parentIgnoreLayout) continue; // Make sure to ignore some transforms

                lastItemSize = StackType == ContentStackType.Horizontal ? childList[c].localBounds.Width : childList[c].localBounds.Height;

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

                contentSizePerpendicular = Math.Max(StackType == ContentStackType.Horizontal ? childList[c].localBounds.Height : childList[c].localBounds.Width, contentSizePerpendicular);

                childList[c].alignment = StartAlignment;
                if (StackType == ContentStackType.Horizontal)
                    childList[c].localPosition = new Vector2(currentPos, 0);
                else
                    childList[c].localPosition = new Vector2(0, currentPos);

                currentPos += lastItemSize / 2;
                contentSize += lastItemSize / 2;
            }

            contentSize = Math.Abs(contentSize + Pad);
            contentSizePerpendicular += Pad * 2;

            switch (StackBehavior)
            {
                case ContentStackBehavior.SizeToFitAll:
                    if (StackType == ContentStackType.Horizontal) parent.transform.size = new Vector2(contentSize, contentSizePerpendicular);
                    if (StackType == ContentStackType.Vertical) parent.transform.size = new Vector2(contentSizePerpendicular, contentSize);
                    break;
                case ContentStackBehavior.SizeToFit:
                    if (StackType == ContentStackType.Horizontal) parent.transform.size = new Vector2(contentSize, parent.transform.size.y);
                    if (StackType == ContentStackType.Vertical) parent.transform.size = new Vector2(parent.transform.size.x, contentSize);
                    break;
            }

            _pageSize = StackType == ContentStackType.Horizontal ? parent.transform.bounds.Width : parent.transform.bounds.Height;
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