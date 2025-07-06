using FenUISharp.Objects;
using FenUISharp.WinFeatures;
using SkiaSharp;

namespace FenUISharp.Behavior
{
    public class SelectableComponent : BehaviorComponent
    {
        public InteractiveSurface Surface { get; init; }

        public bool IsSelected { get; private set; } = false;
        public Action? OnSelectionGained { get; set; }
        public Action? OnSelectionLost { get; set; }

        private KeyBind tabKeybind;
        private KeyBind reverseTabKeybind;

        [ThreadStatic]
        private static List<SelectableComponent> selectableComponents;

        [ThreadStatic]
        private static SelectableComponent? currentlySelected;
        [ThreadStatic]
        private static SelectableComponent? lastSelected;

        private Dispatcher? dispatcher;

        [ThreadStatic]
        private static bool renderSelection;

        public SelectableComponent(UIObject owner, InteractiveSurface surface) : base(owner)
        {
            this.Surface = surface;

            if (selectableComponents == null) selectableComponents = new();

            if (selectableComponents.Count <= 0)
            {
                dispatcher = FContext.GetCurrentDispatcher();
                WindowFeatures.GlobalHooks.OnMouseAction += OnGlobalClick;
            }
            selectableComponents.Add(this);

            tabKeybind = new() { VKCode = 0x0009, OnKeybindExecuted = OnTabPressed };
            reverseTabKeybind = new() { VKCode = 0x0009, Flags = KeyBindFlags.Shift, OnKeybindExecuted = OnReverseTabPressed };

            surface.OnMouseAction += OnClick;

            if (currentlySelected == null)
                SetSelected(this);
        }

        void OnClick(MouseInputCode c)
        {
            if (c.state == MouseInputState.Up && c.button == MouseInputButton.Left)
            {
                SetSelected(this);
                renderSelection = false;
            }
        }

        void OnGlobalClick(MouseInputCode c)
        {
            if (c.button == MouseInputButton.Left)
                renderSelection = false;

            if (c.button == MouseInputButton.Left && c.state == MouseInputState.Down)
                dispatcher?.Invoke(() => SetSelected(null));
        }

        public static void SetObjectSelected(UIObject? selectable)
        {
            if (selectable == null)
            {
                SetSelected(null);
                return;
            }

            if (selectable.BehaviorComponents.Any(x => x is SelectableComponent))
            {
                var sel = (SelectableComponent)selectable.BehaviorComponents.Last(x => x is SelectableComponent);
                SetSelected(sel);
            }
        }

        public static void SetSelected(SelectableComponent? selectableComponent)
        {
            lastSelected = currentlySelected;

            if (currentlySelected != null)
            {
                currentlySelected.IsSelected = false;
                currentlySelected.OnSelectionLost?.Invoke();
                currentlySelected.Owner.Invalidate(UIObject.Invalidation.SurfaceDirty);

                FContext.GetKeyboardInputManager().UnregisterKeybind(currentlySelected.tabKeybind);
                FContext.GetKeyboardInputManager().UnregisterKeybind(currentlySelected.reverseTabKeybind);
            }

            currentlySelected = selectableComponent;

            if (selectableComponent == null || currentlySelected == null) return;

            currentlySelected.IsSelected = true;
            currentlySelected.OnSelectionGained?.Invoke();
            currentlySelected.Owner.Invalidate(UIObject.Invalidation.SurfaceDirty);
            FContext.GetKeyboardInputManager().RegisterKeybind(currentlySelected.tabKeybind);
            FContext.GetKeyboardInputManager().RegisterKeybind(currentlySelected.reverseTabKeybind);

            if (lastSelected == null) lastSelected = currentlySelected;
        }

        public override void ComponentDestroy()
        {
            base.ComponentDestroy();

            if (currentlySelected == this) currentlySelected = null;
            Surface.OnMouseAction -= OnClick;

            selectableComponents.Remove(this);
            FContext.GetKeyboardInputManager().UnregisterKeybind(tabKeybind);
            
            if (selectableComponents.Count <= 0)
                WindowFeatures.GlobalHooks.OnMouseAction -= OnGlobalClick;
        }

        private void OnTabPressed()
        {
            renderSelection = true;

            if (!IsSelected || (currentlySelected == null && selectableComponents.IndexOf(this) == 0)) return;
            int currentIndex = ((currentlySelected == null) ? (lastSelected == null ? 0 : (selectableComponents.IndexOf(lastSelected) + 1)) : (selectableComponents.IndexOf(currentlySelected) + 1)) % selectableComponents.Count;
            if (currentIndex < 0) currentIndex = selectableComponents.Count-1;

            SetSelected(selectableComponents[currentIndex]);
        }

        private void OnReverseTabPressed()
        {
            renderSelection = true;

            if (!IsSelected || (currentlySelected == null && selectableComponents.IndexOf(this) == 0)) return;
            int currentIndex = ((currentlySelected == null) ? (lastSelected == null ? 0 : (selectableComponents.IndexOf(lastSelected) - 1)) : (selectableComponents.IndexOf(currentlySelected) - 1)) % selectableComponents.Count;
            if (currentIndex < 0) currentIndex = selectableComponents.Count-1;

            Console.WriteLine(currentIndex);
            SetSelected(selectableComponents[currentIndex]);
        }

        public override void HandleEvent(BehaviorEventType type, object? data = null)
        {
            base.HandleEvent(type, data);

            switch (type)
            {
                case BehaviorEventType.AfterRender:
                    if ((SKCanvas?)data != null)
                        RenderSelection((SKCanvas)data);
                    break;
            }
        }

        void RenderSelection(SKCanvas canvas)
        {
            if (!renderSelection) return;
            if (!IsSelected) return;

            var bounds = Owner.Shape.LocalBounds;
            bounds.Inflate(3, 3);
            using var path = SKSquircle.CreateSquircle(bounds, 10);

            using var paint = new SKPaint()
            {
                IsStroke = true,
                StrokeMiter = 3,
                PathEffect = SKPathEffect.CreateDash(new float[] { 2, 6 }, 1),
                IsAntialias = true,
                Color = new(150, 150, 150, 200)
            };

            canvas.DrawPath(path, paint);
        }
    }
}