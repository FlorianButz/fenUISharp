using FenUISharp.Components;
using FenUISharp.Mathematics;
using SkiaSharp;

namespace FenUISharp.Components.Buttons
{
    public abstract class Button : UIComponent
    {
        public Action? OnClick { get; set; }

        public Button(Window rootWindow, Vector2 position, Vector2 size, Action? onClick = null) : base(rootWindow, position, size)
        {
            this.OnClick = onClick;
        }
    
        protected override void MouseAction(MouseInputCode inputCode)
        {
            base.MouseAction(inputCode);

            if (inputCode.button == 0 && inputCode.state == 1)
                OnClick?.Invoke();
        }
    }
}