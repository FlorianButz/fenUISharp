using FenUISharp.Mathematics;
using FenUISharp.States;
using SkiaSharp;

namespace FenUISharp.Objects.Buttons
{
    public abstract class SelectableButton : Button
    {
        private bool _isSelected = false;
        public bool IsSelected { get => _isSelected; set { SetSelected(value); } }
        public bool CanUnselect { get; set; } = true;

        public State<SKColor> EnabledFillColor { get; set; }

        public FButtonGroup? ButtonGroup { get; internal set; }

        public Action<bool, SelectableButton>? OnSelectionChanged { get; set; }
        public Action<bool, SelectableButton>? OnSelectionChangedSilent { get; set; }
        public Action<bool, SelectableButton>? OnUserSelectionChanged { get; set; }

        public SelectableButton(Action? onClick = null, Action<bool, SelectableButton>? onSelectionChanged = null, Func<Vector2>? position = null, Func<Vector2>? size = null) : base(onClick, size, position)
        {
            OnSelectionChanged = onSelectionChanged;
            InteractiveSurface.EnableMouseActions.SetStaticState(true);

            EnabledFillColor = new(() => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.Primary, this, this);
        }

        public void SetSelected(bool isSelected)
        {
            _isSelected = isSelected;
            OnSelectionChanged?.Invoke(isSelected, this);
            Invalidate(Invalidation.SurfaceDirty);
        }

        public void SilentSetSelected(bool isSelected)
        {
            _isSelected = isSelected;
            OnSelectionChangedSilent?.Invoke(isSelected, this);
            Invalidate(Invalidation.SurfaceDirty);
        }

        protected override void OnInteract()
        {
            base.OnInteract();

            IsSelected = (IsSelected && CanUnselect) ? !IsSelected : true;
            OnUserSelectionChanged?.Invoke(IsSelected, this);
            Invalidate(Invalidation.SurfaceDirty);
        }

        public override void Render(SKCanvas canvas)
        {
            if (IsSelected)
            {
                using (var path = SKSquircle.CreateSquircle(Shape.LocalBounds, CornerRadius.CachedValue))
                {
                    using var paint = GetRenderPaint();

                    var colorBefore = RenderMaterial.CachedValue.GetProp<Func<SKColor>>("BaseColor", null);
                    var colorBorderBefore = RenderMaterial.CachedValue.GetProp<Func<SKColor>>("BorderColor", null);

                    RenderMaterial.CachedValue.SetProp("BaseColor", () => EnabledFillColor.CachedValue);
                    RenderMaterial.CachedValue.SetProp("BorderColor", () => EnabledFillColor.CachedValue.AddMix(new(25, 25, 25)));
                    RenderMaterial.CachedValue.DrawWithMaterial(canvas, path, this, paint);

                    RenderMaterial.CachedValue.SetProp("BaseColor", colorBefore);
                    RenderMaterial.CachedValue.SetProp("BorderColor", colorBorderBefore);
                }
            }
            else
                base.Render(canvas);
        }
    }
}