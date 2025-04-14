using System.Diagnostics;
using FenUISharp.Components;
using FenUISharp.Mathematics;
using FenUISharp.WinFeatures;

namespace FenUISharp
{
    public class UserScrollComponent : Component
    {
        public Action<float>? MouseScroll { get; set; }
        private volatile float _lastDelta = 0f;

        public UserScrollComponent(UIComponent parent) : base(parent)
        {
            WindowFeatures.GlobalHooks.OnMouseScroll += OnGlobalHooks_onMouseScroll;
        }

        public override void ComponentUpdate()
        {
            base.ComponentUpdate();

            if (_lastDelta != 0f)
            {
                MouseScroll?.Invoke(_lastDelta);
                _lastDelta = 0f;
            }
        }

        private void OnGlobalHooks_onMouseScroll(float delta)
        {
            if (!Parent.WindowRoot.IsWindowFocused) return;
            if (RMath.ContainsPoint(Parent.Transform.Bounds, Parent.WindowRoot.ClientMousePosition) &&
                Parent.GetTopmostComponentAtPositionWithComponent<UserScrollComponent>(Parent.WindowRoot.ClientMousePosition) == Parent)
            {
                _lastDelta += delta;
            }
        }

        public override void ComponentDestroy()
        {
            base.ComponentDestroy();
            WindowFeatures.GlobalHooks.OnMouseScroll -= OnGlobalHooks_onMouseScroll;
        }
    }
}