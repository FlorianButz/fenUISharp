using FenUISharp.Mathematics;
using SkiaSharp;

namespace FenUISharp.Objects
{
    public class Shape
    {
        public UIObject Owner { get; init; }

        public SKRect LocalBounds { get; private set; } = new();
        public SKRect GlobalBounds { get; private set; } = new();
        public SKRect SurfaceDrawRect { get; private set; } = new();

        public Shape(UIObject owner)
        {
            this.Owner = owner;
        }

        public void UpdateShape()
        {
            var size = Owner.Layout.ApplyLayoutToSize(Owner.Transform.Size.CachedValue);

            LocalBounds = new(0, 0, size.x, size.y);
            SurfaceDrawRect = new(-Owner.Padding.CachedValue, -Owner.Padding.CachedValue,
                size.x + Owner.Padding.CachedValue, size.y + Owner.Padding.CachedValue);
            
            GlobalBounds = Owner.Transform.DrawLocalToGlobal(SurfaceDrawRect);
        }
    }
}