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

        public Func<SKRect> LocalFullBounds
        {
            get => GetProp<Func<SKRect>>("LocalFullBounds", () => SKRect.Create(0, 0, 1, 1));
            set => SetProp("LocalFullBounds", value);
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

        public BlurMaterial(Func<SKRect> LocalFullBounds, Func<SKImage?> GrabPassFunction)
        {
            this.LocalFullBounds = LocalFullBounds;
            this.GrabPassFunction = GrabPassFunction;
        }

        protected override void Draw(SKCanvas targetCanvas, SKPath path, SKPaint paint)
        {
            int unmodified = targetCanvas.Save();

            using var windowArea = GrabPassFunction();

            if (windowArea == null) return;
            targetCanvas.ClipPath(path, antialias: true);

            paint.Color = BaseColor();
            targetCanvas.DrawPath(path, paint);

            using (var blur = SKImageFilter.CreateBlur(BlurRadius(), BlurRadius()))
                paint.ImageFilter = blur;

            var displayArea = LocalFullBounds();
            targetCanvas.DrawImage(windowArea, displayArea, sampling: new(SKFilterMode.Linear, SKMipmapMode.Linear), paint);

            targetCanvas.RestoreToCount(unmodified);
        }
    }
}