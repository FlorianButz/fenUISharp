using System.Diagnostics;
using FenUISharp.Mathematics;
using FenUISharp.Objects;

namespace FenUISharp.Behavior
{
    public class DropComponent : BehaviorComponent
    {
        public DropType DropType { get; set; }
        public DROPEFFECT DropEffect { get; set; }

        private bool _windowHasCompatibleActiveDragAction = false;
        private bool _isCurrentlyInDragAction = false;

        public Action<FDropData>? OnDrop { get; set; }
        public Action<FDropData>? OnDragEnter { get; set; }
        public Action? OnDragStay { get; set; }
        public Action? OnDragLeave { get; set; }

        private Dispatcher dispatcher;

        public DropComponent(UIObject owner, DropType dType = DropType.AnyText, DROPEFFECT dEffect = DROPEFFECT.Copy) : base(owner)
        {
            this.DropType = dType;
            this.DropEffect = dEffect;

            FContext.GetCurrentWindow().DropTarget.dragDrop += DragDrop;
            FContext.GetCurrentWindow().DropTarget.dragEnter += DragEnter;
            FContext.GetCurrentWindow().DropTarget.dragLeave += DragLeave;

            if (Owner == null) return;
            Owner.InteractiveSurface.EnableMouseActions.SetStaticState(true, 25);
            Owner.InteractiveSurface.OnMouseEnter += MouseEnter;
            Owner.InteractiveSurface.OnMouseExit += MouseExit;
            Owner.InteractiveSurface.OnMouseMove += MouseStay;

            dispatcher = FContext.GetCurrentDispatcher();

            if (FContext.GetCurrentWindow().DropTarget.IsDragDropActionInProgress) DragEnter(FContext.GetCurrentWindow().DropTarget.lastDropData);
        }

        private void MouseStay(Vector2 vector)
        {
            if (Owner == null || !Owner.GlobalEnabled) return;
            if (!_windowHasCompatibleActiveDragAction) return;

            OnDragStay?.Invoke();
        }

        private void MouseExit()
        {
            if (Owner == null || !Owner.GlobalEnabled) return;
            if (!_windowHasCompatibleActiveDragAction) return;

            _isCurrentlyInDragAction = false;
            OnDragDropActionLeave();
        }

        private void MouseEnter()
        {
            if (Owner == null || !Owner.GlobalEnabled) return;
            if (!_windowHasCompatibleActiveDragAction) return;

            _isCurrentlyInDragAction = true;
            OnDragDropActionEnter(FContext.GetCurrentWindow().DropTarget.lastDropData);
        }

        private void DragDrop(FDropData? data)
        {
            if (Owner == null || !Owner.GlobalEnabled) return;
            if (Owner.InteractiveSurface.IsMouseHovering && _isCurrentlyInDragAction)
                dispatcher.Invoke(() => OnDragDropActionComplete(data));

            _windowHasCompatibleActiveDragAction = false;
            _isCurrentlyInDragAction = false;
        }

        private void DragLeave()
        {
            _windowHasCompatibleActiveDragAction = false;
            _isCurrentlyInDragAction = false;
        }

        private void DragEnter(FDropData? data)
        {
            if (data == null) return;
            if (!IsSameType(data.dropType)) return;

            _windowHasCompatibleActiveDragAction = true;
        }

        protected void OnDragDropActionEnter(FDropData? data)
        {
            FContext.GetCurrentWindow().DropTarget.dropEffect.SetValue(this, DropEffect, 5);

            if (data != null)
                OnDragEnter?.Invoke(data);
        }

        protected void OnDragDropActionLeave()
        {
            FContext.GetCurrentWindow().DropTarget.dropEffect.DissolveValue(this);

            OnDragLeave?.Invoke();
        }

        protected void OnDragDropActionComplete(FDropData? data)
        {
            FContext.GetCurrentWindow().DropTarget.dropEffect.DissolveValue(this);

            if (data != null)
                OnDrop?.Invoke(data);
        }

        bool IsSameType(DropType otherType)
        {
            if (otherType == DropType) return true;
            else if ((otherType == DropType.AnsiText || otherType == DropType.UnicodeText) && DropType == DropType.AnyText) return true;

            return false;
        }

        public override void ComponentDestroy()
        {
            base.ComponentDestroy();

            FContext.GetCurrentWindow().DropTarget.dragDrop -= DragDrop;
            FContext.GetCurrentWindow().DropTarget.dragEnter -= DragEnter;
            FContext.GetCurrentWindow().DropTarget.dragLeave -= DragLeave;

            if (Owner == null) return;
            Owner.InteractiveSurface.EnableMouseActions.DissolvePriority(25);
            Owner.InteractiveSurface.OnMouseEnter -= MouseEnter;
            Owner.InteractiveSurface.OnMouseExit -= MouseExit;
            Owner.InteractiveSurface.OnMouseMove -= MouseStay;
        }
    }
}