using FenUISharp.Behavior;
using FenUISharp.Mathematics;
using FenUISharp.Objects.Buttons;
using FenUISharp.Objects.Text;
using FenUISharp.Objects.Text.Model;
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
            buttonGroup.OnSelectionChanged += (x) => OnSelectionChanged?.Invoke(buttonGroup.LatestSelection);

            InteractiveSurface.MouseInteractionCallbackOnChildMouseInteraction = true;

            Model = model;

            // Need to wait 2 ticks for all the shapes to update properly. Otherwise it will still animate
            FContext.GetCurrentDispatcher().InvokeLater(() => SilentSetSelected(initiallySelected), 2L);
            FContext.GetCurrentDispatcher().InvokeLater(() => Invalidate(Invalidation.SurfaceDirty), 2L);

            RenderMaterial.Value = () => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.InteractableMaterial().WithOverride(new()
            {
                // TODO: Refactor to expose color
                ["BaseColor"] = () => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.Surface  
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
            FSegmentedSelectionPaneSelectableButton selectedButton = (FSegmentedSelectionPaneSelectableButton)buttonGroup.Buttons[index];
            instantiatedTextButtons.TryGetValue(selectedButton, out var action);

            if (lastSelection != index)
            {
                action?.Invoke(index);
                OnSelectionChanged?.Invoke(index);
                lastSelection = index;
            }
        }

        private SKRect lastGlobalRect;
        private void OnMouseEnterSubControl(FSegmentedSelectionPaneSelectableButton button)
        {
            lastGlobalRect = button.Shape.GlobalBounds;
        }

        private void SetModel(SelectPaneModel value)
        {
            _model = value;
            ClearOld();

            int index = 0;
            foreach (var toInstantiate in _model.ValuePairs)
            {
                // TODO: Refactor to expose text color
                var instance = new FSegmentedSelectionPaneSelectableButton(new FText(TextModelFactory.CopyBasic(toInstantiate.Key, textColor: () => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.OnSurface)));
                instantiatedTextButtons.Add(instance, toInstantiate.Value);
                instance.SetParent(this);

                var capturedInstance = instance;
                instance.InteractiveSurface.OnMouseEnter += () =>
                {
                    OnMouseEnterSubControl(capturedInstance);
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

        protected override void Update()
        {
            base.Update();

            var lastPos = rectSpringXY.GetLastValue();

            var globbounds = InteractiveSurface.IsMouseDown ? lastGlobalRect : buttonGroup.Buttons[buttonGroup.LatestSelection].Shape.GlobalBounds;
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
            base.Render(canvas); // TODO: Enable again later

            if (selectionPath == null) return;

            using var paint = GetRenderPaint();
            RenderMaterial.CachedValue.WithOverride(new()
            {
                // TODO: Refactor to expose this color
                ["BaseColor"] = () => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.Secondary
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