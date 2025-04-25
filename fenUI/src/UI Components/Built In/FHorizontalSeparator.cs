using FenUISharp.Mathematics;
using FenUISharp.Themes;
using SkiaSharp;

namespace FenUISharp.Components
{
    public class FHorizontalSeparator : UIComponent
    {
        public ThemeColor BaseColor { get; set; }

        public FHorizontalSeparator(Window rootWindow, Transform layoutParent) : base(rootWindow, new(0, 0), new(0, 2))
        {
            Transform.SetParent(layoutParent);
            Transform.StretchHorizontal = true;

            BaseColor = rootWindow.WindowThemeManager.GetColor(t => t.Surface);
        }

        protected override void DrawToSurface(SKCanvas canvas)
        {
            SkPaint.Color = BaseColor.Value;
            canvas.DrawRoundRect(new(Transform.LocalBounds, 5), SkPaint);
        }
    }
}