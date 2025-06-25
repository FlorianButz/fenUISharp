using FenUISharp.Objects;

namespace FenUISharp.Behavior
{
    public class CursorComponent : BehaviorComponent
    {
        public Cursor CursorOnHover { get; set; }

        public CursorComponent(UIObject owner, Cursor cursorOnHover = Cursor.ARROW) : base(owner)
        {
            CursorOnHover = cursorOnHover;

            owner.InteractiveSurface.OnMouseEnter += MouseEnter;
            owner.InteractiveSurface.OnMouseEnter += MouseExit;
        }

        public override void ComponentDestroy()
        {
            base.ComponentDestroy();
            
            Owner.InteractiveSurface.OnMouseEnter -= MouseEnter;
            Owner.InteractiveSurface.OnMouseEnter -= MouseExit;
        }

        private void MouseEnter()
        {
            if (Owner.Composition.TestIfTopMost()) FContext.GetCurrentWindow().ActiveCursor.SetValue(this, CursorOnHover, 5);
        }

        private void MouseExit()
        {
            if (Owner.Composition.TestIfTopMost()) FContext.GetCurrentWindow().ActiveCursor.DissolveValue(this);
        }
    }
}