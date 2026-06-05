namespace FenUISharp.Objects
{
    public abstract class View
    {
        public bool IsDisposed { get; internal set; }
        public ModelViewPane? PaneRoot { get; set; }

        public abstract List<UIObject> Create();
        public virtual void OnViewShown() { IsDisposed = false; }
        public virtual void OnViewDestroyed() { IsDisposed = true; }
        public virtual void Update() { }

        protected void AddUIObjectToRegisteredList(UIObject uiObject)
        {
            if (PaneRoot == null) throw new InvalidOperationException($"ViewModel cannot register UIObject {uiObject.GetType()} to ModelViewPane as PaneRoot is null.");

            uiObject.SetParent(PaneRoot);
            PaneRoot._modelItems?.Add(uiObject);
        }
    }
}