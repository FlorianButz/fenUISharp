using FenUISharp.Behavior.Layout;
using FenUISharp.Mathematics;
using FenUISharp.States;

namespace FenUISharp.Objects
{
    public class Layout : IDisposable, IStateListener
    {
        public WeakReference<UIObject> Owner { get; private set; }

        public State<Vector2> Alignment { get; init; }
        public State<Vector2> AlignmentAnchor { get; init; }

        public State<float> MaxWidth { get; init; }
        public State<float> MaxHeight { get; init; }

        public State<float> MinWidth { get; init; }
        public State<float> MinHeight { get; init; }

        public State<float> MarginHorizontal { get; init; }
        public State<float> MarginVertical { get; init; }
        public State<Vector2> AbsoluteMarginHorizontal { get; init; }
        public State<Vector2> AbsoluteMarginVertical { get; init; }
        public State<bool> StretchHorizontal { get; init; }
        public State<bool> StretchVertical { get; init; }

        public Func<Vector2, Vector2>? ProcessLayoutPositioning { get; set; }

        public Layout(UIObject owner)
        {
            this.Owner = new(owner);

            MaxWidth = new(() => float.MaxValue, owner, this);
            MaxHeight = new(() => float.MaxValue, owner, this);

            MinWidth = new(() => 0, owner, this);
            MinHeight = new(() => 0, owner, this);

            Alignment = new(() => new(0.5f, 0.5f), owner, this);
            AlignmentAnchor = new(() => new(0.5f, 0.5f), owner, this);

            MarginHorizontal = new(() => 0f, owner, this);
            MarginVertical = new(() => 0f, owner, this);
            AbsoluteMarginHorizontal = new(() => new(0f, 0f), owner, this);
            AbsoluteMarginVertical = new(() => new(0f, 0f), owner, this);
            StretchHorizontal = new(() => false, owner, this);
            StretchVertical = new(() => false, owner, this);
        }

        public void ApplyLayoutToPositioning(in Vector2 size, out Vector2 offset, out Vector2 anchorCorrection)
        {
            if (Owner.TryGetTarget(out var owner))
            {
                var anchor = AlignmentAnchor.CachedValue;
                var align = Alignment.CachedValue;

                var clampedSize = ClampSize(size);

                // Vector2 absoluteMarginCorrection = new(-AbsoluteMarginHorizontal.CachedValue.y * (Alignment.CachedValue.x - 0.5f) * 2, -AbsoluteMarginVertical.CachedValue.y * (Alignment.CachedValue.y - 0.5f) * 2);
                Vector2 absoluteMarginCorrection = new(
                    AbsoluteMarginHorizontal.CachedValue.x * (1 - align.x) - AbsoluteMarginHorizontal.CachedValue.y * align.x,
                    AbsoluteMarginVertical.CachedValue.x * (1 - align.y) - AbsoluteMarginVertical.CachedValue.y * align.y);

                Vector2 sizeOffset = new(clampedSize.x * anchor.x, clampedSize.y * anchor.y);
                Vector2 relativeParentPos = new Vector2(
                    (owner.Parent?.Shape.LocalBounds.Width ?? (FContext.GetCurrentWindow()?.Shape.Bounds.Width ?? 0)) * align.x + absoluteMarginCorrection.x,
                    (owner.Parent?.Shape.LocalBounds.Height ?? (FContext.GetCurrentWindow()?.Shape.Bounds.Height ?? 0)) * align.y + absoluteMarginCorrection.y);

                // A ghost is actually haunting this and I have no idea why it does what it does; edit: seems to be working now; edit 2: it did not work. DO NOT ADD THE LOCAL POSITION TO THIS THING!
                // var returnPos = relativeParentPos - sizeOffset; // Basically not needed anymore, though I'll leave it here so I get reminded of the mistakes from my past

                offset = ProcessLayoutPositioning?.Invoke(relativeParentPos) ?? relativeParentPos;
                anchorCorrection = sizeOffset;

                return;
            }

            offset = Vector2.Zero;
            anchorCorrection = Vector2.Zero;
        }

        public Vector2 ApplyLayoutToSize(Vector2 localSize)
        {
            if (Owner.TryGetTarget(out var owner))
            {
                var clampedSize = ClampSize(localSize);

                Vector2 stretchSize = new((owner.Parent?.Shape.LocalBounds.Width ?? (FContext.GetCurrentWindow()?.Shape.Bounds.Width ?? 0)) - MarginHorizontal.CachedValue * 2,
                    (owner.Parent?.Shape.LocalBounds.Height ?? (FContext.GetCurrentWindow()?.Shape.Bounds.Height ?? 0)) - MarginVertical.CachedValue * 2);

                var absoluteCorrection = new Vector2(AbsoluteMarginHorizontal.CachedValue.x + AbsoluteMarginHorizontal.CachedValue.y, AbsoluteMarginVertical.CachedValue.x + AbsoluteMarginVertical.CachedValue.y);

                return ClampSize(new Vector2(StretchHorizontal.CachedValue ? stretchSize.x : clampedSize.x, StretchVertical.CachedValue ? stretchSize.y : clampedSize.y) - absoluteCorrection);
            }

            return Vector2.Zero;
        }

        public void RecursivelyUpdateLayout()
        {
            if (Owner.TryGetTarget(out var owner))
                owner.RecursiveInvalidate(UIObject.Invalidation.LayoutDirty);
        }



        public Vector2 ClampSize(in Vector2 size)
        {
            return Vector2.Clamp(size, new(MinWidth.CachedValue, MinHeight.CachedValue), new(MaxWidth.CachedValue, MaxHeight.CachedValue));
        }

        public void OnInternalStateChanged<T>(T value)
        {
            if (Owner.TryGetTarget(out var owner))
                owner.Invalidate(UIObject.Invalidation.LayoutDirty);
        }

        public void Dispose()
        {
            Owner = null;
        }
    }
}