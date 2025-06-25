using FenUISharp.Mathematics;
using FenUISharp.States;

namespace FenUISharp.Objects
{
    public class Layout : IDisposable, IStateListener
    {
        public UIObject Owner { get; init; }

        public State<Vector2> Alignment { get; init; }
        public State<Vector2> AlignmentAnchor { get; init; }

        public State<float> MarginHorizontal { get; init; }
        public State<float> MarginVertical { get; init; }
        public State<bool> StretchHorizontal { get; init; }
        public State<bool> StretchVertical { get; init; }

        public Func<Vector2, Vector2>? ProcessLayoutPositioning { get; set; }

        public Layout(UIObject owner)
        {
            this.Owner = owner;

            Alignment = new(() => new(0.5f, 0.5f), this);
            AlignmentAnchor = new(() => new(0.5f, 0.5f), this);

            MarginHorizontal = new(() => 0f, this);
            MarginVertical = new(() => 0f, this);
            StretchHorizontal = new(() => false, this);
            StretchVertical = new(() => false, this);
        }

        public void ApplyLayoutToPositioning(in Vector2 size, out Vector2 offset, out Vector2 anchorCorrection)
        {
            var anchor = AlignmentAnchor.CachedValue;
            var align = Alignment.CachedValue;

            Vector2 sizeOffset = new(size.x * anchor.x, size.y * anchor.y);
            Vector2 relativeParentPos = new Vector2(
                (Owner.Parent?.Shape.LocalBounds.Width ?? (FContext.GetCurrentWindow()?.Bounds.Width ?? 0)) * align.x,
                (Owner.Parent?.Shape.LocalBounds.Height ?? (FContext.GetCurrentWindow()?.Bounds.Height ?? 0)) * align.y);

            // A ghost is actually haunting this and I have no idea why it does what it does; edit: seems to be working now; edit 2: it did not work. DO NOT ADD THE LOCAL POSITION TO THIS THING!
            // var returnPos = relativeParentPos - sizeOffset; // Basically not needed anymore, though I'll leave it here so I get reminded of the mistakes in my past

            offset = ProcessLayoutPositioning?.Invoke(relativeParentPos) ?? relativeParentPos;
            anchorCorrection = sizeOffset;
        }

        public Vector2 ApplyLayoutToSize(Vector2 localSize)
        {
            Vector2 stretchSize = new((Owner.Parent?.Shape.LocalBounds.Width ?? (FContext.GetCurrentWindow()?.Bounds.Width ?? 0)) - MarginHorizontal.CachedValue * 2,
                (Owner.Parent?.Shape.LocalBounds.Height ?? (FContext.GetCurrentWindow()?.Bounds.Height ?? 0)) - MarginVertical.CachedValue * 2);
            
            return new(StretchHorizontal.CachedValue ? stretchSize.x : localSize.x, StretchVertical.CachedValue ? stretchSize.y : localSize.y);
        }

        public void Dispose()
        {
            Alignment.Dispose();
            AlignmentAnchor.Dispose();
            MarginHorizontal.Dispose();
            MarginVertical.Dispose();
            StretchHorizontal.Dispose();
            StretchVertical.Dispose();
        }

        public void OnInternalStateChanged<T>(T value)
        {
            Owner.Invalidate(UIObject.Invalidation.LayoutDirty);
        }
    }
}