using FenUISharp.Components;
using FenUISharp.Mathematics;
using FenUISharp.WinFeatures;

namespace FenUISharp
{
    public class UserDragComponent : Component
    {
        public Action? OnDragStart { get; set; }
        public Action? OnDragEnd { get; set; }
        public Action<Vector2>? OnDrag { get; set; }
        public Action<Vector2>? OnDragDelta { get; set; }

        private Vector2 _startGlobalMousePos;
        private Vector2 _lastGlobalMousePos;

        private volatile bool _stoppedDraggingFlag = false;
        public bool IsDragging { get; private set; }

        public UserDragComponent(UIComponent parent) : base(parent)
        {
            WindowFeatures.GlobalHooks.OnMouseAction += OnGlobalHooks_OnMouseAction;
        }

        public override void ComponentUpdate()
        {
            base.ComponentUpdate();

            if(_stoppedDraggingFlag){
                _stoppedDraggingFlag = false;
                IsDragging = false;

                _startGlobalMousePos = new(0, 0);

                OnDragEnd?.Invoke();
            }

            if(IsDragging){
                OnDrag?.Invoke(GlobalHooks.MousePosition - _startGlobalMousePos);
                OnDragDelta?.Invoke(GlobalHooks.MousePosition - _lastGlobalMousePos);
            }

            _lastGlobalMousePos = GlobalHooks.MousePosition;
        }

        public override void MouseAction(MouseInputCode inputCode)
        {
            base.MouseAction(inputCode);

            if(inputCode.state == (int)MouseInputState.Down && inputCode.button == (int)MouseInputButton.Left) {
                IsDragging = true;

                _startGlobalMousePos = GlobalHooks.MousePosition;
                OnDragStart?.Invoke();
            }
        }

        public override void ComponentDestroy()
        {
            base.ComponentDestroy();

            WindowFeatures.GlobalHooks.OnMouseAction -= OnGlobalHooks_OnMouseAction;
        }

        private void OnGlobalHooks_OnMouseAction(MouseInputCode code)
        {
            _stoppedDraggingFlag = true;
        }
    }
}