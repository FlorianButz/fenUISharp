using FenUISharp.Components;
using FenUISharp.Mathematics;
using SkiaSharp;

namespace FenUISharp.Objects.Buttons
{
    public abstract class Button : UIObject
    {
        public Action? OnClick { get; set; }

        protected const float PIXEL_ADD = 1f; // Just a global constant for the size change in the animation

        public Button(Action? onClick = null, Func<Vector2>? position = null, Func<Vector2>? size = null) : base(position, size)
        {
            this.OnClick = onClick;

            InteractiveSurface.EnableMouseActions.SetStaticState(true);
            InteractiveSurface.OnMouseAction += MouseAction;
        }
    
        protected virtual void MouseAction(MouseInputCode inputCode)
        {
            if (inputCode.button == MouseInputButton.Left && inputCode.state == MouseInputState.Up)
                OnClick?.Invoke();
        }
    }
}