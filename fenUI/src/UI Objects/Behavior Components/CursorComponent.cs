using FenUISharp.Objects;

namespace FenUISharp.Behavior
{
    public class CursorComponent : BehaviorComponent
    {
        public Cursor CursorOnHover { get; set; }

        public CursorComponent(UIObject owner, Cursor cursorOnHover = Cursor.ARROW) : base(owner)
        {
            CursorOnHover = cursorOnHover;

            owner.InteractiveSurface.EnableMouseActions.SetStaticState(true);
            owner.InteractiveSurface.OnMouseEnter += MouseEnter;
            owner.InteractiveSurface.OnMouseExit += MouseExit;
        }

        public override void ComponentDestroy()
        {
            base.ComponentDestroy();
            
            Owner.InteractiveSurface.OnMouseEnter -= MouseEnter;
            Owner.InteractiveSurface.OnMouseExit -= MouseExit;
        }

        private void MouseEnter()
        {
            FContext.GetCurrentWindow().ActiveCursor.SetValue(this, CursorOnHover, 50);
        }

        private void MouseExit()
        {
            FContext.GetCurrentWindow().ActiveCursor.DissolveValue(this);
        }
    }
}