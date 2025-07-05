using System.Diagnostics;
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

        public DropComponent(UIObject owner, DropType dType = DropType.AnyText, DROPEFFECT dEffect = DROPEFFECT.Copy) : base(owner)
        {
            this.DropType = dType;
            this.DropEffect = dEffect;
        }

        private void DragDrop(FDropData? data)
        {
            if(Owner.Composition.TestIfTopMost() && _isCurrentlyInDragAction){
                OnDragDropActionComplete(data);
            }

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

        public override void HandleEvent(BehaviorEventType type, object? data = null)
        {
            base.HandleEvent(type, data);

            switch (type)
            {
                case BehaviorEventType.BeforeBegin:
                    ComponentSetup();
                    break;
                case BehaviorEventType.BeforeUpdate:
                    ComponentUpdate();
                    break;
            }
        }

        public void ComponentSetup()
        {
            FContext.GetCurrentWindow().DropTarget.dragDrop += DragDrop;
            FContext.GetCurrentWindow().DropTarget.dragEnter += DragEnter;
            FContext.GetCurrentWindow().DropTarget.dragLeave += DragLeave;

            if (FContext.GetCurrentWindow().DropTarget.IsDragDropActionInProgress) DragEnter(FContext.GetCurrentWindow().DropTarget.lastDropData);

            // Don't use drag over, it runs on different thread. Should use cutom drag over
        }

        public void ComponentUpdate()
        {
            if (!Owner.Enabled.CachedValue) return;
            if (!_windowHasCompatibleActiveDragAction) return;

            if (Owner.Composition.TestIfTopMost() && !_isCurrentlyInDragAction)
            {
                _isCurrentlyInDragAction = true;
                OnDragDropActionEnter(FContext.GetCurrentWindow().DropTarget.lastDropData);
            }
            else if (Owner.Composition.TestIfTopMost() && _isCurrentlyInDragAction)
            {
                _isCurrentlyInDragAction = false;
                OnDragDropActionLeave();
            }
            else if(Owner.Composition.TestIfTopMost() && _isCurrentlyInDragAction){
                OnDragStay?.Invoke();
            }
        }

        protected void OnDragDropActionEnter(FDropData? data)
        {
            FContext.GetCurrentWindow().DropTarget.dropEffect.SetValue(this, DropEffect, 5);

            if(data != null)
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
        
            if(data != null)
                OnDrop?.Invoke(data);
        }

        bool IsSameType(DropType otherType){
            if(otherType == DropType) return true;
            else if((otherType == DropType.AnsiText || otherType == DropType.UnicodeText) && DropType == DropType.AnyText) return true;
            
            return false;
        }

        public override void ComponentDestroy()
        {
            base.ComponentDestroy();

            FContext.GetCurrentWindow().DropTarget.dragDrop -= DragDrop;
            FContext.GetCurrentWindow().DropTarget.dragEnter -= DragEnter;
            FContext.GetCurrentWindow().DropTarget.dragLeave -= DragLeave;
        }
    }
}