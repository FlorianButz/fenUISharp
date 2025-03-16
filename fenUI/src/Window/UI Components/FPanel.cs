using SkiaSharp;

namespace FenUISharp
{
    public class FPanel : FUIComponent
    {
        protected float _cornerRadius;

        public FPanel(Vector2 position, Vector2 size, float cornerRadius, SKColor color) : base(position, size)
        {
            skPaint.Color = color;
            skPaint.ImageFilter = SKImageFilter.CreateDropShadow(2, 2, 15, 15, SKColors.Black.WithAlpha(125));
            this._cornerRadius = cornerRadius;

            transform.boundsPadding.SetValue(this, 35, 35);
        }

        protected override void DrawToSurface(SKCanvas canvas)
        {
            canvas.DrawRoundRect(transform.localBounds, _cornerRadius, _cornerRadius, skPaint);
        }
    }
}