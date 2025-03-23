namespace FenUISharp {
    public class Component : IDisposable
    {
        public UIComponent parent { get; private set; }

        public Component(UIComponent parent){
            this.parent = parent;
        }

        public virtual void ComponentUpdate() { }

        public virtual void ComponentDestroy() { }

        public virtual void Selected() { }
        public virtual void SelectedLost() { }

        public virtual void MouseEnter() { }
        public virtual void MouseExit() { }
        public virtual void MouseAction(MouseInputCode inputCode) { }
        public virtual void MouseMove(Vector2 pos) { }

        public void Dispose()
        {
            ComponentDestroy();
        }
    }
}