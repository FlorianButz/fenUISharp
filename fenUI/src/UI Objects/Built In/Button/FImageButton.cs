using FenUISharp.Behavior;
using FenUISharp.Mathematics;
using FenUISharp.Objects.Buttons;
using FenUISharp.States;
using SkiaSharp;

namespace FenUISharp.Objects
{
    public class FDisplayButton : Button, IStateListener
    {
        public FDisplayableType Display { get; protected set; }

        float minWidth = 0;
        float maxWidth = 0;

        public FDisplayButton(FDisplayableType display, Action? onClick = null, Func<Vector2>? position = null, Func<Vector2>? size = null) : base(onClick, position, size)
        {
            this.OnClick = onClick;

            this.maxWidth = RMath.Clamp(maxWidth, minWidth, float.MaxValue);
            this.minWidth = RMath.Clamp(minWidth, 0, this.maxWidth);

            Display = display;

            Display.SetParent(this);
            Display.Layout.StretchHorizontal.SetStaticState(true);
            Display.Layout.StretchVertical.SetStaticState(true);

            Padding.SetStaticState(10);
        }
    }
}