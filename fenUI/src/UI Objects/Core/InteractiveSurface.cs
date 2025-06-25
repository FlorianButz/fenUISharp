using FenUISharp.Mathematics;
using FenUISharp.States;
using FenUISharp.WinFeatures;
using SkiaSharp;

namespace FenUISharp.Objects
{
    public class InteractiveSurface : IDisposable
    {
        public UIObject Owner { get; init; }
        public State<SKRect> GlobalSurface { get; init; }
        private Dispatcher Dispatcher { get; init; }

        private string uniqueID = Guid.NewGuid().ToString();

        public State<int> ExtendInteractionRadius { get; init; }

        public State<bool> IgnoreInteractions { get; init; }
        public State<bool> IgnoreChildInteractions { get; init; }

        public State<bool> EnableMouseActions { get; init; } // Both for dragging and mouse actions
        public State<bool> EnableMouseScrolling { get; init; } // Only for scrolling

        public bool IsMouseHovering { get; set; }
        public bool IsMouseDown { get; set; }
        public bool IsDragging { get; set; }

        // Mouse Actions

        public Action? OnMouseEnter { get; set; }
        public Action? OnMouseStay { get; set; }
        public Action? OnMouseExit { get; set; }

        public Action<MouseInputCode>? OnMouseAction { get; set; }

        // Scrolling

        public Action<float>? OnMouseScroll { get; set; }

        private volatile float _lastDelta = 0f; // Makes sure to catch all scrolling events between frames

        // Dragging

        public Action<Vector2>? OnDrag { get; set; } // Returns the delta between current mouse position and the drag start position
        public Action<Vector2>? OnDragDelta { get; set; } // Returns the delta between the current mouse position and the last one
        public Action? OnDragStart { get; set; }
        public Action? OnDragEnd { get; set; }

        private Vector2 _startGlobalMousePos;
        private Vector2 _lastGlobalMousePos;


        private bool ParentIgnoreChild { get => (Owner.Parent?.InteractiveSurface.IgnoreChildInteractions.CachedValue ?? false) || (Owner.Parent?.InteractiveSurface.ParentIgnoreChild ?? false); }

        [ThreadStatic]
        private static List<InteractiveSurface> _surfaces = new();
        
        [ThreadStatic]
        private static InteractiveSurface? _topmostSurface;

        public InteractiveSurface(UIObject owner, Dispatcher dispatcher, Func<SKRect> globalSurface)
        {
            this.Owner = owner;
            GlobalSurface = new(globalSurface, (x) => { });
            this.Dispatcher = dispatcher;

            ExtendInteractionRadius = new(() => 0, (x) => { });
            IgnoreInteractions = new(() => false, (x) => { });
            IgnoreChildInteractions = new(() => false, (x) => { });

            EnableMouseActions = new(() => false, (x) => { });
            EnableMouseScrolling = new(() => false, (x) => { });

            FContext.GetCurrentWindow().MouseAction += FuncOnMouseAction;
            WindowFeatures.GlobalHooks.OnMouseScroll += Global_FuncOnMouseScroll;
            WindowFeatures.GlobalHooks.OnMouseMove += Global_FuncOnMouseMove;
            WindowFeatures.GlobalHooks.OnMouseAction += FuncOnMouseActionGlobal;

            _surfaces.Add(this);
        }

        private void FuncOnMouseActionGlobal(MouseInputCode code)
        {
            Dispatcher.InvokeWithID(() => FuncOnMouseMoveGlobal(code), $"{uniqueID}-globalmousemove");
        }

        private void Global_FuncOnMouseScroll(float obj)
        {
            _lastDelta += obj;
            Dispatcher.InvokeWithID(() => FuncOnMouseScroll(), $"{uniqueID}-mousescroll");
        }

        private void Global_FuncOnMouseMove(Vector2 vector)
        {
            Dispatcher.InvokeWithID(() => FuncOnMouseMove(), $"{uniqueID}-mousemove");
        }

        private void FuncOnMouseScroll()
        {
            if (!FContext.GetCurrentWindow().IsWindowFocused || !TestIfTopMost_MouseScrolling() || !TestForGlobalPoint(FContext.GetCurrentWindow().ClientMousePosition)) return;

            // TODO: Fix delta being accumulated when outside of client area and unfocused

            OnMouseScroll?.Invoke(_lastDelta);
            _lastDelta = 0;
        }

        private void FuncOnMouseMove()
        {
            FuncProcessDrag();

            if (!TestIfTopMost_MouseInteraction() || !TestForGlobalPoint(FContext.GetCurrentWindow().ClientMousePosition))
            {
                if (IsMouseHovering)
                {
                    if (_topmostSurface == this) _topmostSurface = null;
                    IsMouseHovering = false;
                    OnMouseExit?.Invoke();
                }
                return;
            }

            if (!IsMouseHovering)
            {
                OnMouseEnter?.Invoke();
                _topmostSurface = this;
            }

            OnMouseStay?.Invoke();
            IsMouseHovering = true;

        }

        private void FuncProcessDrag()
        {
            if (IsMouseDown && !IsDragging && IsMouseHovering /* Technically not needed, but helps with readability */)
            {
                _startGlobalMousePos = FContext.GetCurrentWindow().ClientMousePosition;
                _lastGlobalMousePos = FContext.GetCurrentWindow().ClientMousePosition;

                OnDragStart?.Invoke();
                IsDragging = true;
            }

            if (IsDragging)
            {
                OnDrag?.Invoke(FContext.GetCurrentWindow().ClientMousePosition - _startGlobalMousePos);
                OnDragDelta?.Invoke(FContext.GetCurrentWindow().ClientMousePosition - _lastGlobalMousePos);
                _lastGlobalMousePos = FContext.GetCurrentWindow().ClientMousePosition;
            }
        }

        private void StopDragging()
        {
            _startGlobalMousePos = new(0, 0);
            _lastGlobalMousePos = new(0, 0);

            OnDragEnd?.Invoke();
            IsDragging = false;
        }

        private void FuncOnMouseMoveGlobal(MouseInputCode code)
        {
            if (code.button == MouseInputButton.Left && code.state == MouseInputState.Up && IsMouseDown)
            {
                IsMouseDown = false;

                if (IsDragging)
                {
                    StopDragging();
                }
            }
        }

        private void FuncOnMouseAction(MouseInputCode code)
        {
            // Dragging

            if (TestIfTopMost_Dragging() && TestForGlobalPoint(FContext.GetCurrentWindow().ClientMousePosition))
            {
                if (IsDragging)
                {
                    StopDragging();
                }
            }

            // Mouse actions

            if (!TestIfTopMost_MouseInteraction() || !TestForGlobalPoint(FContext.GetCurrentWindow().ClientMousePosition)) return;

            if (code.button == MouseInputButton.Left && code.state == MouseInputState.Down) IsMouseDown = true;
            else if (code.button == MouseInputButton.Left && code.state == MouseInputState.Up) IsMouseDown = false;

            OnMouseAction?.Invoke(code);
        }

        // Helper testing functions

        public bool TestForGlobalPoint(Vector2 point)
        {
            return GetGlobalInteractionRect().Contains(point.x, point.y) && (Owner.Parent != null ? Owner.Parent.InteractiveSurface.TestForGlobalPoint(point) : true);
        }

        private bool TestIfTopMost_MouseInteraction()
        {
            List<UIObject> ordered = Owner.Composition.GetZOrderedListOfEverything(FContext.GetRootViewPane());

            // Goes in reverse Z-order (front to back)
            for (int i = ordered.Count - 1; i >= 0; i--)
            {
                var obj = ordered[i];
                var surfaces = _surfaces.Where(s =>
                    s.Owner == obj &&
                    s.Owner.GlobalEnabled &&
                    !s.IgnoreInteractions.CachedValue &&
                    !s.ParentIgnoreChild && 
                    s.EnableMouseActions.CachedValue &&
                    s.TestForGlobalPoint(FContext.GetCurrentWindow().ClientMousePosition) // <- Important!
                ).ToList();

                if (surfaces.Count > 0)
                {
                    // First match is the topmost valid surface under the mouse
                    return surfaces.Contains(this);
                }
            }

            return false;
        }

        // Separate function because buttons will be interactive, however should not block scrolling. I have no idea how actual frameworks handle this, so this has to do for now
        private bool TestIfTopMost_MouseScrolling()
        {
            List<UIObject> ordered = Owner.Composition.GetZOrderedListOfEverything(FContext.GetRootViewPane());

            // Goes in reverse Z-order (front to back)
            for (int i = ordered.Count - 1; i >= 0; i--)
            {
                var obj = ordered[i];
                var surfaces = _surfaces.Where(s =>
                    s.Owner == obj &&
                    s.Owner.GlobalEnabled &&
                    !s.IgnoreInteractions.CachedValue &&
                    !s.ParentIgnoreChild &&
                    s.EnableMouseScrolling.CachedValue &&
                    s.TestForGlobalPoint(FContext.GetCurrentWindow().ClientMousePosition) // <- Important!
                ).ToList();

                if (surfaces.Count > 0)
                {
                    // First match is the topmost valid surface under the mouse
                    return surfaces.Contains(this);
                }
            }

            return false;
        }

        private bool TestIfTopMost_Dragging()
        {
            Dictionary<UIObject, InteractiveSurface> map = new();

            // Build the map of UIObjects to their surfaces
            Owner.Composition.GetZOrderedListOfEverything(FContext.GetRootViewPane())
                .ForEach(y => _surfaces.FindAll(x => x.Owner == y).ForEach(z => map[y] = z));

            // Check if the last element matches the current one
            var last = map.LastOrDefault(x =>
                x.Value != null &&
                x.Key != null &&
                x.Value.Owner.GlobalEnabled &&
                !x.Value.ParentIgnoreChild &&
                !x.Value.IgnoreInteractions.CachedValue &&
                x.Key == Owner &&
                x.Value.EnableMouseActions.CachedValue
            );

            // If it's a match, this surface is the topmost for scrolling specifically
            return last.Value == this;
        }

        public SKRect GetGlobalInteractionRect()
        {
            var global = GlobalSurface.CachedValue;
            global.Inflate(ExtendInteractionRadius.CachedValue, ExtendInteractionRadius.CachedValue);

            return global;
        }

        public void Dispose()
        {
            _surfaces.Remove(this);

            FContext.GetCurrentWindow().MouseAction -= FuncOnMouseAction;
            WindowFeatures.GlobalHooks.OnMouseMove -= Global_FuncOnMouseMove;

            IgnoreInteractions.Dispose();
            IgnoreChildInteractions.Dispose();
            EnableMouseActions.Dispose();
            EnableMouseScrolling.Dispose();
        }
    }
}