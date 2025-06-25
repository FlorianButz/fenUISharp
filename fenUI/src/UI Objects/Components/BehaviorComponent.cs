using FenUISharp.Objects;

namespace FenUISharp.Behavior
{
    public class BehaviorComponent : IDisposable
    {
        public UIObject Owner { get; init; }
        public bool Enabled { get; set; } = true;

        public BehaviorComponent(UIObject owner)
        {
            this.Owner = owner;
            Owner.BehaviorComponents.Add(this);
        }

        public virtual void HandleEvent(BehaviorEventType type, object? data = null) { }
        public virtual void ComponentDestroy() { }

        public void Dispose()
        {
            ComponentDestroy();
            Owner.BehaviorComponents.Remove(this);
        }
    }

    public enum BehaviorEventType
    {
        BeforeBegin, AfterBegin,
        BeforeSurfaceDraw, AfterSurfaceDraw,
        BeforeRender, AfterRender,
        BeforeDrawChildren, AfterDrawChildren,
        BeforeDrawChild, AfterDrawChild,
        BeforeUpdate, AfterUpdate,
        BeforeLateUpdate, AfterLateUpdate,
        BeforeTransform, AfterTransform,
        BeforeLayout, AfterLayout

        // TODO: Add input; Edit: probably not necessary, can easily just hook in to InteractiveSurface
    }
}