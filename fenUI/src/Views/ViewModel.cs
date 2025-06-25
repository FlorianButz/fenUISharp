namespace FenUISharp.Objects
{
    public abstract class View
    {
        public ModelViewPane? PaneRoot { get; set; }

        public abstract List<UIObject> Create();
        public virtual void OnViewShown() { }
        public virtual void OnViewDestroyed() { }
        public virtual void Update() { }
    }
}