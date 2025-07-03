using FenUISharp.Behavior;
using FenUISharp.Components;
using FenUISharp.Materials;
using FenUISharp.Mathematics;
using FenUISharp.States;
using FenUISharp.WinFeatures;
using SkiaSharp;

namespace FenUISharp.Objects.Buttons
{
    public abstract class Button : UIObject, IStateListener
    {
        public Action? OnClick { get; set; }

        public State<float> CornerRadius { get; init; }
        public State<SKColor> HoverMix { get; init; }

        public float HoverPixelAddition { get; set; } = 1f;

        protected SelectableComponent selectableComponent;

        // Basic animation fields
        protected AnimatorComponent animatorComponent;
        internal SKColor currentHoverMix;

        private KeyBind spaceInteract;

        public Button(Action? onClick = null, Func<Vector2>? position = null, Func<Vector2>? size = null) : base(position, size)
        {
            this.OnClick = onClick;

            spaceInteract = new() { VKCode = 0x20, OnKeybindExecuted = OnInteract };

            selectableComponent = new SelectableComponent(this, this.InteractiveSurface);
            selectableComponent.OnSelectionGained += () => FContext.GetKeyboardInputManager().RegisterKeybind(spaceInteract);
            selectableComponent.OnSelectionLost += () => FContext.GetKeyboardInputManager().UnregisterKeybind(spaceInteract);

            HoverMix = new(() => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.HoveredMix, this);
            RenderMaterial.Value = FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.InteractableMaterial;
            currentHoverMix = SKColors.Transparent;

            CornerRadius = new(() => 10f, this);

            InteractiveSurface.EnableMouseActions.SetStaticState(true);
            InteractiveSurface.OnMouseAction += MouseAction;

            animatorComponent = new AnimatorComponent(this, Easing.EaseOutCubic);
            animatorComponent.Duration = 0.15f;

            InteractiveSurface.OnMouseEnter += MouseEnter;
            InteractiveSurface.OnMouseExit += MouseExit;

            animatorComponent.OnValueUpdate += (t) =>
            {
                var hoveredMix = RMath.Lerp(SKColors.Transparent, HoverMix.CachedValue, 0.1f);
                currentHoverMix = RMath.Lerp(SKColors.Transparent, hoveredMix, t);

                float pixelsAdd = HoverPixelAddition;
                float sx = (Transform.Size.CachedValue.x + pixelsAdd) / Transform.Size.CachedValue.x;
                float sy = (Transform.Size.CachedValue.y + pixelsAdd / 2) / Transform.Size.CachedValue.y;

                Transform.Scale.SetStaticState(Vector2.Lerp(new Vector2(1, 1), new Vector2(sx, sy), t));
                Invalidate(Invalidation.SurfaceDirty);
            };

            Padding.SetStaticState(10);

            Transform.SnapPositionToPixelGrid.SetStaticState(true);
            UpdateColors();
        }

        public override void Dispose()
        {
            base.Dispose();
            CornerRadius.Dispose();
            HoverMix.Dispose();
        }

        public override void OnInternalStateChanged<T>(T value)
        {
            base.OnInternalStateChanged(value);

            UpdateColors();
            Invalidate(Invalidation.SurfaceDirty);
        }

        protected void UpdateColors()
        {
            if (animatorComponent.IsRunning) return;

            var hoveredMix = RMath.Lerp(SKColors.Transparent, HoverMix.CachedValue, 0.1f);
            currentHoverMix = RMath.Lerp(SKColors.Transparent, hoveredMix, InteractiveSurface.IsMouseHovering ? 1 : 0);
        }

        protected virtual void MouseEnter()
        {
            if (GlobalHooks.MouseDown) return;

            animatorComponent.Inverse = false;
            animatorComponent.Start();
        }

        protected virtual void MouseExit()
        {
            if (GlobalHooks.MouseDown) return;

            animatorComponent.Inverse = true;
            animatorComponent.Start();
        }

        protected virtual void MouseAction(MouseInputCode inputCode)
        {
            if (inputCode.button == MouseInputButton.Left && inputCode.state == MouseInputState.Down)
            {
                animatorComponent.Inverse = true;
                animatorComponent.Restart();
            }
            else if (inputCode.button == MouseInputButton.Left && inputCode.state == MouseInputState.Up)
            {
                OnInteract();
                animatorComponent.Inverse = false;
                animatorComponent.Restart();
            }
        }

        protected virtual void OnInteract()
        {
            animatorComponent.Inverse = true;
            animatorComponent.Restart();
            OnClick?.Invoke();
        }

        public override void Render(SKCanvas canvas)
        {
            base.Render(canvas);

            using (var path = SKSquircle.CreateSquircle(Shape.LocalBounds, CornerRadius.CachedValue))
            {
                using var paint = GetRenderPaint();
                RenderMaterial.CachedValue.DrawWithMaterial(canvas, path, this, paint);
            }
        }

        public override void AfterRender(SKCanvas canvas)
        {
            base.AfterRender(canvas);

            using (var path = SKSquircle.CreateSquircle(Shape.LocalBounds, CornerRadius.CachedValue))
            {
                using var paint = GetRenderPaint();
                paint.Color = currentHoverMix;
                canvas.DrawPath(path, paint);
            }
        }
    }
}