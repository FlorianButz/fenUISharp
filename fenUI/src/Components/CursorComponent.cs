using System.Diagnostics;
using FenUISharp.Components;

namespace FenUISharp
{
    public class CursorComponent : BehaviorComponent
    {
        public Cursor CursorOnHover { get; set; }

        public CursorComponent(UIComponent parent, Cursor cursorOnHover = Cursor.ARROW) : base(parent)
        {
            CursorOnHover = cursorOnHover;
        }

        public override void MouseEnter()
        {
            base.MouseEnter();

            if(Parent.GetTopmostComponentAtPosition(Parent.WindowRoot.ClientMousePosition) == Parent) Parent.WindowRoot.ActiveCursor.SetValue(this, CursorOnHover, 5);
        }

        public override void MouseExit()
        {
            base.MouseExit();

            Parent.WindowRoot.ActiveCursor.DissolveValue(this);
        }
    }
}