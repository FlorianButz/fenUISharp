using FenUISharp.Objects;

namespace FenUISharp.Behavior
{
    public class BehaviorComponent : IDisposable
    {
        private WeakReference<UIObject>? WeakOwner;
        public UIObject? Owner
        {
            get
            {
                // TODO: This line seems to bite me regardless of what I do
                if (WeakOwner?.TryGetTarget(out var target) ?? false) return target;
                return null; // Usually shouldn't happen but some edge cases may lead to it
            }
        }

        public bool Enabled { get; set; } = true;

        public BehaviorComponent(UIObject owner)
        {
            this.WeakOwner = new(owner);
            Owner?.BehaviorComponents.Add(this);
        }

        public virtual void HandleEvent(BehaviorEventType type, object? data = null) { }
        public virtual void ComponentDestroy() { }

        public void Dispose()
        {
            ComponentDestroy();
            
            Owner?.BehaviorComponents.Remove(this);
        }
    }

    public enum BehaviorEventType
    {
        BeforeBegin, AfterBegin,
        BeforeLateBegin, AfterLateBegin,
        BeforeSurfaceDraw, AfterSurfaceDraw,
        BeforeRender, AfterRender,
        BeforeDrawChildren, AfterDrawChildren,
        BeforeDrawChild, AfterDrawChild,
        BeforeEarlyUpdate, AfterEarlyUpdate,
        BeforeUpdate, AfterUpdate,
        BeforeLateUpdate, AfterLateUpdate,
        BeforeTransform, AfterTransform,
        BeforeLayout, AfterLayout
    }
}