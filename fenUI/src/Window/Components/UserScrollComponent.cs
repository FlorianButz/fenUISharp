using System.Diagnostics;

namespace FenUISharp
{
    public class UserScrollComponent : Component
    {
        public Action<float>? MouseScroll { get; set; }

        public UserScrollComponent(UIComponent parent) : base(parent)
        {
            WindowFeatures.GlobalHooks.onMouseScroll += OnGlobalHooks_onMouseScroll;
        }

        private void OnGlobalHooks_onMouseScroll(float delta)
        {
            if(!Parent.WindowRoot.IsWindowFocused) return;
            if (RMath.ContainsPoint(Parent.Transform.Bounds, Parent.WindowRoot.ClientMousePosition) && 
                Parent.GetTopmostComponentAtPositionWithComponent<UserScrollComponent>(Parent.WindowRoot.ClientMousePosition) == Parent)
            {
                MouseScroll?.Invoke(delta);
            }
        }

        public override void ComponentDestroy()
        {
            base.ComponentDestroy();

            WindowFeatures.GlobalHooks.onMouseScroll -= OnGlobalHooks_onMouseScroll;
        }
    }
}