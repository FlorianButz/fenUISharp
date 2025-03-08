using SkiaSharp;

namespace FenUISharp
{
    public abstract class UIComponentStatic : UIComponent
    {
        protected SKSurface? cachedSurface = null;

        protected UIComponentStatic(float x, float y, float width, float height) : base(x, y, width, height)
        {

        }

        public override void DrawToScreen(SKCanvas canvas)
        {
            var bounds = transform.fullBounds;
            if (cachedSurface == null)
            {
                // Create an offscreen surface for this component
                cachedSurface = SKSurface.Create(new SKImageInfo((int)Math.Ceiling(bounds.Width), (int)Math.Ceiling(bounds.Height)));
                DrawToSurface(cachedSurface.Canvas);
            }

            // Draw the cached surface onto the main canvas
            canvas.DrawImage(cachedSurface.Snapshot(), transform.position.x, transform.position.y);
        }

        public void Invalidate()
        {
            cachedSurface?.Dispose();
            cachedSurface = null; // Mark for redraw
        }

        protected abstract void DrawToSurface(SKCanvas canvas);
    }
}