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
        public bool ContentFade { get; set; } = true;
        public bool ContentClip { get; set; } = true;
        public float FadeLength { get; set; } = 15f;

        public ContentStackType StackType { get; set; }
        public ContentStackBehavior StackBehavior { get; set; }

        public ContentClipBehaviorProvider? ContentClipBehaviorProvider;

        private float _scrollSpeed = 0.25f;
        private float _scrollPosition = 0f;
        private float _scrollDisplayPosition = 0f;
        private float _lastScrollDisplayPosition = 0f;

        private float _scrollMin = 0;
        private float _scrollMax = 0;

        public float _pageSize { get; protected set; } = 0;
        public float _contentSize { get; protected set; } = 0;

        public Spring? ScrollSpring { get; set; }
        private UserScrollComponent? scrollComponent;
        private FScrollBar scrollBar;

        public List<Vector2> ChildLocalPosition { get; private set; }

        public StackContentComponent(UIComponent parent, ContentStackType type, ContentStackBehavior behavior, Vector2? startAlign = null) : base(parent)
        {
            StackType = type;
            StackBehavior = behavior;

            scrollComponent = new UserScrollComponent(parent);
            scrollComponent.MouseScroll += OnScroll;

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

        private int? _fadeLayerSaveCount = null;

        public override void OnBeforeRenderChildren(SKCanvas canvas)
        {
            base.OnBeforeRenderChildren(canvas);

            if (StackBehavior != ContentStackBehavior.Scroll) return;
            if (!ContentFade || _contentSize <= _pageSize) return;

            if(ContentClip) canvas.ClipRect(parent.Transform.Bounds);

            // Save a layer for the children to be rendered into
            var bounds = parent.Transform.Bounds;
            _fadeLayerSaveCount = canvas.SaveLayer(bounds, null);
        }

        public override void OnAfterRenderChildren(SKCanvas canvas)
        {
            base.OnAfterRender(canvas);

            if (StackBehavior != ContentStackBehavior.Scroll) return;
            if (!ContentFade) return;
            if (_contentSize <= _pageSize || _fadeLayerSaveCount == null) return;

            using (var maskPaint = new SKPaint())
            {
                maskPaint.BlendMode = SKBlendMode.DstIn;

                SKPoint start = new SKPoint(0, parent.Transform.Bounds.Top);
                SKPoint end = new SKPoint(0, parent.Transform.Bounds.Bottom);

                if (StackType == ContentStackType.Horizontal)
                {
                    start = new SKPoint(parent.Transform.Bounds.Left, 0);
                    end = new SKPoint(parent.Transform.Bounds.Right, 0);
                }

                var fLen = (parent.Transform.Bounds.Height - (parent.Transform.Bounds.Height - FadeLength)) / parent.Transform.Bounds.Height;

                maskPaint.Shader = SKShader.CreateLinearGradient(
                    start,
                    end,
                    new SKColor[] { SKColors.Transparent, SKColors.Black, SKColors.Black, SKColors.Transparent },
                    new float[] { 0f, fLen, 1 - fLen, 1f },
                    SKShaderTileMode.Clamp
                );

                canvas.DrawRect(parent.Transform.Bounds, maskPaint);
            }

            canvas.RestoreToCount(_fadeLayerSaveCount.Value);
            _fadeLayerSaveCount = null;
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
            scrollBar.Enabled = false;
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
            const float distance = 5;

            scrollBar.Visible = StackBehavior == ContentStackBehavior.Scroll;
            scrollBar.Enabled = StackBehavior == ContentStackBehavior.Scroll;
            if (StackBehavior == ContentStackBehavior.Scroll)
            {
                if (StackType == ContentStackType.Vertical)
                {
                    scrollBar.Transform.LocalPosition = new Vector2(-distance, 0);
                    scrollBar.Transform.Alignment = new Vector2(1, 0.5f);
                    scrollBar.Transform.StretchHorizontal = false;
                    scrollBar.Transform.StretchVertical = true;
                    scrollBar.HorizontalOrientation = false;
                }
                else if (StackType == ContentStackType.Horizontal)
                {
                    scrollBar.Transform.LocalPosition = new Vector2(0, -distance);
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

            if (_contentSize > _pageSize)
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

            if (ContentClipBehaviorProvider != null)
            {
                var childList = parent.Transform.Children;
                ContentClipBehaviorProvider.Update(this, childList);
                for (int i = 0; i < childList.Count; i++)
                {
                    if (childList[i].ParentIgnoreLayout) continue;
                    childList[i].LocalPosition = ChildLocalPosition[i];

                    Vector2 pos = new(childList[i].Bounds.MidX, childList[i].Bounds.MidY);
                    float parPos = parent.Transform.Position.y + parent.Transform.BoundsPadding.Value;

                    float startFactor = Math.Clamp((parPos - pos.y + ContentClipBehaviorProvider.ClipLength + ContentClipBehaviorProvider.ClipStart) / ContentClipBehaviorProvider.ClipLength, 0, 1);
                    float endFactor = Math.Clamp(1 - (parPos - pos.y + _pageSize - ContentClipBehaviorProvider.ClipStart) / ContentClipBehaviorProvider.ClipLength, 0, 1);

                    if (ContentClipBehaviorProvider.Inverse)
                    {
                        startFactor = 1f - startFactor;
                        endFactor = 1f - endFactor;
                    }

                    float t = Math.Abs(Math.Max(startFactor, endFactor));
                    if (ContentClipBehaviorProvider.Inverse) t = Math.Abs(Math.Min(startFactor, endFactor));

                    ContentClipBehaviorProvider.ClipBehavior(ContentClipBehaviorProvider.ClipEase(t), this, childList[i].ParentComponent, i, (startFactor <= endFactor));
                }
            }

            _lastScrollDisplayPosition = _scrollDisplayPosition;

            scrollBar.ScrollMin = -_scrollMax;
            scrollBar.ScrollMax = _scrollMin;

            scrollBar.PageSize = _pageSize;
            scrollBar.ContentSize = _contentSize;

            scrollBar.ScrollPosition = _scrollDisplayPosition;
        }

        public void UpdatePosition()
        {
            var childList = parent.Transform.Children;
            ChildLocalPosition = new List<Vector2>();

            float currentPos = 0;
            float contentSize = 0;
            float contentSizePerpendicular = 0;

            for (int c = 0; c < childList.Count; c++)
            {
                if (childList[c].ParentIgnoreLayout) continue; // Make sure to ignore some transforms

                var lastItemSize = StackType == ContentStackType.Horizontal ? childList[c].LocalBounds.Width : childList[c].LocalBounds.Height;

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

                ChildLocalPosition.Insert(c, childList[c].LocalPositionExcludeBounds);

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

    public abstract class ContentClipBehaviorProvider
    {
        public float ClipStart { get; set; } = 0;
        public float ClipLength { get; set; } = 35;
        public Func<float, float> ClipEase { get; set; } = Easing.EaseOutCubic;
        public bool Inverse { get; set; } = false;

        protected StackContentComponent Layout;

        public ContentClipBehaviorProvider(StackContentComponent layout)
        {
            Layout = layout;
        }

        public virtual void Update(StackContentComponent layout, List<Transform> children) { }
        public abstract void ClipBehavior(float t, StackContentComponent layout, UIComponent child, int childIndex, bool isTop);
    }

    public class ScaleContentClipBehavior : ContentClipBehaviorProvider
    {
        public Vector2 DefaultScale { get; set; } = new Vector2(1, 1);
        public Vector2 ClipScale { get; set; } = new Vector2(0, 0);

        public ScaleContentClipBehavior(StackContentComponent layout) : base(layout)
        {
            Inverse = true;
        }

        public override void ClipBehavior(float t, StackContentComponent layout, UIComponent child, int childIndex, bool isTop)
        {
            child.Transform.Scale = RMath.Lerp(ClipScale, DefaultScale, t);
        }
    }

    public class StackContentClipBehavior : ContentClipBehaviorProvider
    {
        public float DistanceFromEdge { get; set; } = 15;

        public StackContentClipBehavior(StackContentComponent layout) : base(layout) { }

        public override void ClipBehavior(float t, StackContentComponent layout, UIComponent child, int childIndex, bool isTop)
        {
            var locPos = layout.ChildLocalPosition[childIndex];

            locPos.y = RMath.Lerp(locPos.y, isTop ? (layout._pageSize - layout.parent.Transform.ChildOffset.y - DistanceFromEdge) : -layout.parent.Transform.ChildOffset.y + DistanceFromEdge, t);

            child.Transform.LocalPosition = locPos;
        }
    }

    public class RandomContentClipBehavior : ContentClipBehaviorProvider
    {
        private List<Vector2> offsets;

        public RandomContentClipBehavior(StackContentComponent layout) : base(layout)
        {
            layout.ContentFade = false;
            layout.ContentClip = false;
            layout.parent.Transform.BoundsPadding.SetValue(this, 350, 100);
        }

        public override void Update(StackContentComponent layout, List<Transform> children)
        {
            base.Update(layout, children);

            if(offsets == null){
                offsets = new List<Vector2>();
                for (int i = 0; i < layout.parent.Transform.Children.Count; i++)
                {
                    layout.parent.Transform.Children[i].ClipWhenFullyOutsideParent = false;
                    offsets.Add(new Vector2(Random.Shared.NextSingle() * 1000 - 500, -(25 + Random.Shared.NextSingle() * 85)));
                }
            }
        }

        public override void ClipBehavior(float t, StackContentComponent layout, UIComponent child, int childIndex, bool isTop)
        {
            var locPos = layout.ChildLocalPosition[childIndex];

            locPos = RMath.Lerp(locPos, isTop ? (new Vector2(0, layout._pageSize) - layout.parent.Transform.ChildOffset - offsets[childIndex]) : layout.parent.Transform.ChildOffset * -1 + offsets[childIndex], t);

            child.Transform.LocalPosition = locPos;
        }
    }
}