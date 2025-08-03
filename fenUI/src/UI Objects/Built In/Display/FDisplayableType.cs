using FenUISharp.Mathematics;
using FenUISharp.States;
using SkiaSharp;

namespace FenUISharp.Objects
{
    // FDisplayableTypes are UIObjects that display symbols, images, etc..
    public class FDisplayableType : FPanel
    {
        public State<SKColor> TintColor { get; private init; }
        public State<SKBlendMode> TintBlendMode { get; private init; }

        public FDisplayableType(Func<Vector2>? position = null, Func<Vector2>? size = null, bool dynamicColor = false, float? cornerRadius = null, Func<SKColor>? color = null) : base(position, size, cornerRadius, color)
        {
            TintBlendMode = new(() => SKBlendMode.Modulate, this, this);
            TintColor = new(() => dynamicColor ? FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.OnSurface : SKColors.White, this, this);
        }
    }
}