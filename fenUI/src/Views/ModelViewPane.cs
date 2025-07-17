using FenUISharp.Behavior;
using FenUISharp.Mathematics;
using SkiaSharp;

namespace FenUISharp.Objects
{
    public class ModelViewPane : UIObject
    {
        private List<UIObject>? _modelItems;

        private View? _model;
        public View? ViewModel { get => _model; set { SetViewAnimated(value); } }

        public bool AnimateViewModelSwap { get; set; } = true;
        public Action<float>? OnAnimationValueUpdated { get; set; }

        private AnimatorComponent _viewTransitionComponent;

        public float AnimOutDuration { get; set; } = 0.25f;
        public float AnimInDuration { get; set; } = 0.25f;

        public ModelViewPane(View? model, Func<Vector2>? position = null, Func<Vector2>? size = null) : base(position, size)
        {
            if(model != null)
                SilentSetView(model);

            _viewTransitionComponent = new(this, Easing.EaseInCubic, Easing.EaseOutCubic);
            _viewTransitionComponent.OnValueUpdate += (x) => OnAnimationValueUpdated?.Invoke(x);
            _viewTransitionComponent.Duration = 0.25f;

            OnAnimationValueUpdated += (x) =>
            {
                Transform.Scale.SetStaticState(Vector2.One * RMath.Remap(x, 0, 1, 1, 0.95f));
                // Transform.LocalPosition.SetStaticState(new(0, x * (_viewTransitionComponent.Inverse ? -15 : 15)));
                ImageEffects.Opacity.SetStaticState(1 - x);
            };
        }

        protected override void Update()
        {
            base.Update();
            _model?.Update();
        }

        private void SetViewAnimated(View? view)
        {
            if (view == null) return;

            var onComplete = () =>
            {
                SilentSetView(view);
                RecursiveInvalidate(Invalidation.All);

                _viewTransitionComponent.OnComplete = () =>
                {
                    _viewTransitionComponent.Duration = AnimateViewModelSwap ? AnimInDuration : 0f;
                    _viewTransitionComponent.OnComplete = null;
                    RecursiveInvalidate(Invalidation.All);
                };

                _viewTransitionComponent.Inverse = true;
                _viewTransitionComponent.Restart();
            };

            if (_model == null)
                onComplete.Invoke();
            else
            {
                _viewTransitionComponent.Duration = AnimateViewModelSwap ? AnimOutDuration : 0f;
                _viewTransitionComponent.Inverse = false;
                _viewTransitionComponent.OnComplete = onComplete;
                _viewTransitionComponent.Restart();
            }
        }

        protected void UpdateView()
        {
            if (_model == null) return;

            _model.PaneRoot = this;
            _modelItems = _model.Create();
            _modelItems.ForEach(x => { if (x.Parent == null || x.Parent is ModelViewPane) x.SetParent(this); });
            _model.OnViewShown();

            Layout.RecursivelyUpdateLayout();
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