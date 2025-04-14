using SkiaSharp;

namespace FenUISharp.Themes
{
    public class ThemeColor
    {
        private readonly Func<SKColor> _colorProvider;
        private SKColor? _overrideColor;

        public SKColor Value => _overrideColor ?? _colorProvider();

        public ThemeColor(Func<SKColor> colorProvider)
        {
            _colorProvider = colorProvider;
        }

        public ThemeColor(SKColor fixedColor)
        {
            _colorProvider = () => fixedColor;
            _overrideColor = fixedColor;
        }

        public void SetOverride(SKColor color)
        {
            _overrideColor = color;
        }

        public void ResetOverride()
        {
            _overrideColor = null;
        }
    }
}