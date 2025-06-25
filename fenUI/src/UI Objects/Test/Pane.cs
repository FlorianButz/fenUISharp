using FenUISharp.Mathematics;
using SkiaSharp;

namespace FenUISharp.Objects
{
    public class Pane : UIObject
    {
        public Pane(Func<Vector2>? position = null, Func<Vector2>? size = null) : base(position, size)
        {
            
        }

        public override void Render(SKCanvas? canvas)
        {
            base.Render(canvas);
            if (canvas == null) return;

            Console.WriteLine("test");

            using var paint = GetRenderPaint();
            canvas.DrawRect(Shape.LocalBounds, paint);
            // canvas.DrawCircle(new(Shape.LocalBounds.Left, Shape.LocalBounds.Top), 100, paint);
        }
    }
}