using SkiaSharp;

namespace FenUISharp {
    public class Component : IDisposable
    {
        public UIComponent parent { get; private set; }
        private bool _isSetup = false;

        public Component(UIComponent parent){
            this.parent = parent;
            this.parent.Components.Add(this);
        }

        public void CmpUpdate() {
            if(!_isSetup) {
                _isSetup = true;
                ComponentSetup();
            }

            ComponentUpdate();
        }

        public virtual void OnBeforeRender(SKCanvas canvas) { }
        public virtual void OnAfterRender(SKCanvas canvas) { }
        public virtual void OnBeforeRenderCache(SKCanvas canvas) { }
        public virtual void OnAfterRenderCache(SKCanvas canvas) { }
        public virtual void OnBeforeRenderChildren(SKCanvas canvas) { }
        public virtual void OnAfterRenderChildren(SKCanvas canvas) { }

        public virtual void ComponentSetup() { }
        public virtual void ComponentUpdate() { }
        public virtual void ComponentDestroy() { }

        public virtual void Selected() { }
        public virtual void SelectedLost() { }

        public virtual void MouseEnter() { }
        public virtual void MouseExit() { }
        public virtual void MouseAction(MouseInputCode inputCode) { }
        public virtual void GlobalMouseAction(MouseInputCode inputCode) { }
        public virtual void MouseMove(Vector2 pos) { }

        public void Dispose()
        {
            ComponentDestroy();
            this.parent.Components.Remove(this);
        }
    }
}