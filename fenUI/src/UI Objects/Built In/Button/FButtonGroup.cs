using FenUISharp.Mathematics;
using SkiaSharp;

namespace FenUISharp.Objects.Buttons
{
    public class FButtonGroup
    {
        public List<SelectableButton> Buttons { get; } = new();

        private bool _allowMultiSelect = false;
        public bool AllowMultiSelect { get => _allowMultiSelect; set { _allowMultiSelect = value; UpdateButtonGroup(); } }

        private bool _alwaysMustSelectOne = false;
        public bool AlwaysMustSelectOne { get => _alwaysMustSelectOne; set { _alwaysMustSelectOne = value; UpdateButtonGroup(); } }

        // Will report only user made changes
        public Action<bool[]>? OnUserSelectionChanged { get; set; }

        // Will report any change
        public Action<bool[]>? OnSelectionChanged { get; set; }

        // Will return the latest selected. More useful for button groups that don't allow multi selection
        public int LatestSelection { get; private set; } = 0;

        public void Add(SelectableButton button)
        {
            Buttons.Add(button);

            if (AlwaysMustSelectOne && Buttons.Where(x => x.IsSelected).Count() == 0) button.SetSelected(true);
            else if (AlwaysMustSelectOne && Buttons.Where(x => x.IsSelected).Count() != 0 && !AllowMultiSelect) button.SetSelected(false);

            button.OnUserSelectionChanged += (x) => OnButtonChanged(x, button);
            button.ButtonGroup = this;
        }

        public void Remove(SelectableButton button)
        {
            Buttons.Remove(button);

            UpdateButtonGroup();

            button.OnUserSelectionChanged -= (x) => OnButtonChanged(x, button);
            if(button.ButtonGroup == this) button.ButtonGroup = null;
        }

        void OnButtonChanged(bool isSelected, SelectableButton button)
        {
            if (!AllowMultiSelect && isSelected) Buttons.ForEach(x => { if (x != button && x.IsSelected) x.SetSelected(false); });
            UpdateButtonGroup();

            LatestSelection = Buttons.IndexOf(button);

            bool[] buttons = new bool[Buttons.Count];
            for (int i = 0; i < Buttons.Count; i++) buttons[i] = Buttons[i].IsSelected;
            
            OnUserSelectionChanged?.Invoke(buttons);
            OnSelectionChanged?.Invoke(buttons);
        }

        void UpdateButtonGroup()
        {
            if (Buttons.Count == 0) return;

            bool[] beforeButtons = new bool[Buttons.Count];
            for (int i = 0; i < Buttons.Count; i++) beforeButtons[i] = Buttons[i].IsSelected;

            if (!AllowMultiSelect)
                Buttons.ForEach(x => { x.CanUnselect = !AlwaysMustSelectOne; });
            else
                Buttons.ForEach(x => { x.CanUnselect = !AlwaysMustSelectOne || Buttons.Where(x => x.IsSelected).Count() > 1; });

            if (!AllowMultiSelect && Buttons.Where(x => x.IsSelected).Count() > 1)
            {
                int c = 0;
                Buttons.ForEach(x => { if (x.IsSelected && c != 0) x.SetSelected(false); c++; });
            }
            else if (AlwaysMustSelectOne && Buttons.Where(x => x.IsSelected).Count() == 0) Buttons[0].SetSelected(true);

            bool[] afterButtons = new bool[Buttons.Count];
            for (int i = 0; i < Buttons.Count; i++) afterButtons[i] = Buttons[i].IsSelected;

            if (!afterButtons.SequenceEqual(beforeButtons)) OnSelectionChanged?.Invoke(afterButtons);
        }
    }
}