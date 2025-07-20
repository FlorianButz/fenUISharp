using FenUISharp.Mathematics;
using SkiaSharp;

namespace FenUISharp.Objects
{
    public class Shape
    {
        public WeakReference<UIObject> Owner { get; init; }

        public SKRect LocalBounds { get; private set; } = new();
        public SKRect GlobalBounds { get; private set; } = new();
        public SKRect SurfaceDrawRect { get; private set; } = new();

        // Needed for invalidation path
        public SKRect LastGlobalBounds { get; private set; } = new();

        public Shape(UIObject owner)
        {
            Owner = new(owner);
        }

        public void UpdateShape()
        {
            if (Owner.TryGetTarget(out var owner))
            {
                var size = owner.Layout.ApplyLayoutToSize(owner.Transform.Size.CachedValue);
                LocalBounds = new(0, 0, size.x, size.y);
                SurfaceDrawRect = new(-owner.Padding.CachedValue, -owner.Padding.CachedValue,
                    size.x + owner.Padding.CachedValue, size.y + owner.Padding.CachedValue);

                LastGlobalBounds = GlobalBounds;
                GlobalBounds = owner.Transform.DrawLocalToGlobal(SurfaceDrawRect);
            }
        }
    }
}