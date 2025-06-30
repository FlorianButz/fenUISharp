using FenUISharp.Objects;
using FenUISharp.States;

namespace FenUISharp.Behavior
{
    public class LayoutObject : BehaviorComponent, IStateListener
    {
        public State<bool> IgnoreParentLayout { get; init; }

        public LayoutObject(UIObject owner) : base(owner)
        {
            IgnoreParentLayout = new(() => false, this);
        }

        public override void ComponentDestroy()
        {
            IgnoreParentLayout.Dispose();
        }

        public void OnInternalStateChanged<T>(T value)
        {
            Owner.Invalidate(UIObject.Invalidation.LayoutDirty);
        }
    }
}