using FenUISharp.Components;
using FenUISharp.Mathematics;
using SkiaSharp;

namespace FenUISharp.Components.Buttons
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
        
        public SelectableButton(Window rootWindow, Vector2 position, Vector2 size, Action? onClick = null, Action<bool>? onSelectionChanged = null) : base(rootWindow, position, size, onClick)
        {
            OnSelectionChanged = onSelectionChanged;
        }

        public void SetSelected(bool isSelected)
        {
            _isSelected = isSelected;
            OnSelectionChanged?.Invoke(isSelected);
            Invalidate();
        }

        public void SilentSetSelected(bool isSelected)
        {
            _isSelected = isSelected;
            OnSelectionChangedSilent?.Invoke(isSelected);
            Invalidate();
        }
    
        protected override void MouseAction(MouseInputCode inputCode)
        {
            base.MouseAction(inputCode);

            if (inputCode.button == 0 && inputCode.state == 1)
            {
                IsSelected = (IsSelected && CanUnselect) ? !IsSelected : true;
                OnUserSelectionChanged?.Invoke(IsSelected);
                Invalidate();
            }
        }
    }
}