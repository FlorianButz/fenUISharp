using FenUISharp.Behavior;
using FenUISharp.Behavior.Layout;
using FenUISharp.Behavior.RuntimeEffects;
using FenUISharp.Materials;
using FenUISharp.Mathematics;
using FenUISharp.States;
using FenUISharp.Themes;
using SkiaSharp;

namespace FenUISharp.Objects
{
    public abstract class UIObject : IDisposable, IStateListener
    {
        public Transform Transform { get; init; }
        public Layout Layout { get; init; }
        public Shape Shape { get; init; }
        public Compositor Composition { get; init; }
        public InteractiveSurface InteractiveSurface { get; init; }
        public ImageEffects ImageEffects { get; init; }

        public State<Material> RenderMaterial { get; init; }

        internal List<BehaviorComponent> BehaviorComponents { get; private set; }

        public UIObject? Parent { get; private set; }
        public List<UIObject> Children { get; private set; }

        public State<bool> Enabled { get; init; }
        public State<bool> Visible { get; init; }

        public bool IsParentRoot { get => Parent == FContext.GetRootViewPane(); }

        /// <summary>
        /// This is highly recommended and disabling it can lead to very poor performance
        /// </summary>
        public bool DisableWhenOutOfParentBounds { get; set; } = true;

        public bool GlobalEnabled { get => Enabled.CachedValue && (Parent?.Enabled.CachedValue ?? true) && (DisableWhenOutOfParentBounds ? _insideParent : true); }
        public bool GlobalVisible { get => Visible.CachedValue && (Parent?.Visible.CachedValue ?? true) && (DisableWhenOutOfParentBounds ? _insideParent : true); }

        private bool _wasBeginCalled = false;
        private bool _insideParent = true;

        [Flags]
        public enum Invalidation
        {
            Clean = 0,
            ChildDirty = 1 << 0,
            TransformDirty = 1 << 1,
            SurfaceDirty = 1 << 2,
            LayoutDirty = 1 << 3,
            All = ChildDirty | TransformDirty | SurfaceDirty | LayoutDirty
        }

        public Invalidation InvalidationState { get; private set; } // TODO: Fix in the future that state will not represent a child update when it was added as parent after constructor
        public bool WindowRedrawThisObject { get; internal set; }

        public State<float> Quality { get; init; }
        public State<int> Padding { get; init; }

        internal CachedSurface _objectSurface;
        internal CachedSurface _childSurface;

        public Action? OnObjectDisposed { get; set; }

        public UIObject(Func<Vector2>? position = null, Func<Vector2>? size = null)
        {
            if (!FContext.IsValidContext()) throw new Exception("Invalid FenUISharp window context.");
            FContext.GetCurrentWindow().WindowThemeManager.ThemeChanged += OnThemeChanged;

            RenderMaterial = new(FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.DefaultMaterial, this);

            Enabled = new(() => true, this);
            Visible = new(() => true, this);

            Transform = new(this);
            Layout = new(this);
            Shape = new(this);
            Composition = new(this);
            InteractiveSurface = new(this, FContext.GetCurrentDispatcher(), () => Transform.DrawLocalToGlobal(Shape.LocalBounds));

            Quality = new(() => 1f, (x) => Invalidate(Invalidation.SurfaceDirty));
            Quality.SetResolver(StateResolverTemplates.SmallestFloatResolver);

            Padding = new(() => 2, (x) => Invalidate(Invalidation.SurfaceDirty)); // Default to use 2 padding. Helps to reduce sharp edges on lower quality settings
            Padding.SetResolver(StateResolverTemplates.BiggestIntResolver);

            Transform.LocalPosition.Value = position ?? (() => Vector2.Zero);
            Transform.Size.Value = size ?? (() => new(100, 100));

            _objectSurface = new(RenderToSurface);
            _childSurface = new(DrawChildren);

            // Default to using window view pane as parent
            SetParent(FContext.GetRootViewPane());

            BehaviorComponents = new();
            Children = new();

            ImageEffects = new(this);

            // Make sure to initially invalidate
            Invalidate(Invalidation.All);
        }

        public void CheckIfObjectMustBeDisabled()
        {
            if (!DisableWhenOutOfParentBounds) return;

            _insideParent = RMath.IsRectPartiallyInside(Parent?.Shape.GlobalBounds ?? FContext.GetCurrentWindow().Bounds, Shape.GlobalBounds);
            CheckIfSurfaceCanBeDisposed();
        }

        protected virtual void OnThemeChanged()
        {
            // On theme changed
            // RenderMaterial.ReevaluateValue(true);
            Invalidate(Invalidation.SurfaceDirty);
        }

        public void RecursiveInvalidate(Invalidation invalidation)
        {
            Invalidate(invalidation);
            Children.ToList().ForEach(x => x.RecursiveInvalidate(invalidation));
        }

        public void Invalidate(Invalidation invalidation)
        {
            if (invalidation == Invalidation.LayoutDirty)
            {
                // Shallow layout recalculation. Might have to change it to full recursive layout recalc in the future
                if (Parent != null)
                    Parent.InvalidationState |= Invalidation.LayoutDirty;
                Composition.GetZOrderedListOfChildren(this).ForEach(x => x.InvalidationState |= Invalidation.LayoutDirty);

                // Update layout components:
                List<LayoutComponent> layoutComponents = new List<LayoutComponent>();
                layoutComponents = SearchForLayoutComponentsRecursive(Parent ?? this);
                layoutComponents.Reverse();
                layoutComponents.ForEach(x => x.FullUpdateLayout());
            }

            InvalidationState |= invalidation;
            Parent?.Invalidate(Invalidation.ChildDirty);

            if (InvalidationState != Invalidation.Clean && InvalidationState != Invalidation.ChildDirty) WindowRedrawThisObject = true;

            OnInvalidate(invalidation);
        }

        private List<LayoutComponent> SearchForLayoutComponentsRecursive(UIObject obj)
        {
            List<LayoutComponent> returnList = new();

            obj.BehaviorComponents.ForEach((x) => { if (x is LayoutComponent) returnList.Add((LayoutComponent)x); });
            obj.Children.ForEach((x) => x.SearchForLayoutComponentsRecursive(x).ForEach((y) =>
            {
                if (!returnList.Contains((LayoutComponent)y)) returnList.Add((LayoutComponent)y);
            }));

            return returnList;
        }

        protected virtual void OnInvalidate(Invalidation invalidation)
        {
            // On invalidation
        }

        public void ClearInvalidation(Invalidation invalidation = Invalidation.All)
        {
            InvalidationState &= ~invalidation; // I hope this works, maybe I'm stupid who knows
        }

        public void SetParent(UIObject? parent)
        {
            if (Parent != null) Parent.Children.Remove(this);

            // Make sure to automatically parent to the root view pane if parent is cleared. A UIObject should NEVER not have a parent (except the root)
            if (parent == null) Parent = FContext.GetRootViewPane();
            else
            {
                Parent = parent;
                parent.Children.Add(this);
            }
        }

        void RemoveFromParent()
        {
            if (Parent != null) Parent.Children.Remove(this);
        }

        protected SKPaint GetRenderPaint()
        {
            return new()
            {
                IsAntialias = true,
                Color = SKColors.White
            };
        }

        protected SKPaint GetDrawPaint()
        {
            return new()
            {
                IsAntialias = true,
                Color = SKColors.White
            };
        }

        public virtual void Begin()
        {
            // Triggered once on the first frame update call of being instantiated
        }

        public virtual void LateBegin()
        {
            // Same as begin, however runs after first update
        }

        public void OnUpdate()
        {
            if (!Enabled.CachedValue && _wasBeginCalled) return;

            DispatchBehaviorEvent(BehaviorEventType.BeforeUpdate);

            if (!_wasBeginCalled)
            {
                DispatchBehaviorEvent(BehaviorEventType.BeforeBegin);
                Begin();
                DispatchBehaviorEvent(BehaviorEventType.AfterBegin);

                // _wasBeginCalled = true;
            }

            // Check for transform/layout/surface rebuild
            if (InvalidationState.HasFlag(Invalidation.TransformDirty) || InvalidationState.HasFlag(Invalidation.LayoutDirty) || InvalidationState.HasFlag(Invalidation.SurfaceDirty))
            {
                ClearInvalidation(Invalidation.LayoutDirty);

                if (InvalidationState.HasFlag(Invalidation.TransformDirty))
                {
                    ClearInvalidation(Invalidation.TransformDirty);
                    DispatchBehaviorEvent(BehaviorEventType.BeforeLayout);

                    DispatchBehaviorEvent(BehaviorEventType.BeforeTransform);
                    Transform.UpdateTransform();
                    Children.ForEach(x => x.Invalidate(Invalidation.TransformDirty));
                    DispatchBehaviorEvent(BehaviorEventType.AfterTransform);

                    Shape.UpdateShape(); // Includes updating layout

                    DispatchBehaviorEvent(BehaviorEventType.AfterLayout);
                }

                if (InvalidationState.HasFlag(Invalidation.SurfaceDirty))
                {
                    ClearInvalidation(Invalidation.SurfaceDirty);
                    _objectSurface.InvalidateSurface(Shape.LocalBounds, Quality.CachedValue, Padding.CachedValue); // Make sure to use LocalBounds since those don't include padding
                }
            }

            CheckIfObjectMustBeDisabled();
            if (!GlobalEnabled) return;

            // Run own update behavior before children
            Update();

            // Recursively update all objects
            Composition.GetZOrderedListOfChildren(this).ForEach(x => x.OnUpdate());

            // Check for child surface rebuild; Important: this should only execute after the recursive children update. Otherwise issues will occur for obvious reasons
            if (InvalidationState.HasFlag(Invalidation.ChildDirty))
            {
                ClearInvalidation(Invalidation.ChildDirty);
                _childSurface.InvalidateSurface(Shape.LocalBounds, Quality.CachedValue, Padding.CachedValue); // Make sure to use LocalBounds since those don't include padding
            }

            DispatchBehaviorEvent(BehaviorEventType.AfterUpdate);
        }

        protected virtual void Update()
        {
            // Update behavior
        }

        public void OnLateUpdate()
        {
            DispatchBehaviorEvent(BehaviorEventType.BeforeLateUpdate);

            if (!_wasBeginCalled)
            {
                DispatchBehaviorEvent(BehaviorEventType.BeforeLateBegin);
                LateBegin();
                DispatchBehaviorEvent(BehaviorEventType.AfterLateBegin);

                _wasBeginCalled = true;
            }

            // Run own late update behavior before children
            LateUpdate();

            // Recursively late update all objects
            Composition.GetZOrderedListOfChildren(this).ForEach(x => x.OnLateUpdate());

            DispatchBehaviorEvent(BehaviorEventType.AfterLateUpdate);
        }

        protected virtual void LateUpdate()
        {
            // Late update behavior
        }

        public void OnEarlyUpdate()
        {
            DispatchBehaviorEvent(BehaviorEventType.BeforeEarlyUpdate);
            
            // Run own early update behavior before children
            EarlyUpdate();

            // Recursively early update all objects
            Composition.GetZOrderedListOfChildren(this).ForEach(x => x.OnEarlyUpdate());

            DispatchBehaviorEvent(BehaviorEventType.AfterEarlyUpdate);
        }

        protected virtual void EarlyUpdate()
        {
            // Update behavior
        }

        public void RenderToSurface(SKCanvas? canvas)
        {
            if (canvas == null) return;

            DispatchBehaviorEvent(BehaviorEventType.BeforeRender, canvas);

            Render(canvas);
            AfterRender(canvas);

            DispatchBehaviorEvent(BehaviorEventType.AfterRender, canvas);

            // Debug bounds
            if (FContext.GetCurrentWindow().DebugDisplayBounds)
            {
                using var debugPaint = new SKPaint() { IsStroke = true, StrokeWidth = 2, Color = SKColors.Green };
                canvas?.DrawRect(Shape.LocalBounds, debugPaint);
            }
        }

        public virtual void Render(SKCanvas canvas)
        {
            // The actual rendering is done here. It is cached, therefor does not get called every render tick
        }

        public virtual void AfterRender(SKCanvas canvas)
        {
            // Second render pass after the inital is done
        }

        public virtual void DrawChildren(SKCanvas? canvas)
        {
            DispatchBehaviorEvent(BehaviorEventType.BeforeDrawChildren, canvas);

            // Iterate through children for rendering.
            Composition.GetZOrderedListOfChildren(this).ForEach(x =>
            {
                DispatchBehaviorEvent(BehaviorEventType.BeforeDrawChild, canvas);
                x.DrawToSurface(canvas);
                DispatchBehaviorEvent(BehaviorEventType.AfterDrawChild, canvas);
            });

            DispatchBehaviorEvent(BehaviorEventType.AfterDrawChildren, canvas);
        }

        public bool RenderThisFrame()
        {
            if (!RMath.IsRectPartiallyInside(Parent?.Shape.GlobalBounds ?? FContext.GetCurrentWindow().Bounds, Shape.GlobalBounds)) return false;
            // if (!RMath.IsRectPartiallyInside(Shape.GlobalBounds, FContext.GetCurrentWindow().GetCurrentDirtyClipPath())) return false; // Technically smart, though it wouldn't work like that

            if (!GlobalEnabled) return false;
            if (!GlobalVisible) return false;

            return true;
        }

        public virtual void DrawToSurface(SKCanvas? canvas)
        {
            if (!RenderThisFrame()) return;
            if (canvas == null) return;

            int? save = canvas?.Save();
            DispatchBehaviorEvent(BehaviorEventType.BeforeSurfaceDraw, canvas);

            canvas?.Concat(Transform.DrawMatrix);

            using var paint = GetDrawPaint();

            _objectSurface.Draw();

            if (_objectSurface.GetImage() != null)
                canvas?.DrawImage(_objectSurface.GetImage(), Shape.SurfaceDrawRect, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear), paint);

            _childSurface.Draw();

            if (_childSurface.GetImage() != null)
                canvas?.DrawImage(_childSurface.GetImage(), Shape.SurfaceDrawRect, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear), paint);

            // Debug bounds
            if (FContext.GetCurrentWindow().DebugDisplayBounds)
            {
                using var debugPaint = new SKPaint() { IsStroke = true, StrokeWidth = 2, Color = SKColors.Red };
                canvas?.DrawRect(Shape.SurfaceDrawRect, debugPaint);
            }

            DispatchBehaviorEvent(BehaviorEventType.AfterSurfaceDraw, canvas);
            if (save != null) canvas?.RestoreToCount(save.Value);
        }

        public void DispatchBehaviorEvent(BehaviorEventType type, object? data = null)
        {
            foreach (var behavior in BehaviorComponents.ToList())
            {
                if (behavior.Enabled)
                {
                    behavior.HandleEvent(type, data);
                }
            }
        }

        public virtual void Dispose()
        {
            Transform.Dispose();
            Layout.Dispose();

            Enabled.Dispose();
            Visible.Dispose();
            Quality.Dispose();
            Padding.Dispose();
            RenderMaterial.Dispose();

            RemoveFromParent();
            OnObjectDisposed?.Invoke();
        }

        void CheckIfSurfaceCanBeDisposed()
        {
            if (!GlobalEnabled || !GlobalVisible)
            {
                _childSurface.DisposeSurface();
                _objectSurface.DisposeSurface();
                Invalidate(Invalidation.SurfaceDirty);
            }
        }

        public virtual void OnInternalStateChanged<T>(T value)
        {
            Invalidate(Invalidation.All); // Make sure to invalidate all when quality or padding is updated. This stuff can break easily if not updated
        }
    }
}