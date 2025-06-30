using FenUISharp.Behavior;
using FenUISharp.Mathematics;
using FenUISharp.Objects.Buttons;
using FenUISharp.States;
using SkiaSharp;

namespace FenUISharp.Objects
{
    public class FImageButton : Button, IStateListener
    {
        public FImage Image { get; protected set; }

        float minWidth = 0;
        float maxWidth = 0;

        public FImageButton(FImage image, Action? onClick = null, Func<Vector2>? position = null, Func<Vector2>? size = null) : base(onClick, position, size)
        {
            this.OnClick = onClick;

            this.maxWidth = RMath.Clamp(maxWidth, minWidth, float.MaxValue);
            this.minWidth = RMath.Clamp(minWidth, 0, this.maxWidth);

            Image = image;

            Image.SetParent(this);
            Image.Layout.StretchHorizontal.SetStaticState(true);
            Image.Layout.StretchVertical.SetStaticState(true);

            Padding.SetStaticState(10);
            InteractiveSurface.ExtendInteractionRadius.SetStaticState(-10);
        }
    }
}