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
            if(!parent.WindowRoot.IsWindowFocused) return;
            if (RMath.ContainsPoint(parent.Transform.Bounds, parent.WindowRoot.ClientMousePosition) && 
                parent.GetTopmostComponentAtPositionWithComponent<UserScrollComponent>(parent.WindowRoot.ClientMousePosition) == parent)
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