using SkiaSharp;

namespace FenUISharp.Materials
{
    public class EmptyDefaultMaterial : Material
    {
        public Func<SKColor> BaseColor
        {
            get => GetProp<Func<SKColor>>("BaseColor", () => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.Secondary);
            set => SetProp("BaseColor", value);
        }

        protected override void Draw(SKCanvas targetCanvas, SKPath path, SKPaint paint)
        {
            paint.Color = BaseColor();
            targetCanvas.DrawPath(path, paint);
        }
    }
}