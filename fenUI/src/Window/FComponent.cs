namespace FenUISharp {
    public class FComponent : IDisposable
    {
        public FUIComponent parent { get; private set; }

        public FComponent(FUIComponent parent){
            this.parent = parent;
        }

        public virtual void OnComponentUpdate() { }

        public virtual void OnComponentDestroy() { }

        public virtual void OnSelected() { }
        public virtual void OnSelectedLost() { }

        public virtual void OnMouseEnter() { }
        public virtual void OnMouseExit() { }
        public virtual void OnMouseDown() { }
        public virtual void OnMouseUp() { }
        public virtual void OnMouseRight() { }
        public virtual void OnMouseMove(Vector2 pos) { }

        public void Dispose()
        {
            OnComponentDestroy();
        }
    }
}