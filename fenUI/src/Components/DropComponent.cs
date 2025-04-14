using System.Diagnostics;
using FenUISharp.Components;

namespace FenUISharp
{

    public class DropComponent : Component
    {
        public DropType DropType { get; set; }
        public DROPEFFECT DropEffect { get; set; }

        private bool _windowHasCompatibleActiveDragAction = false;
        private bool _isCurrentlyInDragAction = false;

        public Action<FDropData>? OnDrop { get; set; }
        public Action<FDropData>? OnDragEnter { get; set; }
        public Action? OnDragStay { get; set; }
        public Action? OnDragLeave { get; set; }

        public DropComponent(UIComponent parent, DropType dType = DropType.AnyText, DROPEFFECT dEffect = DROPEFFECT.Copy) : base(parent)
        {
            this.DropType = dType;
            this.DropEffect = dEffect;
        }

        private void DragDrop(FDropData? data)
        {
            if(Parent.GetTopmostComponentAtPosition(Parent.WindowRoot.ClientMousePosition) == Parent && _isCurrentlyInDragAction){
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

        public override void ComponentSetup()
        {
            base.ComponentSetup();

            Parent.WindowRoot.DropTarget.dragDrop += DragDrop;
            Parent.WindowRoot.DropTarget.dragEnter += DragEnter;
            Parent.WindowRoot.DropTarget.dragLeave += DragLeave;

            // Don't use drag over, it runs on different thread. Should use cutom drag over
        }

        public override void ComponentUpdate()
        {
            base.ComponentUpdate();

            if (!Parent.CareAboutInteractions) return;
            if (!Parent.Enabled) return;

            if (!_windowHasCompatibleActiveDragAction) return;

            if (Parent.GetTopmostComponentAtPosition(Parent.WindowRoot.ClientMousePosition) == Parent && !_isCurrentlyInDragAction)
            {
                _isCurrentlyInDragAction = true;
                OnDragDropActionEnter(Parent.WindowRoot.DropTarget.lastDropData);
            }
            else if (Parent.GetTopmostComponentAtPosition(Parent.WindowRoot.ClientMousePosition) != Parent && _isCurrentlyInDragAction)
            {
                _isCurrentlyInDragAction = false;
                OnDragDropActionLeave();
            }
            else if(Parent.GetTopmostComponentAtPosition(Parent.WindowRoot.ClientMousePosition) == Parent && _isCurrentlyInDragAction){
                OnDragStay?.Invoke();
            }
        }

        protected void OnDragDropActionEnter(FDropData? data)
        {
            Parent.WindowRoot.DropTarget.dropEffect.SetValue(this, DropEffect, 5);

            OnDragEnter?.Invoke(data);
        }

        protected void OnDragDropActionLeave()
        {
            Parent.WindowRoot.DropTarget.dropEffect.DissolveValue(this);
        
            OnDragLeave?.Invoke();
        }

        protected void OnDragDropActionComplete(FDropData? data)
        {
            Parent.WindowRoot.DropTarget.dropEffect.DissolveValue(this);
        
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

            Parent.WindowRoot.DropTarget.dragDrop -= DragDrop;
            Parent.WindowRoot.DropTarget.dragEnter -= DragEnter;
            Parent.WindowRoot.DropTarget.dragLeave -= DragLeave;
        }
    }
}