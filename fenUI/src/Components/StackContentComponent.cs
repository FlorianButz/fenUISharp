using System.Diagnostics;
using FenUISharp.Components;
using FenUISharp.Mathematics;
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
        private UserDragComponent? dragComponent;
        private FScrollBar scrollBar;

        public List<Vector2> ChildLocalPosition { get; private set; }

        public StackContentComponent(UIComponent parent, ContentStackType type, ContentStackBehavior behavior, Vector2? startAlign = null) : base(parent)
        {
            StackType = type;
            StackBehavior = behavior;

            scrollComponent = new UserScrollComponent(parent);
            scrollComponent.MouseScroll += OnScroll;
            dragComponent = new UserDragComponent(parent);
            dragComponent.OnDragDelta += OnDrag;

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

            if (ContentClip) canvas.ClipRect(Parent.Transform.Bounds);

            // Save a layer for the children to be rendered into
            var bounds = Parent.Transform.Bounds;
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

                SKPoint start = new SKPoint(0, Parent.Transform.Bounds.Top);
                SKPoint end = new SKPoint(0, Parent.Transform.Bounds.Bottom);

                if (StackType == ContentStackType.Horizontal)
                {
                    start = new SKPoint(Parent.Transform.Bounds.Left, 0);
                    end = new SKPoint(Parent.Transform.Bounds.Right, 0);
                }

                var fadeLength = (Parent.Transform.Bounds.Height - (Parent.Transform.Bounds.Height - FadeLength)) / Parent.Transform.Bounds.Height;

                maskPaint.Shader = SKShader.CreateLinearGradient(
                    start,
                    end,
                    new SKColor[] { SKColors.Transparent, SKColors.Black, SKColors.Black, SKColors.Transparent },
                    new float[] { 0f, fadeLength, 1 - fadeLength, 1f },
                    SKShaderTileMode.Clamp
                );

                canvas.DrawRect(Parent.Transform.Bounds, maskPaint);
            }

            if(_fadeLayerSaveCount != null)
                canvas.RestoreToCount(_fadeLayerSaveCount.Value);
            _fadeLayerSaveCount = null;
        }

        public override void ComponentSetup()
        {
            base.ComponentSetup();

            scrollBar = new FScrollBar(Parent.WindowRoot, new Vector2(0, 0), new Vector2(4f, 4f));
            scrollBar.Transform.MarginHorizontal = 8;
            scrollBar.Transform.MarginVertical = 8;

            scrollBar.Transform.ParentIgnoreLayout = true;
            scrollBar.Transform.IgnoreParentOffset = true;
            scrollBar.Visible = false;
            scrollBar.Enabled = false;
            scrollBar.onPositionChanged += OnScrollbarUpdate;
            scrollBar.Transform.SetParent(Parent.Transform);
            Parent.WindowRoot.AddUIComponent(scrollBar);

            ChangeScrollbar();
        }

        private void OnScroll(float delta)
        {
            if (StackBehavior == ContentStackBehavior.Scroll)
                _scrollPosition += delta * _scrollSpeed;
        }

        private void OnDrag(Vector2 vector)
        {
            if (StackBehavior == ContentStackBehavior.Scroll)
                _scrollPosition += StackType == ContentStackType.Horizontal ? vector.x : vector.y;
        }

        private void OnScrollbarUpdate(float value)
        {
            if (StackBehavior == ContentStackBehavior.Scroll)
                _scrollPosition = value;
        }

        public void ChangeScrollbar()
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
                _scrollDisplayPosition = ScrollSpring.Update((float)Parent.WindowRoot.DeltaTime, new Vector2(_scrollPosition, 0)).x;
            else
                _scrollDisplayPosition = _pageSize / 2 - _contentSize / 2;

            if (AllowScrollOverflow)
                _scrollPosition = RMath.Clamp(_scrollPosition, -_scrollMax, _scrollMin);

            if (StackBehavior == ContentStackBehavior.Scroll)
            {
                if (StackType == ContentStackType.Horizontal)
                    Parent.Transform.ChildOffset = new Vector2(_scrollDisplayPosition, 0);
                else if (StackType == ContentStackType.Vertical)
                    Parent.Transform.ChildOffset = new Vector2(0, _scrollDisplayPosition);
            }

            if (Math.Round(_lastScrollDisplayPosition) != Math.Round(_scrollDisplayPosition))
            {
                scrollBar.UpdateScrollbar();
                Parent.Invalidate();
            }

            ProcessLayout();
            UpdateScrollbar();
        }

        public void GetClipFactors(Transform child, in float clipStart, in float clipLength, out float startFactor, out float endFactor)
        {
            float pos = StackType == ContentStackType.Horizontal ? child.Bounds.MidX : child.Bounds.MidY;
            float parPos = (StackType == ContentStackType.Horizontal ? Parent.Transform.Position.x : Parent.Transform.Position.y) + Parent.Transform.BoundsPadding.Value;

            var clipLengthCapped = Math.Clamp(clipLength, 0.01f, float.MaxValue);

            startFactor = Math.Clamp((parPos - pos + clipLength + clipStart) / clipLengthCapped, 0, 1);
            endFactor = Math.Clamp(1 - (parPos - pos + _pageSize - clipStart) / clipLengthCapped, 0, 1);
        }

        public void UpdatePosition()
        {
            var childList = Parent.Transform.Children;
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
                    if (StackType == ContentStackType.Horizontal) Parent.Transform.Size = new Vector2(contentSize, contentSizePerpendicular);
                    if (StackType == ContentStackType.Vertical) Parent.Transform.Size = new Vector2(contentSizePerpendicular, contentSize);
                    break;
                case ContentStackBehavior.SizeToFit:
                    if (StackType == ContentStackType.Horizontal) Parent.Transform.Size = new Vector2(contentSize, Parent.Transform.Size.y);
                    if (StackType == ContentStackType.Vertical) Parent.Transform.Size = new Vector2(Parent.Transform.Size.x, contentSize);
                    break;
            }

            _pageSize = StackType == ContentStackType.Horizontal ? Parent.Transform.Bounds.Width : Parent.Transform.Bounds.Height;
            _scrollMax = contentSize - _pageSize;
            _contentSize = contentSize;

            ProcessLayout();
        }

        void ProcessLayout()
        {
            if (ContentClipBehaviorProvider != null)
            {
                var childList = Parent.Transform.Children;

                ContentClipBehaviorProvider.Update(this, childList);
                for (int i = 0; i < childList.Count; i++)
                {
                    if (childList[i].ParentIgnoreLayout) continue;
                    childList[i].LocalPosition = ChildLocalPosition[i];

                    GetClipFactors(childList[i], ContentClipBehaviorProvider.ClipStart, ContentClipBehaviorProvider.ClipLength, out float startFactor, out float endFactor);

                    if (ContentClipBehaviorProvider.Inverse)
                    {
                        startFactor = 1f - startFactor;
                        endFactor = 1f - endFactor;
                    }

                    float t = Math.Abs(Math.Max(startFactor, endFactor));
                    if (ContentClipBehaviorProvider.Inverse) t = Math.Abs(Math.Min(startFactor, endFactor));

                    ContentClipBehaviorProvider.ClipBehavior(ContentClipBehaviorProvider.ClipEase(t), this, childList[i].ParentComponent, i, startFactor <= endFactor);
                }
            }

            _lastScrollDisplayPosition = _scrollDisplayPosition;
        }

        void UpdateScrollbar()
        {
            scrollBar.ScrollMin = -_scrollMax;
            scrollBar.ScrollMax = _scrollMin;

            scrollBar.PageSize = _pageSize;
            scrollBar.ContentSize = _contentSize;

            scrollBar.ScrollPosition = _scrollDisplayPosition;
        }

        public void FullUpdateLayout()
        {
            UpdatePosition();
            Parent.Invalidate();
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
        public abstract void ClipBehavior(float t, StackContentComponent layout, UIComponent child, int childIndex, bool isBottom);
    }

    public class ScaleContentClipBehavior : ContentClipBehaviorProvider
    {
        public Vector2 DefaultScale { get; set; } = new Vector2(1, 1);
        public Vector2 ClipScale { get; set; } = new Vector2(0, 0);

        public ScaleContentClipBehavior(StackContentComponent layout) : base(layout)
        {
            Inverse = true;
        }

        public override void ClipBehavior(float t, StackContentComponent layout, UIComponent child, int childIndex, bool isBottom)
        {
            child.Transform.Scale = Vector2.Lerp(ClipScale, DefaultScale, t);
        }
    }

    public class StackContentClipBehavior : ContentClipBehaviorProvider
    {
        public float DistanceFromEdge { get; set; } = 25;

        public Vector2 Scale { get; set; } = new Vector2(1, 1) * 0.85f;
        public bool FlipZIndex { get; set; } = false;

        public float SeparationDistance { get; set; } = 25;
        public float SlideStart { get; set; } = 65;

        public StackContentClipBehavior(StackContentComponent layout) : base(layout)
        {
            layout.ContentFade = false;
            layout.ContentClip = false;
            layout.Parent.Transform.BoundsPadding.SetValue(this, 25, 25);
        }

        public override void ClipBehavior(float t, StackContentComponent layout, UIComponent child, int childIndex, bool isBottom)
        {
            var locPos = layout.ChildLocalPosition[childIndex];
            child.Transform.ClipWhenFullyOutsideParent = false;

            layout.GetClipFactors(child.Transform, ClipStart - 200, ClipLength * 2, out float startFactor1, out float endFactor1);
            float tAlpha = ClipEase(Math.Abs(Math.Max(startFactor1, endFactor1)));

            layout.GetClipFactors(child.Transform, ClipStart - 100, ClipLength * 3, out float startFactor2, out float endFactor2);
            float tScale = Math.Abs(Math.Max(startFactor2, endFactor2));

            layout.GetClipFactors(child.Transform, ClipStart - SlideStart, ClipLength * 3, out float startFactor3, out float endFactor3);
            float tPos = Math.Abs(Math.Max(startFactor3, endFactor3));

            layout.GetClipFactors(child.Transform, ClipStart - SlideStart - 50, ClipLength * 3, out float startFactor4, out float endFactor4);
            float tSlide = Math.Abs(Math.Max(startFactor4, endFactor4));

            float dist = layout.StackType == StackContentComponent.ContentStackType.Horizontal ?
                (child.Transform.Position.x - layout.Parent.Transform.Position.x) :
                (child.Transform.Position.y - layout.Parent.Transform.Position.y);

            bool isInUpperHalf = layout.StackType == StackContentComponent.ContentStackType.Horizontal ?
                dist < layout._pageSize / 2 :
                dist < layout._pageSize / 2;

            float pos = layout.StackType == StackContentComponent.ContentStackType.Vertical ?
                !isInUpperHalf ? (layout._pageSize - layout.Parent.Transform.ChildOffset.y - DistanceFromEdge) : -layout.Parent.Transform.ChildOffset.y + DistanceFromEdge :
                !isInUpperHalf ? (layout._pageSize - layout.Parent.Transform.ChildOffset.x - DistanceFromEdge) : -layout.Parent.Transform.ChildOffset.x + DistanceFromEdge;

            if (FlipZIndex) isInUpperHalf = !isInUpperHalf;
            child.Transform.ZIndex = isInUpperHalf ? 0 : -childIndex;


            if (layout.StackType == StackContentComponent.ContentStackType.Horizontal)
                locPos.x = RMath.Lerp(locPos.x, RMath.Lerp(pos, pos + (isBottom ? SeparationDistance : -SeparationDistance), tPos), tPos);
            else
                locPos.y = RMath.Lerp(locPos.y, RMath.Lerp(pos, pos + (isBottom ? (SeparationDistance) : -(SeparationDistance)), tPos), tPos);

            child.ImageEffect.Opacity = 1 - tAlpha;
            child.Transform.Scale = new Vector2(RMath.Remap(1 - tScale, 0, 1, Scale.x, 1f), RMath.Remap(1 - tScale, 0, 1, Scale.y, 1f));

            child.Transform.LocalPosition = locPos;
        }
    }

    public class RandomContentClipBehavior : ContentClipBehaviorProvider
    {
        public float Spread { get; set; } = 500;
        public float PerpendicularSpread { get; set; } = 100;

        private List<Vector2> offsets;

        public RandomContentClipBehavior(StackContentComponent layout) : base(layout)
        {
            layout.ContentFade = false;
            layout.ContentClip = false;
            layout.Parent.Transform.BoundsPadding.SetValue(this, Math.Max((int)Spread, (int)PerpendicularSpread), Math.Max((int)Spread, (int)PerpendicularSpread));
        }

        public override void Update(StackContentComponent layout, List<Transform> children)
        {
            base.Update(layout, children);

            if (offsets == null)
            {
                offsets = new List<Vector2>();
                for (int i = 0; i < layout.Parent.Transform.Children.Count; i++)
                {
                    layout.Parent.Transform.Children[i].ClipWhenFullyOutsideParent = false;
                    offsets.Add(new Vector2(Random.Shared.NextSingle() * Spread - Spread / 2, -(25 + Random.Shared.NextSingle() * PerpendicularSpread)));
                }
            }
        }

        public override void ClipBehavior(float t, StackContentComponent layout, UIComponent child, int childIndex, bool isBottom)
        {
            var locPos = layout.ChildLocalPosition[childIndex];

            if (layout.StackType == StackContentComponent.ContentStackType.Vertical)
                locPos = Vector2.Lerp(locPos, isBottom ? (new Vector2(0, layout._pageSize) - layout.Parent.Transform.ChildOffset - offsets[childIndex]) : layout.Parent.Transform.ChildOffset * -1 + offsets[childIndex], t);
            else
                locPos = Vector2.Lerp(locPos, isBottom ? (new Vector2(layout._pageSize, 0) - layout.Parent.Transform.ChildOffset - offsets[childIndex].Swapped) : layout.Parent.Transform.ChildOffset * -1 + offsets[childIndex].Swapped, t);

            child.Transform.LocalPosition = locPos;
        }
    }
}