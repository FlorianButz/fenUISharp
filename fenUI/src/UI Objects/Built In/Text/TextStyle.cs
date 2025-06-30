using FenUISharp.Themes;
using SkiaSharp;

namespace FenUISharp.Objects.Text.Model
{
    public class TextStyle
    {
        public TextStyle()
        {
            
        }

        public TextStyle(TextStyle copy)
        {
            this.Weight = copy.Weight;
            this.Slant = copy.Slant;
            this.Underlined = copy.Underlined;
            this.FontSize = copy.FontSize;
            this.Color = copy.Color;
            this.BackgroundColor = copy.BackgroundColor;
            this.BlurRadius = copy.BlurRadius;
        }

        public SKFontStyleWeight Weight { get; set; } = SKFontStyleWeight.Normal;
        public SKFontStyleSlant Slant { get; set; } = SKFontStyleSlant.Upright;
        public bool Underlined { get; set; } = false;
        public float FontSize { get; set; } = 16;
        public Func<SKColor> Color { get; set; } = new Func<SKColor>(() => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.OnSurface);
        public Func<SKColor> BackgroundColor { get; set; } = new Func<SKColor>(() => SKColors.Transparent);

        public float BlurRadius { get; set; } = 0;
        public float Opacity { get; set; } = 1;
    }
}