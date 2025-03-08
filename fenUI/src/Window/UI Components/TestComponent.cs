using SkiaSharp;

namespace FenUISharp
{
    public class TestComponent : UIComponentStatic
    {
        public TestComponent(float x, float y, float width, float height) : base(x, y, width, height)
        {
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
        }

        protected override void DrawToSurface(SKCanvas canvas)
        {
            Console.WriteLine("Test");
            canvas.DrawRect(transform.localBounds, skPaint);
        }
    }
}