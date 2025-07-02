using FenUISharp.Mathematics;
using FenUISharp.Objects;
using SkiaSharp;

namespace FenUISharp
{
    public class FSeparator : UIObject
    {
        public FSeparator(bool isHorizontal = true) : base(null, () => new(2, 2))
        {
            if (isHorizontal)
                Layout.StretchHorizontal.SetStaticState(true);
            else
                Layout.StretchVertical.SetStaticState(true);
        }

        public override void Render(SKCanvas canvas)
        {
            base.Render(canvas);

            using var roundRect = new SKRoundRect(Shape.LocalBounds, 2);
            using var paint = GetRenderPaint();

            RenderMaterial.CachedValue.DrawWithMaterial(canvas, roundRect, this, paint);
        }
    }
}