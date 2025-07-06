using FenUISharp.Objects;

namespace FenUISharp.Behavior.Layout
{
    public abstract class LayoutComponent : BehaviorComponent
    {
        protected LayoutComponent(UIObject owner) : base(owner)
        {
        }

        public abstract void FullUpdateLayout();
    }
}