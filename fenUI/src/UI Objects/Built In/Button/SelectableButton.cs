using FenUISharp.Mathematics;
using SkiaSharp;

namespace FenUISharp.Objects.Buttons
{
    public abstract class SelectableButton : Button
    {
        private bool _isSelected = false;
        public bool IsSelected { get => _isSelected; set { SetSelected(value); } }
        public bool CanUnselect { get; set; } = true;

        public FButtonGroup? ButtonGroup { get; internal set; }

        public Action<bool>? OnSelectionChanged { get; set; }
        public Action<bool>? OnSelectionChangedSilent { get; set; }
        public Action<bool>? OnUserSelectionChanged { get; set; }

        public SelectableButton(Action? onClick = null, Action<bool>? onSelectionChanged = null, Func<Vector2>? position = null, Func<Vector2>? size = null) : base(onClick, size, position)
        {
            OnSelectionChanged = onSelectionChanged;
            InteractiveSurface.EnableMouseActions.SetStaticState(true);
        }

        public void SetSelected(bool isSelected)
        {
            _isSelected = isSelected;
            OnSelectionChanged?.Invoke(isSelected);
            Invalidate(Invalidation.SurfaceDirty);
        }

        public void SilentSetSelected(bool isSelected)
        {
            _isSelected = isSelected;
            OnSelectionChangedSilent?.Invoke(isSelected);
            Invalidate(Invalidation.SurfaceDirty);
        }
    
        protected override void MouseAction(MouseInputCode inputCode)
        {
            base.MouseAction(inputCode);

            if (inputCode.button == MouseInputButton.Left && inputCode.state == MouseInputState.Up)
            {
                IsSelected = (IsSelected && CanUnselect) ? !IsSelected : true;
                OnUserSelectionChanged?.Invoke(IsSelected);
                Invalidate(Invalidation.SurfaceDirty);
            }
        }
    }
}