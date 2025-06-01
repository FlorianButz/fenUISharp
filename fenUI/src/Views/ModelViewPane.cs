using FenUISharp.Components;
using FenUISharp.Mathematics;
using SkiaSharp;

namespace FenUISharp.Views
{
    public class ModelViewPane : UIComponent
    {
        private List<UIComponent>? _modelItems;

        private View? _model;
        public View? ViewModel { get => _model; set { SetViewAnimated(value); } }

        public bool AnimateViewModelSwap { get; set; } = true;
        public Action<float>? OnAnimationValueUpdated { get; set; }

        private AnimatorComponent _viewTransitionComponent;

        public float AnimOutDuration { get; set; } = 0.25f;
        public float AnimInDuration { get; set; } = 0.25f;

        public ModelViewPane(Window rootWindow, View? model, Vector2 position, Vector2 size) : base(rootWindow, position, size)
        {
            this._model = model;
            UpdateView();

            _viewTransitionComponent = new(this, Easing.EaseInCubic, Easing.EaseOutCubic);
            _viewTransitionComponent.onValueUpdate += (x) => OnAnimationValueUpdated?.Invoke(x);
            _viewTransitionComponent.Duration = 0.25f;

            OnAnimationValueUpdated += (x) =>
            {
                Transform.Scale = Vector2.One * RMath.Remap(x, 0, 1, 1, 0.95f);
                ImageEffect.Opacity = RMath.Remap(x, 0, 1, 1, 0f);
            };
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            _model?.Update();
        }

        private void SetViewAnimated(View? view)
        {
            if (view == null) return;

            _viewTransitionComponent.Duration = AnimateViewModelSwap ? AnimOutDuration : 0f;
            _viewTransitionComponent.Inverse = false;
            _viewTransitionComponent.onComplete = () =>
            {
                SilentSetView(view);
                RecursiveInvalidate();
                Transform.UpdateLayout();

                _viewTransitionComponent.onComplete = () =>
                {
                    _viewTransitionComponent.Duration = AnimateViewModelSwap ? AnimInDuration : 0f;
                    _viewTransitionComponent.onComplete = null;
                    RecursiveInvalidate();
                    Transform.UpdateLayout();
                };

                _viewTransitionComponent.Inverse = true;
                _viewTransitionComponent.Restart();
            };
            _viewTransitionComponent.Restart();
        }

        protected void UpdateView()
        {
            if (_model == null) return;

            _model.WindowRoot = WindowRoot;
            _model.PaneRoot = this;
            _modelItems = _model.Create(WindowRoot);
            _modelItems.ForEach(x => { if (x.Transform.Parent == null) x.Transform.SetParent(this.Transform); });
            _model.OnViewShown();
        }

        public void SilentSetView(View view)
        {
            DisposeItems();
            _model = view;
            UpdateView();
        }

        protected void DisposeItems()
        {
            if (_model == null) return;

            _model.OnViewDestroyed();
            _modelItems?.ForEach(x => x.Transform.ClearParent());
            _modelItems?.ForEach(x => x.Dispose());
        }

        protected override void ComponentDestroy()
        {
            base.ComponentDestroy();
            DisposeItems();
        }

        protected override void DrawToSurface(SKCanvas canvas) { return; }
    }
}