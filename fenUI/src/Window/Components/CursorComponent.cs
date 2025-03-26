using System.Diagnostics;

namespace FenUISharp
{
    public class CursorComponent : Component
    {
        public Cursor CursorOnHover { get; set; }

        public CursorComponent(UIComponent parent, Cursor cursorOnHover = Cursor.ARROW) : base(parent)
        {
            CursorOnHover = cursorOnHover;
        }

        public override void MouseEnter()
        {
            base.MouseEnter();

            if(parent.GetTopmostComponentAtPosition(parent.WindowRoot.ClientMousePosition) == parent) parent.WindowRoot.ActiveCursor.SetValue(this, CursorOnHover, 5);
        }

        public override void MouseExit()
        {
            base.MouseExit();

            parent.WindowRoot.ActiveCursor.DissolveValue(this);
        }
    }
}