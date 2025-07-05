using System.Diagnostics;
using FenUISharp.Mathematics;
using FenUISharp.Objects;
using FenUISharp.RuntimeEffects;
using FenUISharp.States;
using SkiaSharp;

namespace FenUISharp.Behavior
{
    public class StackContentComponent : BehaviorComponent, IStateListener
    {
        public enum ContentStackType { Horizontal, Vertical }
        public enum ContentStackBehavior { Overflow, SizeToFit, SizeToFitAll, Scroll }

        public Vector2 StartAlignment { get; set; }
        public Vector2 AlignInside { get; set; }
        public State<float> Gap { get; set; }
        public State<float> Pad { get; set; }

        public bool AllowScrollOverflow { get; set; } = true;
        public bool ContentFade { get; set; } = false;
        public bool ContentClip { get; set; } = true;
        public float FadeLength { get; set; } = 60;

        public bool EnableEdgeBlur { get; set; } = false;

        public ContentStackType StackType { get; set; }
        public ContentStackBehavior StackBehavior { get; set; }

        public ContentClipBehaviorProvider? ContentClipBehaviorProvider { get; set; }

        public Func<float, float>? SnappingProvider { get; set; } = null;
        public bool ApplySnapSeparately { get; set; } = true;

        public float ScrollSpeed { get; set; } = 0.45f;
        private float _scrollPosition = 0f;
        private float _snappedScrollPosition = 0f;
        private float _scrollDisplayPosition = 0f;
        private float _lastScrollDisplayPosition = 0f;

        private float _scrollMin = 0;
        private float _scrollMax = 0;

        public float _pageSize { get; protected set; } = 0;
        public float _contentSize { get; protected set; } = 0;

        public Spring? ScrollSpring { get; set; }
        private FScrollBar scrollBar;

        public List<Vector2> ChildLocalPosition { get; private set; } = new();

        private bool _positionDirty = false;

        /// <summary>
        /// Applies a layout to the owner's children that were added before this behavior was added
        /// </summary>
        public StackContentComponent(UIObject owner, ContentStackType type, ContentStackBehavior behavior, Vector2? startAlign = null, Vector2? alignInside = null) : base(owner)
        {
            StackType = type;
            StackBehavior = behavior;

            owner.InteractiveSurface.EnableMouseActions.Value = () => true;
            owner.InteractiveSurface.OnDragDelta += OnDrag;
            owner.InteractiveSurface.EnableMouseScrolling.Value = () => StackBehavior == ContentStackBehavior.Scroll;
            owner.InteractiveSurface.OnMouseScroll += OnScroll;

            Gap = new(() => 10, this);
            Pad = new(() => 15, this);

            ScrollSpring = new Spring(new Vector2(0, 0), 2f, 1f / 0.85f, 0.1f);

            if (startAlign == null)
            {
                if (type == ContentStackType.Horizontal) StartAlignment = new Vector2(0f, 0.5f);
                else if (type == ContentStackType.Vertical) StartAlignment = new Vector2(0.5f, 0f);
            }
            else
            {
                StartAlignment = startAlign.Value;
            }

            if (alignInside == null)
            {
                AlignInside = new Vector2(0.5f, 0.5f);
            }
            else
            {
                AlignInside = alignInside.Value;
            }

            UpdateChildDeps();
        }

        public override void ComponentDestroy()
        {
            base.ComponentDestroy();

            Owner.InteractiveSurface.OnDrag -= OnDrag;
            Owner.InteractiveSurface.OnMouseScroll -= OnScroll;

            GetAffectedChildren().ForEach(x => x.Layout.ProcessLayoutPositioning = null);

            Gap.Dispose();
            Pad.Dispose();
        }

        public Vector2 ProcessChildLayout(Vector2 i)
        {
            return i + ChildrenOffset;
        }

        public override void HandleEvent(BehaviorEventType type, object? data = null)
        {
            base.HandleEvent(type, data);

            switch (type)
            {
                case BehaviorEventType.BeforeDrawChildren:
                    OnBeforeRenderChildren((SKCanvas)data);
                    break;
                case BehaviorEventType.AfterDrawChildren:
                    OnAfterRenderChildren((SKCanvas)data);
                    break;
                case BehaviorEventType.BeforeBegin:
                    ComponentSetup();
                    break;
                case BehaviorEventType.BeforeUpdate:
                    UpdateChildDeps();
                    Update();
                    break;
                case BehaviorEventType.AfterUpdate:
                    if (_positionDirty) UpdatePosition();
                    _positionDirty = false;
                    break;
            }
        }

        List<UIObject> GetAffectedChildren()
        {
            List<UIObject> affected = new();

            Owner.Composition.GetZOrderedListOfChildren(Owner).ForEach(x =>
            {
                if (!x.BehaviorComponents.Any(x => x is LayoutObject && ((LayoutObject)x).IgnoreParentLayout.CachedValue))
                    affected.Add(x);
            });

            return affected;
        }

        void UpdateChildDeps()
        {
            GetAffectedChildren().ForEach(x => x.Layout.ProcessLayoutPositioning = ProcessChildLayout);
        }

        void InvalidateChildren()
        {
            GetAffectedChildren().ForEach(x => x.Invalidate(UIObject.Invalidation.TransformDirty));
        }

        private int? _fadeLayerSaveCount = null;
        public void OnBeforeRenderChildren(SKCanvas? canvas)
        {
            if (ContentClip) canvas?.ClipRect(Owner.Shape.LocalBounds);

            if (StackBehavior != ContentStackBehavior.Scroll) return;
            if (!ContentFade || _contentSize <= _pageSize) return;
            
            // TODO: Fix layer issue with render surface system

            // Save a layer for the children to be rendered into
            var bounds = Owner.Shape.LocalBounds;
            _fadeLayerSaveCount = canvas?.SaveLayer(bounds, null);
        }

        public void OnAfterRenderChildren(SKCanvas? canvas)
        {
            if (StackBehavior != ContentStackBehavior.Scroll) return;
            if (!ContentFade) return;
            if (_contentSize <= _pageSize || _fadeLayerSaveCount == null) return;

            var bounds = Owner.Shape.LocalBounds;

            using (var maskPaint = new SKPaint())
            {
                maskPaint.BlendMode = SKBlendMode.DstIn;

                SKPoint start = new SKPoint(0, bounds.Top);
                SKPoint end = new SKPoint(0, bounds.Bottom);

                if (StackType == ContentStackType.Horizontal)
                {
                    start = new SKPoint(bounds.Left, 0);
                    end = new SKPoint(bounds.Right, 0);
                }

                var fadeLength = (bounds.Height - (bounds.Height - FadeLength)) / bounds.Height;

                float bias(float t) => (float)Math.Pow(t, 1.5f); // Simple quadratic ease-in

                maskPaint.Shader = SKShader.CreateLinearGradient(
                    start,
                    end,
                    new SKColor[] {
                        SKColors.Transparent,
                        SKColors.Black,
                        SKColors.Black,
                        SKColors.Transparent
                    },
                    new float[] {
                        0f,
                        bias(fadeLength),
                        1 - bias(fadeLength),
                        1f
                    },
                    SKShaderTileMode.Clamp
                );

                canvas?.DrawRect(bounds, maskPaint);
            }

            if (_fadeLayerSaveCount != null)
                canvas?.RestoreToCount(_fadeLayerSaveCount.Value);
            _fadeLayerSaveCount = null;

            if (EnableEdgeBlur)
            {
                if (Owner._childSurface.TryGetSurface(out var surf))
                {
                    float l = FadeLength / 2f;
                    var topBounds = new SKRect(bounds.Left, bounds.Top, bounds.Right, bounds.Top + l);
                    var bottomBounds = new SKRect(bounds.Left, bounds.Bottom - l, bounds.Right, bounds.Bottom);

                    VariableBlur.ApplyBlur(surf, topBounds, Owner.Padding.CachedValue, new(0, 1), maxBlur: 3);
                    VariableBlur.ApplyBlur(surf, bottomBounds, Owner.Padding.CachedValue, new(0, -1), maxBlur: 3);
                }
            }
        }

        // public void OnBeforeRenderChildren(SKCanvas? canvas){}
        // public void OnAfterRenderChildren(SKCanvas? canvas)
        // {
        //     if (!ContentFade || _contentSize <= _pageSize || canvas == null) return;

        //     var bounds = Owner.Shape.SurfaceDrawRect;
        //     float fadeLength = FadeLength; // pixels

        //     using var paint = new SKPaint
        //     {
        //         IsAntialias = true,
        //         BlendMode = SKBlendMode.Clear, // for actual masking, or SrcOver for visual fade
        //     };

        //     // Top fade
        //     paint.Shader = SKShader.CreateLinearGradient(
        //         new SKPoint(0, bounds.Top),
        //         new SKPoint(0, bounds.Top + fadeLength),
        //         new[] { SKColors.Transparent, SKColors.Black },
        //         null,
        //         SKShaderTileMode.Clamp);

        //     canvas.DrawRect(new SKRect(bounds.Left, bounds.Top, bounds.Right, bounds.Top + fadeLength), paint);

        //     // Bottom fade
        //     paint.Shader = SKShader.CreateLinearGradient(
        //         new SKPoint(0, bounds.Bottom - fadeLength),
        //         new SKPoint(0, bounds.Bottom),
        //         new[] { SKColors.Black, SKColors.Transparent },
        //         null,
        //         SKShaderTileMode.Clamp);

        //     canvas.DrawRect(new SKRect(bounds.Left, bounds.Bottom - fadeLength, bounds.Right, bounds.Bottom), paint);
        // }

        public void ComponentSetup()
        {
            scrollBar = new FScrollBar(() => new Vector2(0, 0), () => new Vector2(4f, 4f));
            scrollBar.Layout.MarginHorizontal.SetStaticState(8);
            scrollBar.Layout.MarginVertical.SetStaticState(8);

            // Exclude scrollbar from being affected by layout
            new LayoutObject(scrollBar).IgnoreParentLayout.SetStaticState(true);

            scrollBar.Visible.SetStaticState(false);
            scrollBar.HorizontalOrientation.SetResponsiveState(() => StackType == ContentStackType.Horizontal);
            scrollBar.Visible.Value = () => (StackBehavior == ContentStackBehavior.Scroll && (_pageSize < _contentSize));
            scrollBar.onPositionChanged += OnScrollbarUpdate;
            scrollBar.SetParent(Owner);

            ChangeScrollbar();
        }

        private void OnScroll(float delta)
        {
            if (StackBehavior == ContentStackBehavior.Scroll)
                _scrollPosition += delta * ScrollSpeed;
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

            if (StackBehavior == ContentStackBehavior.Scroll)
            {
                if (StackType == ContentStackType.Vertical)
                {
                    scrollBar.Transform.LocalPosition.SetStaticState(new Vector2(-distance, 0));
                    scrollBar.Layout.Alignment.SetStaticState(new Vector2(1, 0.5f));
                    scrollBar.Layout.StretchHorizontal.SetStaticState(false);
                    scrollBar.Layout.StretchVertical.SetStaticState(true);
                }
                else if (StackType == ContentStackType.Horizontal)
                {
                    scrollBar.Transform.LocalPosition.SetStaticState(new Vector2(0, -distance));
                    scrollBar.Layout.Alignment.SetStaticState(new Vector2(0.5f, 1));
                    scrollBar.Layout.StretchHorizontal.SetStaticState(true);
                    scrollBar.Layout.StretchVertical.SetStaticState(false);
                }
            }
        }

        public Vector2 ChildrenOffset { get; private set; } = new(0, 0);

        public void Update()
        {
            if (!AllowScrollOverflow || ScrollSpring == null)
                _scrollPosition = RMath.Clamp(_scrollPosition, -_scrollMax, _scrollMin);

            if (SnappingProvider != null && ApplySnapSeparately)
                _snappedScrollPosition = SnappingProvider.Invoke(_scrollPosition);
            else
                _snappedScrollPosition = _scrollPosition;

            if (ScrollSpring != null && _contentSize > _pageSize)
                _scrollDisplayPosition = ScrollSpring.Update((float)FContext.GetCurrentWindow().DeltaTime, new Vector2(_snappedScrollPosition, 0)).x;
            else if (ScrollSpring == null)
                _scrollDisplayPosition = _snappedScrollPosition;
            else
                _scrollDisplayPosition = _pageSize / 2 - _contentSize / 2;

            if (AllowScrollOverflow && ScrollSpring != null)
                _scrollPosition = RMath.Clamp(_scrollPosition, -_scrollMax, _scrollMin);

            if (SnappingProvider != null && !ApplySnapSeparately)
                _scrollPosition = SnappingProvider.Invoke(_scrollPosition);

            if (StackBehavior == ContentStackBehavior.Scroll)
            {
                if (StackType == ContentStackType.Horizontal)
                    ChildrenOffset = new Vector2(_scrollDisplayPosition, 0);
                else if (StackType == ContentStackType.Vertical)
                    ChildrenOffset = new Vector2(0, _scrollDisplayPosition);

                if (_contentSize < _pageSize)
                    ChildrenOffset += new Vector2(
                        RMath.Lerp(-_pageSize / 2 + _contentSize / 2, _pageSize / 2 - _contentSize / 2, AlignInside.x),
                        RMath.Lerp(-_pageSize / 2 + _contentSize / 2, _pageSize / 2 - _contentSize / 2, AlignInside.y));
            }

            if (Math.Round(_lastScrollDisplayPosition) != Math.Round(_scrollDisplayPosition))
            {
                scrollBar?.UpdateScrollbar();
                // Owner.Invalidate(UIObject.Invalidation.LayoutDirty); // Technically not needed and also breaks a few things
                InvalidateChildren();
            }

            ProcessLayout();
            UpdateScrollbar();
        }

        public void GetClipFactors(UIObject child, in float clipStart, in float clipLength, out float startFactor, out float endFactor)
        {
            float pos = StackType == ContentStackType.Horizontal ? child.Shape.SurfaceDrawRect.MidX : child.Shape.SurfaceDrawRect.MidY;
            float parPos = (StackType == ContentStackType.Horizontal ? Owner.Transform.Position.x : Owner.Transform.Position.y) + Owner.Padding.CachedValue;

            var clipLengthCapped = Math.Clamp(clipLength, 0.01f, float.MaxValue);

            startFactor = Math.Clamp((parPos - pos + clipLength + clipStart) / clipLengthCapped, 0, 1);
            endFactor = Math.Clamp(1 - (parPos - pos + _pageSize - clipStart) / clipLengthCapped, 0, 1);
        }

        public void UpdatePosition()
        {
            var childList = GetAffectedChildren();
            ChildLocalPosition = new List<Vector2>();

            float currentPos = 0;
            float contentSize = 0;
            float contentSizePerpendicular = 0;

            for (int c = 0; c < childList.Count; c++)
            {
                var l = childList[c].BehaviorComponents.FirstOrDefault(x => x is LayoutObject);
                if (l != null && ((LayoutObject)l).IgnoreParentLayout.CachedValue) continue; // Make sure to ignore some transforms

                var lastItemSize = StackType == ContentStackType.Horizontal ? childList[c].Shape.LocalBounds.Width : childList[c].Shape.LocalBounds.Height;

                currentPos += lastItemSize / 2;
                contentSize += lastItemSize / 2;

                if (c == 0)
                {
                    currentPos += Pad.CachedValue;
                    contentSize += Pad.CachedValue;
                }
                else
                {
                    currentPos += Gap.CachedValue;
                    contentSize += Gap.CachedValue;
                }

                contentSizePerpendicular = Math.Max(StackType == ContentStackType.Horizontal ? childList[c].Shape.LocalBounds.Height : childList[c].Shape.LocalBounds.Width, contentSizePerpendicular);

                childList[c].Layout.Alignment.SetStaticState(StartAlignment);
                if (StackType == ContentStackType.Horizontal)
                    childList[c].Transform.LocalPosition.SetStaticState(new Vector2(currentPos, 0));
                else
                    childList[c].Transform.LocalPosition.SetStaticState(new Vector2(0, currentPos));


                if (!(childList.Count <= c || ChildLocalPosition.Capacity <= c))
                    ChildLocalPosition.Insert(c, childList[c].Transform.LocalPosition.CachedValue); // Since the value is statically assigned it should already be updated in the cached value

                currentPos += lastItemSize / 2;
                contentSize += lastItemSize / 2;
            }

            contentSize = Math.Abs(contentSize + Pad.CachedValue);
            contentSizePerpendicular += Pad.CachedValue * 2;

            Vector2 calculatedOwnerSize = Owner.Transform.Size.CachedValue;

            switch (StackBehavior)
            {
                case ContentStackBehavior.SizeToFitAll:
                    if (StackType == ContentStackType.Horizontal) calculatedOwnerSize = new Vector2(contentSize, contentSizePerpendicular);
                    if (StackType == ContentStackType.Vertical) calculatedOwnerSize = new Vector2(contentSizePerpendicular, contentSize);
                    break;
                case ContentStackBehavior.SizeToFit:
                    if (StackType == ContentStackType.Horizontal) calculatedOwnerSize = new Vector2(contentSize, Owner.Transform.Size.CachedValue.y);
                    if (StackType == ContentStackType.Vertical) calculatedOwnerSize = new Vector2(Owner.Transform.Size.CachedValue.x, contentSize);
                    break;
            }

            Owner.Transform.Size.Value = () => calculatedOwnerSize;

            _pageSize = StackType == ContentStackType.Horizontal ? calculatedOwnerSize.x : calculatedOwnerSize.y;
            _scrollMax = contentSize - _pageSize;
            _contentSize = contentSize;

            ProcessLayout();
        }

        void ProcessLayout()
        {
            if (ContentClipBehaviorProvider != null && StackBehavior == ContentStackBehavior.Scroll)
            {
                var childList = GetAffectedChildren();

                ContentClipBehaviorProvider.Update(this, childList);
                for (int i = 0; i < childList.Count; i++)
                {
                    if (i >= childList.Count || i >= ChildLocalPosition.Count) break;

                    var l = childList[i].BehaviorComponents.FirstOrDefault(x => x is LayoutObject);
                    if (l != null && ((LayoutObject)l).IgnoreParentLayout.CachedValue) continue; // Make sure to ignore some transforms

                    if (childList[i] == null || ChildLocalPosition[i] == null) continue;
                    childList[i].Transform.LocalPosition.SetStaticState(ChildLocalPosition[i]);

                    GetClipFactors(childList[i], ContentClipBehaviorProvider.ClipStart, ContentClipBehaviorProvider.ClipLength, out float startFactor, out float endFactor);

                    if (ContentClipBehaviorProvider.Inverse)
                    {
                        startFactor = 1f - startFactor;
                        endFactor = 1f - endFactor;
                    }

                    float t = Math.Abs(Math.Max(startFactor, endFactor));
                    if (ContentClipBehaviorProvider.Inverse) t = Math.Abs(Math.Min(startFactor, endFactor));

                    ContentClipBehaviorProvider.ClipBehavior(ContentClipBehaviorProvider.ClipEase(t), this, childList[i], i, startFactor <= endFactor);
                }
            }

            _lastScrollDisplayPosition = _scrollDisplayPosition;
        }

        void UpdateScrollbar()
        {
            if (scrollBar == null) return;

            scrollBar.ScrollMin = -_scrollMax;
            scrollBar.ScrollMax = _scrollMin;

            scrollBar.PageSize = _pageSize;
            scrollBar.ContentSize = _contentSize;

            scrollBar.ScrollPosition = _scrollDisplayPosition;
        }

        public void FullUpdateLayout()
        {
            _positionDirty = true;
            Owner.Invalidate(UIObject.Invalidation.LayoutDirty);
        }

        public void OnInternalStateChanged<T>(T value)
        {
            Owner.Invalidate(UIObject.Invalidation.LayoutDirty);
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

        public virtual void Update(StackContentComponent layout, List<UIObject> children) { }
        public abstract void ClipBehavior(float t, StackContentComponent layout, UIObject child, int childIndex, bool isBottom);
    }

    public class ScaleContentClipBehavior : ContentClipBehaviorProvider
    {
        public Vector2 DefaultScale { get; set; } = new Vector2(1, 1);
        public Vector2 ClipScale { get; set; } = new Vector2(0, 0);

        public ScaleContentClipBehavior(StackContentComponent layout) : base(layout)
        {
            Inverse = true;
        }

        public override void ClipBehavior(float t, StackContentComponent layout, UIObject child, int childIndex, bool isBottom)
        {
            child.Transform.Scale.SetStaticState(Vector2.Lerp(ClipScale, DefaultScale, t));
            child.Invalidate(UIObject.Invalidation.TransformDirty);
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
        }

        public override void ClipBehavior(float t, StackContentComponent layout, UIObject child, int childIndex, bool isBottom)
        {
            var locPos = layout.ChildLocalPosition[childIndex];

            layout.GetClipFactors(child, ClipStart - 200, ClipLength * 2, out float startFactor1, out float endFactor1);
            float tAlpha = ClipEase(Math.Abs(Math.Max(startFactor1, endFactor1)));

            layout.GetClipFactors(child, ClipStart - 100, ClipLength * 3, out float startFactor2, out float endFactor2);
            float tScale = Math.Abs(Math.Max(startFactor2, endFactor2));

            layout.GetClipFactors(child, ClipStart - SlideStart, ClipLength * 3, out float startFactor3, out float endFactor3);
            float tPos = Math.Abs(Math.Max(startFactor3, endFactor3));

            layout.GetClipFactors(child, ClipStart - SlideStart - 50, ClipLength * 3, out float startFactor4, out float endFactor4);
            float tSlide = Math.Abs(Math.Max(startFactor4, endFactor4));

            float dist = layout.StackType == StackContentComponent.ContentStackType.Horizontal ?
                (child.Transform.Position.x - layout.Owner.Transform.Position.x) :
                (child.Transform.Position.y - layout.Owner.Transform.Position.y);

            bool isInUpperHalf = layout.StackType == StackContentComponent.ContentStackType.Horizontal ?
                dist < layout._pageSize / 2 :
                dist < layout._pageSize / 2;

            float pos = layout.StackType == StackContentComponent.ContentStackType.Vertical ?
                !isInUpperHalf ? (layout._pageSize - layout.ChildrenOffset.y - DistanceFromEdge) : -layout.ChildrenOffset.y + DistanceFromEdge :
                !isInUpperHalf ? (layout._pageSize - layout.ChildrenOffset.x - DistanceFromEdge) : -layout.ChildrenOffset.x + DistanceFromEdge;

            if (FlipZIndex) isInUpperHalf = !isInUpperHalf;
            child.Composition.LocalZIndex.SetStaticState(isInUpperHalf ? 0 : -childIndex + layout.Owner.Children.Count);


            if (layout.StackType == StackContentComponent.ContentStackType.Horizontal)
                locPos.x = RMath.Lerp(locPos.x, RMath.Lerp(pos, pos + (isBottom ? SeparationDistance : -SeparationDistance), tPos), tPos);
            else
                locPos.y = RMath.Lerp(locPos.y, RMath.Lerp(pos, pos + (isBottom ? (SeparationDistance) : -(SeparationDistance)), tPos), tPos);

            // child.ImageEffect.Opacity = 1 - tAlpha; // TODO: Add back alpha
            child.Transform.Scale.SetStaticState(new Vector2(RMath.Remap(1 - tScale, 0, 1, Scale.x, 1f), RMath.Remap(1 - tScale, 0, 1, Scale.y, 1f)));

            child.Transform.LocalPosition.SetStaticState(locPos);
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
            // layout.Parent.Transform.BoundsPadding.SetValue(this, Math.Max((int)Spread, (int)PerpendicularSpread), Math.Max((int)Spread, (int)PerpendicularSpread));

            offsets = new();
        }

        public override void Update(StackContentComponent layout, List<UIObject> children)
        {
            base.Update(layout, children);

            if (offsets == null)
            {
                offsets = new List<Vector2>();
                for (int i = 0; i < children.Count; i++)
                {
                    offsets.Add(new Vector2(Random.Shared.NextSingle() * Spread - Spread / 2, -(25 + Random.Shared.NextSingle() * PerpendicularSpread)));
                }
            }
        }

        public override void ClipBehavior(float t, StackContentComponent layout, UIObject child, int childIndex, bool isBottom)
        {
            var locPos = layout.ChildLocalPosition[childIndex];

            if (layout.StackType == StackContentComponent.ContentStackType.Vertical)
                locPos = Vector2.Lerp(locPos, isBottom ? (new Vector2(0, layout._pageSize) - layout.ChildrenOffset - offsets[childIndex]) : layout.ChildrenOffset * -1 + offsets[childIndex], t);
            else
                locPos = Vector2.Lerp(locPos, isBottom ? (new Vector2(layout._pageSize, 0) - layout.ChildrenOffset - offsets[childIndex].Swapped) : layout.ChildrenOffset * -1 + offsets[childIndex].Swapped, t);

            child.Transform.LocalPosition.SetStaticState(locPos);
        }
    }
}