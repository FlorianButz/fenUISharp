using FenUISharp.Mathematics;
using FenUISharp.States;
using FenUISharp.WinFeatures;
using SkiaSharp;

namespace FenUISharp.Objects
{
    public class InteractiveSurface : IDisposable
    {
        public WeakReference<UIObject> Owner { get; private set; }
        private UIObject? owner
        {
            get
            {
                if (Owner?.TryGetTarget(out var target) ?? false) return target;

                else return null;
            }
        }

        public State<SKRect> GlobalSurface
        { get; init; }
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

        /// <summary>
        /// Specifies the time frame in which a mouse action has to be completed twice to be counted as a double mouse action
        /// </summary>
        public float DoubleMouseActionTimeFrame { get; set; } = 0.4f;

        /// <summary>
        /// Specifies the time in which a mouse action has to be held to be counted as a long mouse action
        /// </summary>
        public float LongMouseActionTime { get; set; } = 1f;

        public bool MouseInteractionCallbackOnChildMouseInteraction { get; set; } = false;

        // Mouse Actions

        public Action? OnMouseEnter { get; set; }
        public Action? OnMouseStay { get; set; }
        public Action? OnMouseExit { get; set; }

        public Action<Vector2>? OnMouseMove { get; set; }

        public Action<MouseInputCode>? OnMouseAction { get; set; }

        // Advanced mouse action

        /// <summary>
        /// When a specific action is executed twice in quick succession such as a double left click
        /// </summary>
        public Action<MouseInputButton>? OnDoubleMouseAction { get; set; }

        /// <summary>
        /// When a specific action is held down for a long time
        /// </summary>
        public Action<MouseInputButton>? OnLongMouseAction { get; set; }

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

        private MouseInputCode _lastInputMouseState;
        private DateTime _lastInputMouseStateTime;
        private int _lastInputID = 0; // Used for long press detection

        private bool ParentIgnoreChild { get => (owner?.Parent?.InteractiveSurface.IgnoreChildInteractions.CachedValue ?? false) || (owner?.Parent?.InteractiveSurface.ParentIgnoreChild ?? false); }

        [ThreadStatic]
        private static List<InteractiveSurface> _surfaces;

        [ThreadStatic]
        private static InteractiveSurface? _topmostSurface;
        [ThreadStatic]
        private static InteractiveSurface? _topmostSurfaceMouseAction;
        [ThreadStatic]
        private static InteractiveSurface? _topmostSurfaceMouseScroll;

        [ThreadStatic]
        private static int activeInstances = 0;

        public InteractiveSurface(UIObject owner, Dispatcher dispatcher, Func<SKRect> globalSurface)
        {
            this.Owner = new(owner);
            GlobalSurface = new(globalSurface, owner, (x) => { });
            this.Dispatcher = dispatcher;

            ExtendInteractionRadius = new(() => 2, owner, (x) => { });
            IgnoreInteractions = new(() => false, owner, (x) => { });
            IgnoreChildInteractions = new(() => false, owner, (x) => { });

            EnableMouseActions = new(() => false, owner, (x) => { });
            EnableMouseScrolling = new(() => false, owner, (x) => { });

            FContext.GetCurrentWindow().Callbacks.ClientMouseAction += FuncOnMouseAction;
            FContext.GetCurrentWindow().Callbacks.OnMouseScroll += Global_FuncOnMouseScroll;
            FContext.GetCurrentWindow().Callbacks.OnMouseMove += Global_FuncOnMouseMove;
            WindowFeatures.GlobalHooks.OnMouseAction += FuncOnMouseActionGlobal;

            if (activeInstances == 0)
            {
                _surfaces = new();
                FContext.GetCurrentWindow().Callbacks.OnPreUpdate += CacheTopmostMouseAction;
                FContext.GetCurrentWindow().Callbacks.OnPreUpdate += CacheTopmostMouseScroll;
            }

            activeInstances++;

            _surfaces.Add(this);
        }

        private void CacheTopmostMouseAction()
        {
            var capturedOwner = owner;
            if (capturedOwner == null) return;

            List<UIObject> ordered = capturedOwner.Composition.GetZOrderedListOfEnabled();

            // Goes in reverse Z-order (front to back)
            for (int i = ordered.Count - 1; i >= 0; i--)
            {
                var obj = ordered[i];
                var surfaces = _surfaces.Where(s =>
                    s.owner == obj &&
                    s.owner.GlobalEnabled &&
                    !s.IgnoreInteractions.CachedValue &&
                    !s.ParentIgnoreChild &&
                    s.EnableMouseActions.CachedValue &&
                    s.TestForGlobalPoint(FContext.GetCurrentWindow().ClientMousePosition) // <- Important!
                ).ToList();

                if (surfaces.Count == 0) _topmostSurfaceMouseAction = null;
                else
                {
                    _topmostSurfaceMouseAction = surfaces[0];
                    break;
                }
            }
        }

        private void CacheTopmostMouseScroll()
        {
            var capturedOwner = owner;
            if (capturedOwner == null) return;

            List<UIObject> ordered = capturedOwner.Composition.GetZOrderedListOfEnabled();

            // Goes in reverse Z-order (front to back)
            for (int i = ordered.Count - 1; i >= 0; i--)
            {
                var obj = ordered[i];
                var surfaces = _surfaces.Where(s =>
                    s.owner == obj &&
                    s.owner.GlobalEnabled &&
                    !s.IgnoreInteractions.CachedValue &&
                    !s.ParentIgnoreChild &&
                    s.EnableMouseScrolling.CachedValue &&
                    s.TestForGlobalPoint(FContext.GetCurrentWindow().ClientMousePosition) // <- Important!
                ).ToList();

                if (surfaces.Count == 0) _topmostSurfaceMouseScroll = null;
                else
                {
                    _topmostSurfaceMouseScroll = surfaces[0];
                    break;
                }
            }
        }

        private void FuncOnMouseActionGlobal(MouseInputCode code)
        {
            var capturedOwner = owner;

            if (capturedOwner == null) return;
            if (!capturedOwner.GlobalEnabled || !capturedOwner.GlobalVisible) return;

            Dispatcher.InvokeWithID(() => FuncOnMouseMoveGlobal(code), $"{uniqueID}-globalmousemove");
        }

        private void Global_FuncOnMouseScroll(float obj)
        {
            var capturedOwner = owner;

            if (capturedOwner == null) return;
            if (!capturedOwner.GlobalEnabled || !capturedOwner.GlobalVisible) return;

            _lastDelta += obj;
            Dispatcher.InvokeWithID(() => FuncOnMouseScroll(), $"{uniqueID}-mousescroll");
        }

        private void Global_FuncOnMouseMove(Vector2 vector)
        {
            var capturedOwner = owner;

            if (capturedOwner == null) return;
            if (!capturedOwner.GlobalEnabled || !capturedOwner.GlobalVisible) return;

            Dispatcher.InvokeWithID(() => FuncOnMouseMove(), $"{uniqueID}-mousemove");
        }

        private void FuncOnMouseScroll()
        {
            var capturedOwner = owner;

            if (capturedOwner == null) return;
            if (!capturedOwner.GlobalEnabled || !capturedOwner.GlobalVisible) return;
            if (!FContext.GetCurrentWindow().Properties.IsWindowFocused || !TestIfTopMost_MouseScrolling() || !TestForGlobalPoint(FContext.GetCurrentWindow().ClientMousePosition)) return;

            OnMouseScroll?.Invoke(_lastDelta);
            _lastDelta = 0;
        }

        private void FuncOnMouseMove()
        {
            var capturedOwner = owner;

            if (capturedOwner == null) return;
            if (!capturedOwner.GlobalEnabled || !capturedOwner.GlobalVisible) return;
            
            OnMouseMove?.Invoke(FContext.GetCurrentWindow().ClientMousePosition);

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
            var capturedOwner = owner;

            if (capturedOwner == null) return;
            if (!capturedOwner.GlobalEnabled || !capturedOwner.GlobalVisible) return;

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
            var capturedOwner = owner;

            if (capturedOwner == null) return;
            if (!capturedOwner.GlobalEnabled || !capturedOwner.GlobalVisible) return;

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
            var capturedOwner = owner;

            if (capturedOwner == null) return;
            if (!capturedOwner.GlobalEnabled || !capturedOwner.GlobalVisible) return;

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

            // Special actions

            // Double mouse action
            if (_lastInputMouseState.button == code.button && code.state == MouseInputState.Up)
            {
                // Check if the action was executed in the given time frame
                if ((DateTime.Now - _lastInputMouseStateTime).TotalSeconds <= DoubleMouseActionTimeFrame)
                {
                    _lastInputMouseState = new() { button = MouseInputButton.None }; // Reset state
                    OnDoubleMouseAction?.Invoke(code.button);
                }

                _lastInputMouseState = code;
                _lastInputMouseStateTime = DateTime.Now;
            }

            // Long press
            if (code.state == MouseInputState.Down)
            {
                _lastInputID++;
                var id = _lastInputID;

                Dispatcher.InvokeLater(() =>
                {
                    if (_lastInputID == id)
                        OnLongMouseAction?.Invoke(code.button);
                }, LongMouseActionTime);
            }
        }

        // Helper testing functions

        public bool TestForGlobalPoint(in Vector2 point)
        {
            if (owner == null) return false;
            
            // return GetGlobalInteractionRect().Contains(point.x, point.y);
            return GetGlobalInteractionRect().Contains(point.x, point.y) && (owner.Parent != null ? owner.Parent.InteractiveSurface.TestForGlobalPoint(point) : true);
        }

        private bool TestIfTopMost_MouseInteraction()
        {
            var capturedOwner = owner;

            if (capturedOwner == null) return false;
            if (MouseInteractionCallbackOnChildMouseInteraction && capturedOwner.Children.Any(x => x.InteractiveSurface.TestIfTopMost_MouseInteraction()))
                return true;

            return _topmostSurfaceMouseAction == this;
        }

        // Separate function because buttons will be interactive, however should not block scrolling. I have no idea how actual frameworks handle this, so this has to do for now
        private bool TestIfTopMost_MouseScrolling()
        {
            return _topmostSurfaceMouseScroll == this;
        }

        // Should be same as TestIfTopMost_MouseInteraction. Leave it there though, might change in future
        private bool TestIfTopMost_Dragging()
        {
            return _topmostSurfaceMouseAction == this;
        }

        public SKRect GetGlobalInteractionRect()
        {
            var global = GlobalSurface.CachedValue;
            global.Inflate(ExtendInteractionRadius.CachedValue, ExtendInteractionRadius.CachedValue);

            return global;
        }

        public void Dispose()
        {
            if(_surfaces != null)
                _surfaces.Remove(this);
            Owner = null;

            if (!FContext.IsDisposingWindow)
            {
                FContext.GetCurrentWindow().Callbacks.ClientMouseAction -= FuncOnMouseAction;
                FContext.GetCurrentWindow().Callbacks.OnMouseScroll -= Global_FuncOnMouseScroll;
                FContext.GetCurrentWindow().Callbacks.OnMouseMove -= Global_FuncOnMouseMove;
                WindowFeatures.GlobalHooks.OnMouseAction -= FuncOnMouseActionGlobal;
            }

            activeInstances--;
            if (activeInstances <= 0 && !FContext.IsDisposingWindow)
            {
                FContext.GetCurrentWindow().Callbacks.OnPreUpdate += CacheTopmostMouseAction;
                FContext.GetCurrentWindow().Callbacks.OnPreUpdate += CacheTopmostMouseScroll;
            }
        }
    }
}