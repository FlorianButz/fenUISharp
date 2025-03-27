using FenUISharp.Themes;
using SkiaSharp;

namespace FenUISharp
{
    public class FPanel : UIComponent
    {
        public float CornerRadius { get; set; }

        public ThemeColor PanelColor { get; set; }

        public ThemeColor BorderColor { get; set; }

        public float BorderSize { get; set; } = 2;

        public FPanel(Window root, Vector2 position, Vector2 size, float cornerRadius, ThemeColor? color = null) : base(root, position, size)
        {
            BorderColor = new ThemeColor(SKColors.Transparent);
            PanelColor = color ?? WindowRoot.WindowThemeManager.GetColor(t => t.Surface);

            skPaint.ImageFilter = SKImageFilter.CreateDropShadow(0, 2, 5, 5, WindowRoot.WindowThemeManager.GetColor(t => t.Shadow).Value);
            this.CornerRadius = cornerRadius;

            transform.boundsPadding.SetValue(this, 35, 35);
        }

        protected override void DrawToSurface(SKCanvas canvas)
        {
            skPaint.Color = PanelColor.Value;
            canvas.DrawRoundRect(transform.localBounds, CornerRadius, CornerRadius, skPaint);

            using(var strokePaint = skPaint.Clone()){
                strokePaint.IsStroke = true;
                strokePaint.Color = BorderColor.Value;
                strokePaint.StrokeWidth = BorderSize;

                strokePaint.StrokeCap = SKStrokeCap.Round;
                strokePaint.StrokeJoin = SKStrokeJoin.Round;

                canvas.DrawRoundRect(transform.localBounds, CornerRadius, CornerRadius, strokePaint);
            }
        }
    }
}