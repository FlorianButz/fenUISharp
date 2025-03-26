using FenUISharp.Themes;
using SkiaSharp;

namespace FenUISharp
{
    public class FPanel : UIComponent
    {
        protected float _cornerRadius;
        public float CornerRadius { get => _cornerRadius; set { _cornerRadius = value; Invalidate(); } }

        private ThemeColor _panelColor;
        public ThemeColor PanelColor { get => _panelColor; set { _panelColor = value; Invalidate(); } }

        private ThemeColor _borderColor;
        public ThemeColor BorderColor { get => _borderColor; set { _borderColor = value; Invalidate(); } }

        private float _borderSize = 2;
        public float BorderSize { get => _borderSize; set { _borderSize = value; Invalidate(); } }

        public FPanel(Window root, Vector2 position, Vector2 size, float cornerRadius, ThemeColor? color = null) : base(root, position, size)
        {
            _borderColor = new ThemeColor(SKColors.Transparent);
            _panelColor = color ?? WindowRoot.WindowThemeManager.GetColor(t => t.Surface);

            skPaint.ImageFilter = SKImageFilter.CreateDropShadow(0, 2, 5, 5, WindowRoot.WindowThemeManager.GetColor(t => t.Shadow).Value);
            this._cornerRadius = cornerRadius;

            transform.boundsPadding.SetValue(this, 35, 35);
        }

        protected override void DrawToSurface(SKCanvas canvas)
        {
            skPaint.Color = _panelColor.Value;
            canvas.DrawRoundRect(transform.localBounds, _cornerRadius, _cornerRadius, skPaint);

            using(var strokePaint = skPaint.Clone()){
                strokePaint.IsStroke = true;
                strokePaint.Color = _borderColor.Value;
                strokePaint.StrokeWidth = _borderSize;

                strokePaint.StrokeCap = SKStrokeCap.Round;
                strokePaint.StrokeJoin = SKStrokeJoin.Round;

                canvas.DrawRoundRect(transform.localBounds, _cornerRadius, _cornerRadius, strokePaint);
            }
        }
    }
}