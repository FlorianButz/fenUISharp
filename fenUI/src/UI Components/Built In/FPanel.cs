using FenUISharp.Mathematics;
using FenUISharp.Themes;
using SkiaSharp;

namespace FenUISharp.Components
{
    public class FPanel : UIComponent
    {
        public float CornerRadius { get; set; }
        public float DropShadowRadius { get; set; } = 5;

        public ThemeColor PanelColor { get; set; }
        public ThemeColor ShadowColor { get; set; }
        public ThemeColor BorderColor { get; set; }

        public float BorderSize { get; set; } = 0.5f;
        public bool UseSquircle { get; set; } = true;

        protected bool _drawBasePanel = true;

        public FPanel(Window root, Vector2 position, Vector2 size, float cornerRadius, ThemeColor? color = null) : base(root, position, size)
        {
            BorderColor = new ThemeColor(SKColors.Transparent);
            ShadowColor = WindowRoot.WindowThemeManager.GetColor(t => t.Shadow);
            PanelColor = color ?? WindowRoot.WindowThemeManager.GetColor(t => t.Surface);

            this.CornerRadius = cornerRadius;

            Transform.BoundsPadding.SetValue(this, 35, 35);
        }

        protected override void DrawToSurface(SKCanvas canvas)
        {
            SkPaint.Color = PanelColor.Value;

            using (var dropShadow = SKImageFilter.CreateDropShadow(0, 2, DropShadowRadius, DropShadowRadius, ShadowColor.Value))
                SkPaint.ImageFilter = dropShadow;
                
            if (_drawBasePanel)
            {
                if (UseSquircle)
                    canvas.DrawPath(SKSquircle.CreateSquircle(Transform.LocalBounds, CornerRadius), SkPaint);
                else
                    canvas.DrawRoundRect(Transform.LocalBounds, CornerRadius, CornerRadius, SkPaint);
            }

            using (var strokePaint = SkPaint.Clone())
            {
                strokePaint.IsStroke = true;
                strokePaint.Color = BorderColor.Value;
                strokePaint.StrokeWidth = BorderSize;

                strokePaint.StrokeCap = SKStrokeCap.Round;
                strokePaint.StrokeJoin = SKStrokeJoin.Round;

                var strokeRect = SKRect.Create((float)Math.Round(Transform.LocalBounds.Left) + 0.5f, (float)Math.Round(Transform.LocalBounds.Top) + 0.5f, Transform.LocalBounds.Width, Transform.LocalBounds.Height);

                if (UseSquircle)
                    canvas.DrawPath(SKSquircle.CreateSquircle(strokeRect, CornerRadius), strokePaint);
                else
                    canvas.DrawRoundRect(strokeRect, CornerRadius, CornerRadius, strokePaint);
            }
        }
    }
}