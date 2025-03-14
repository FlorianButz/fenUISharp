using SkiaSharp;

namespace FenUISharp
{
    public class FPanel : FUIComponent
    {
        public FPanel(Vector2 position, Vector2 size, SKColor color) : base(position, size)
        {
            skPaint.Color = color;
            skPaint.ImageFilter = SKImageFilter.CreateDropShadow(2, 2, 15, 15, SKColors.Black.WithAlpha(125));
        
            transform.boundsPadding.SetValue(this, 35, 35);
        }

        protected override void DrawToSurface(SKCanvas canvas)
        {
            canvas.DrawRoundRect(transform.localBounds, 15, 15, skPaint);
        }
    }
}