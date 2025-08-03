using FenUISharp.Mathematics;
using FenUISharp.Objects;
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

        protected override void Draw(SKCanvas targetCanvas, SKPath path, UIObject caller, SKPaint paint)
        {
            paint.Color = paint.Color.MultiplyMix(BaseColor());
            targetCanvas.DrawPath(path, paint);
        }
    }
}