using FenUISharp.Behavior;
using FenUISharp.Mathematics;
using FenUISharp.Objects.Buttons;
using FenUISharp.Objects.Text;
using FenUISharp.Objects.Text.Model;
using FenUISharp.States;
using SkiaSharp;

namespace FenUISharp.Objects
{
    public class FSegmentedControl : FPanel
    {
        private SelectPaneModel _model;
        public SelectPaneModel Model { get => _model; set => SetModel(value); }

        private Dictionary<FSegmentedSelectionPaneSelectableButton, Action<int>> instantiatedTextButtons = new();
        private StackContentComponent layout;
        private FButtonGroup buttonGroup;

        public State<SKColor> Selection { get; init; }
        public State<SKColor> Background { get; init; }
        public State<SKColor> Text { get; init; }
        public State<SKColor> TextSelected { get; init; }

        private SKPath? selectionPath;
        private Spring rectSpringXY;
        private Spring rectSpringWH;

        private int lastSelection = -1;

        public Action<int>? OnUserSelectionChanged { get; set; }
        public Action<int>? OnSelectionChanged { get; set; }

        public FSegmentedControl(SelectPaneModel model, int initiallySelected)
        {
            layout = new StackContentComponent(this, StackContentComponent.ContentStackType.Horizontal, StackContentComponent.ContentStackBehavior.SizeToFitAll);
            layout.Pad.SetStaticState(4);
            layout.Gap.SetStaticState(2);

            rectSpringXY = new(3f, 2f);
            rectSpringWH = new(3f, 2f);

            buttonGroup = new();
            buttonGroup.AlwaysMustSelectOne = true;
            buttonGroup.AllowMultiSelect = false;

            buttonGroup.OnUserSelectionChanged += (x) => OnSlcChanged(buttonGroup.LatestSelection);
            buttonGroup.OnUserSelectionChanged += (x) => OnAnySlcChanged(buttonGroup.LatestSelection);
            buttonGroup.OnSelectionChanged += (x) => OnSelectionChanged?.Invoke(buttonGroup.LatestSelection);
            buttonGroup.OnSelectionChanged += (x) => OnAnySlcChanged(buttonGroup.LatestSelection);

            InteractiveSurface.MouseInteractionCallbackOnChildMouseInteraction = true;

            Model = model; // First create the buttons
            buttonGroup.Select(initiallySelected); // Then activate initial selection

            Selection = new(() => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.Secondary, this);
            Background = new(() => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.Surface, this);
            Text = new(() => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.OnSurface, this);
            TextSelected = new(() => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.OnSurface, this);

            RenderMaterial.Value = () => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.InteractableMaterial().WithOverride(new()
            {
                ["BaseColor"] = () => Background.CachedValue
            });
        }

        public void SetSelected(int index)
        {
            buttonGroup.Buttons[index].SetSelected(true);
        }

        public void SilentSetSelected(int index)
        {
            buttonGroup.Buttons[index].SilentSetSelected(true);

            var globalBounds = buttonGroup.Buttons[buttonGroup.LatestSelection].Shape.GlobalBounds;
            globalBounds.Inflate(-buttonGroup.Buttons[buttonGroup.LatestSelection].Padding.CachedValue, -buttonGroup.Buttons[buttonGroup.LatestSelection].Padding.CachedValue);
            var bounds = Transform.GlobalToDrawLocal(globalBounds);

            rectSpringXY.ResetVector(new(bounds.Left, bounds.Top));
            rectSpringWH.ResetVector(new(bounds.Width, bounds.Height));

            Invalidate(Invalidation.SurfaceDirty);
        }

        private void OnSlcChanged(int index)
        {
            int beforeSelection = lastSelection;

            FSegmentedSelectionPaneSelectableButton selectedButton = (FSegmentedSelectionPaneSelectableButton)buttonGroup.Buttons[index];
            instantiatedTextButtons.TryGetValue(selectedButton, out var action);

            if (lastSelection != index)
            {
                action?.Invoke(index);
                OnSelectionChanged?.Invoke(index);
                lastSelection = index;
            }
        }

        private void OnAnySlcChanged(int index)
        {
            lastIndex = index;
            UpdateText();
        }

        private void UpdateText()
        {
            // Make sure to update selected text buttons (for color updates)
            foreach (var item in instantiatedTextButtons)
                item.Key.Label.Invalidate(Invalidation.SurfaceDirty);
        }

        private int lastIndex;
        private void OnMouseDownSubControl(int index)
        {
            lastIndex = index;
            UpdateText();
        }

        private Func<SKRect> lastGlobalRect;
        private void OnMouseEnterSubControl(FSegmentedSelectionPaneSelectableButton button)
        {
            lastGlobalRect = () => button.Shape.GlobalBounds;

            if (InteractiveSurface.IsMouseDown)
            {
                lastIndex = buttonGroup.Buttons.IndexOf(button);
                UpdateText();
            }
        }

        private void SetModel(SelectPaneModel value)
        {
            _model = value;
            ClearOld();

            int index = 0;
            foreach (var toInstantiate in _model.ValuePairs)
            {
                var capturedIndex = index;
                var instance = new FSegmentedSelectionPaneSelectableButton(new FText(TextModelFactory.CopyBasic(toInstantiate.Key, textColor: () => lastIndex == capturedIndex ? TextSelected.CachedValue : Text.CachedValue)));
                instantiatedTextButtons.Add(instance, toInstantiate.Value);
                instance.SetParent(this);

                var capturedInstance = instance;
                instance.InteractiveSurface.OnMouseEnter += () =>
                {
                    OnMouseEnterSubControl(capturedInstance);
                };
                instance.InteractiveSurface.OnMouseAction += (x) =>
                {
                    if(x.state == 0 && x.button == 0)
                        OnMouseDownSubControl(capturedIndex);
                };

                instance.CornerRadius.SetResponsiveState(() => CornerRadius.CachedValue);

                buttonGroup.Add(instance);
                index++;
            }

            layout.FullUpdateLayout();
        }

        private void ClearOld()
        {
            foreach (var instance in instantiatedTextButtons)
            {
                buttonGroup.Remove(instance.Key);
                instance.Key.Dispose();
            }
            instantiatedTextButtons = new();
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();

            var lastPos = rectSpringXY.GetLastValue();

            var globbounds = InteractiveSurface.IsMouseDown ? lastGlobalRect() : buttonGroup.Buttons[buttonGroup.LatestSelection].Shape.GlobalBounds;
            globbounds.Inflate(-buttonGroup.Buttons[buttonGroup.LatestSelection].Padding.CachedValue, -buttonGroup.Buttons[buttonGroup.LatestSelection].Padding.CachedValue);
            var bounds = Transform.GlobalToDrawLocal(globbounds);

            float sizeOff = InteractiveSurface.IsMouseDown ? 4 : InteractiveSurface.IsMouseHovering ? 0 : 0;

            var pos = rectSpringXY.Update(FContext.DeltaTime, new(bounds.Left - sizeOff, -sizeOff));
            var size = rectSpringWH.Update(FContext.DeltaTime, new(bounds.Width + sizeOff * 2, bounds.Height + sizeOff * 2));
            SKRect rect = SKRect.Create(pos.x, bounds.Top + pos.y, size.x, size.y);

            selectionPath?.Dispose();
            selectionPath = SKSquircle.CreateSquircle(rect, CornerRadius.CachedValue + (InteractiveSurface.IsMouseDown ? CornerRadius.CachedValue : 0));

            if (lastPos.Magnitude != pos.Magnitude)
                Invalidate(Invalidation.SurfaceDirty);
        }

        public override void Render(SKCanvas canvas)
        {
            base.Render(canvas);

            if (selectionPath == null) return;

            using var paint = GetRenderPaint();
            RenderMaterial.CachedValue.WithOverride(new()
            {
                ["BaseColor"] = () => Selection.CachedValue
            }).DrawWithMaterial(canvas, selectionPath, this, paint);
        }
    }

    public struct SelectPaneModel
    {
        public SelectPaneModel(Dictionary<TextModel, Action<int>> ValuePairs)
        {
            this.ValuePairs = ValuePairs;
        }

        public Dictionary<TextModel, Action<int>> ValuePairs { get; init; }
    }
}