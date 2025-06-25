using FenUISharp.Mathematics;
using SkiaSharp;

namespace FenUISharp.Objects
{
    public class ModelViewPane : UIObject
    {
        private List<UIObject>? _modelItems;

        // TODO: Reenable all features

        private View? _model;
        public View? ViewModel { get => _model; set { /*SetViewAnimated(value);*/ _model = value; UpdateView(); } }

        // public bool AnimateViewModelSwap { get; set; } = true;
        // public Action<float>? OnAnimationValueUpdated { get; set; }

        // private AnimatorComponent _viewTransitionComponent;

        // public float AnimOutDuration { get; set; } = 0.25f;
        // public float AnimInDuration { get; set; } = 0.25f;

        public ModelViewPane(View? model, Func<Vector2>? position = null, Func<Vector2>? size = null) : base(position, size)
        {
            this._model = model;
            UpdateView();

            // _viewTransitionComponent = new(this, Easing.EaseInCubic, Easing.EaseOutCubic);
            // _viewTransitionComponent.onValueUpdate += (x) => OnAnimationValueUpdated?.Invoke(x);
            // _viewTransitionComponent.Duration = 0.25f;

            // OnAnimationValueUpdated += (x) =>
            // {
            //     Transform.Scale = Vector2.One * RMath.Remap(x, 0, 1, 1, 0.95f);
            //     ImageEffect.Opacity = RMath.Remap(x, 0, 1, 1, 0f);
            // };
        }

        protected override void Update()
        {
            base.Update();
            _model?.Update();
        }

        // private void SetViewAnimated(View? view)
        // {
        //     if (view == null) return;

        //     _viewTransitionComponent.Duration = AnimateViewModelSwap ? AnimOutDuration : 0f;
        //     _viewTransitionComponent.Inverse = false;
        //     _viewTransitionComponent.onComplete = () =>
        //     {
        //         SilentSetView(view);
        //         RecursiveInvalidate();
        //         Transform.UpdateLayout();

        //         _viewTransitionComponent.onComplete = () =>
        //         {
        //             _viewTransitionComponent.Duration = AnimateViewModelSwap ? AnimInDuration : 0f;
        //             _viewTransitionComponent.onComplete = null;
        //             RecursiveInvalidate();
        //             Transform.UpdateLayout();
        //         };

        //         _viewTransitionComponent.Inverse = true;
        //         _viewTransitionComponent.Restart();
        //     };
        //     _viewTransitionComponent.Restart();
        // }

        protected void UpdateView()
        {
            if (_model == null) return;

            _model.PaneRoot = this;
            _modelItems = _model.Create();
            _modelItems.ForEach(x => { if (x.Parent == null || x.Parent is ModelViewPane) x.SetParent(this); });
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
            _modelItems?.ForEach(x => x.Dispose());
        }

        public override void Dispose()
        {
            base.Dispose();
            DisposeItems();
        }
    }
}