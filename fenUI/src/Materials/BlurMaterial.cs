using FenUISharp.Objects;
using SkiaSharp;

namespace FenUISharp.Materials
{
    public class BlurMaterial : Material
    {
        public Func<SKColor> BaseColor
        {
            get => GetProp<Func<SKColor>>("BaseColor", () => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.Secondary);
            set => SetProp("BaseColor", value);
        }

        public Func<SKImage?> GrabPassFunction
        {
            get => GetProp<Func<SKImage?>>("GrabPassFunction", () => null);
            set => SetProp("GrabPassFunction", value);
        }

        public Func<float> BlurRadius
        {
            get => GetProp<Func<float>>("BlurRadius", () => 5);
            set => SetProp("BlurRadius", value);
        }

        public BlurMaterial(Func<SKImage?> GrabPassFunction)
        {
            this.GrabPassFunction = GrabPassFunction;
        }

        protected override void Draw(SKCanvas targetCanvas, SKPath path, UIObject caller, SKPaint paint)
        {
            int unmodified = targetCanvas.Save();

            using var windowArea = GrabPassFunction();

            if (windowArea == null) return;
            targetCanvas.ClipPath(path, antialias: true);

            paint.Color = BaseColor();
            targetCanvas.DrawPath(path, paint);

            using (var blur = SKImageFilter.CreateBlur(BlurRadius(), BlurRadius()))
                paint.ImageFilter = blur;

            var displayArea = caller.Shape.SurfaceDrawRect;
            targetCanvas.DrawImage(windowArea, displayArea, sampling: new(SKFilterMode.Linear, SKMipmapMode.Linear), paint);

            targetCanvas.RestoreToCount(unmodified);
        }
    }
}