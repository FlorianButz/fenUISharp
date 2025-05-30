using FenUISharp.Components;

namespace FenUISharp.Views
{
    public abstract class View
    {
        public Window WindowRoot { get; set; }
        public ModelViewPane? PaneRoot { get; set; }

        public abstract List<UIComponent> Create(Window rootWindow);
        public virtual void OnViewShown() { }
        public virtual void OnViewDestroyed() { }
        public virtual void Update() { }
    }
}