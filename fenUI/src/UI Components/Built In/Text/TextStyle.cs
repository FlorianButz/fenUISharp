using SkiaSharp;

namespace FenUISharp.Components.Text.Model
{
    public class TextStyle
    {
        public SKFontStyleWeight Weight { get; set; } = SKFontStyleWeight.Normal;
        public SKFontStyleSlant Slant { get; set; } = SKFontStyleSlant.Upright;
        public bool Underlined { get; set; } = false;
        public float FontSize { get; set; } = 16;
        public SKColor Color { get; set; } = SKColors.White;

        public float BlurRadius { get; set; } = 0;
    }
}