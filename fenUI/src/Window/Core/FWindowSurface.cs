using FenUISharp.Mathematics;
using FenUISharp.Objects;
using SkiaSharp;

namespace FenUISharp
{
    public class FWindowSurface : IDisposable
    {
        private WeakReference<FWindow> window { get; set; }
        public FWindow Window { get => window.TryGetTarget(out var target) ? target : throw new Exception("Window not set."); }

        public Func<SKColor> ClearColor { get; set; }

        internal FenUISharp.Objects.ModelViewPane? RootViewPane { get; private set; }

        private readonly object _renderLock = new object();

        public FWindowSurface(FWindow window)
        {
            this.window = new WeakReference<FWindow>(window);

            ClearColor = () => Window.Properties.UseSystemDarkMode ?
                (!Window.Properties._isFocused ? SKColor.Parse("#202020") : SKColor.Parse("#211f23")) :
                SKColor.Parse("#f3f3f3");
        }

        public virtual void SetupSurface()
        {
            // Create root view pane
            RootViewPane = new(null);
            RootViewPane.Layout.Alignment.Value = () => new(0.5f, 0.5f);
            RootViewPane.Layout.StretchHorizontal.Value = () => true;
            RootViewPane.Layout.StretchVertical.Value = () => true;

            RootViewPane.DisableWhenOutOfParentBounds = false;

            // Make root pane default
            FContext.WithRootViewPane(RootViewPane);
        }

        public void Dispose()
        {
            RootViewPane?.Dispose();
            RootViewPane = null!;
        }

        public void Draw(SKCanvas canvas)
        {
            // if (Window.Procedure._isResizing) return;

            // Thread safety, lock render object
            lock (_renderLock)
            {
                // Saving the initial canvas state
                int notClipped = canvas.Save();

                // Calculate and get clip path
                using var clipPath = CalculateThisFramesDirtyClipPath();
                canvas.ClipPath(clipPath);

                // Clear surface and draw backdrop
                Window.ClearSurface(canvas);
                Window.DrawBackdrop(canvas);

                // Trigger callback
                if (Window.SkiaDirectCompositionContext?.Surface != null)
                    Window.Callbacks.OnWindowBeforeDraw?.Invoke(Window.SkiaDirectCompositionContext.Surface);

                // Draw all ui objects
                RootViewPane?.DrawToSurface(canvas);

                // If bounds debug flag is set
                if (Window.DebugDisplayBounds)
                {
                    // Loop over all objects
                    foreach (var component in GetAllUIObjects())
                    {
                        // Check if component is being rendered this frame
                        if (component.RenderThisFrame())
                        {
                            using var debugPaint = new SKPaint() { IsStroke = true, StrokeWidth = 1f, Color = SKColors.Blue };

                            // Get global bounds
                            var bounds = component.Shape.GlobalBounds;
                            // Extend
                            bounds.Inflate(2, 2);
                            // Draw normal bounds blue
                            canvas.DrawRect(bounds, debugPaint);
                            // Draw anchor point with circle
                            canvas.DrawCircle(
                                component.Transform.LocalToGlobal(component.Transform.Anchor.CachedValue).x,
                                component.Transform.LocalToGlobal(component.Transform.Anchor.CachedValue).y,
                                2, debugPaint);

                            // Change color to yellow
                            debugPaint.Color = SKColors.Yellow;

                            // Get interactive bounds
                            var boundsInteractive = component.InteractiveSurface.GlobalSurface.CachedValue;
                            // Extend
                            boundsInteractive.Inflate(1, 1);
                            // Draw interactive bounds yellow
                            canvas.DrawRect(bounds, debugPaint);
                            // Draw alignment anchor as circle
                            canvas.DrawCircle(
                                component.Transform.LocalToGlobal(component.Layout.AlignmentAnchor.CachedValue).x,
                                component.Transform.LocalToGlobal(component.Layout.AlignmentAnchor.CachedValue).y,
                                1, debugPaint);
                        }
                    }
                }

                if (Window.DebugDisplayObjectIDs)
                {
                    // Loop over all objects
                    foreach (var component in GetAllUIObjects())
                    {
                        // Check if component is being rendered this frame
                        if (component.RenderThisFrame())
                        {
                            using var debugPaint = new SKPaint() { Color = SKColors.Magenta };

                            canvas.DrawText("ID: " + component.InstanceID,
                                new SKPoint(component.Shape.GlobalBounds.Left, component.Shape.GlobalBounds.Top),
                                SKTextAlign.Left, new SKFont(SKTypeface.Default, 10),
                                debugPaint);
                        }
                    }
                }

                // Restore canvas to initial no-clipping state
                canvas.RestoreToCount(notClipped);

                // Check for display area cache flag
                if (Window.DebugDisplayAreaCache)
                {
                    // Draw bounds area red
                    using var paint = new SKPaint() { Color = SKColors.Red.WithAlpha(1) };
                    canvas.DrawRect(SKRect.Create(0, 0, Window.Shape.ClientSize.x, Window.Shape.ClientSize.y), paint);

                    paint.Color = SKColors.Yellow.WithAlpha(25);
                    paint.IsStroke = true;
                    paint.StrokeWidth = 2;
                    foreach (var area in Window.Shape.GetWinRegion())
                    {
                        var a = area.Invoke();
                        a.Inflate(-2, -2);
                        canvas.DrawRect(a, paint);
                    }
                }

                // Trigger callback
                if (Window.SkiaDirectCompositionContext?.Surface != null)
                    Window.Callbacks.OnWindowAfterDraw?.Invoke(Window.SkiaDirectCompositionContext.Surface);
            }
        }

        public bool IsNextFrameRendering()
        {
            // Don't render if resizing; Edit: Actually, better if utilizing immediate updates
            // if (Window.Procedure._isSizeMoving) return false;

            // If no root view pane exist, skip rendering
            if (RootViewPane == null) return false;

            // Check if any active and visible object wants to be redrawn
            var doesAnyObjectRequireRedraw = GetAllUIObjects().Any(x => x.WindowRedrawThisObject && x.Enabled.CachedValue && x.Visible.CachedValue);

            // Debug
            // GetAllUIObjects().Where(x => x.WindowRedrawThisObject && x.Enabled.CachedValue && x.Visible.CachedValue).ToList().ForEach(x => Console.WriteLine(x));
            // Console.WriteLine("+===+");
            // Console.WriteLine(Window._isDirty);
            // Console.WriteLine(Window._fullRedraw);
            // Console.WriteLine("+===+");

            // Check for other flags and return true if any of them is true
            return doesAnyObjectRequireRedraw || Window.DebugDisplayAreaCache || Window._isDirty || Window._fullRedraw;
        }

        internal List<UIObject> GetAllUIObjects()
        {
            List<UIObject> list = new();

            // If RootViewPane is null, return empty list
            if (RootViewPane == null) return list;
            // Else return zordered list of all children
            else list = RootViewPane.Composition.GetZOrderedListOfEverything();

            return list;
        }

        // private void RecursiveAddChildrenToList(UIObject parent, List<UIObject> list)
        // {
        //     if(!list.Contains(parent)) list.Add(parent);
        //     parent.Children.ToList().ForEach(x =>
        //     {
        //         if(!list.Contains(parent)) list.Add(x);
        //         RecursiveAddChildrenToList(x, list);
        //     });
        // }

        public SKPath GetCurrentDirtyClipPath()
        {
            // Return a copy to avoid external modifications
            return _cachedDirtyPath != null ? new SKPath(_cachedDirtyPath) : new SKPath();
        }

        private SKPath? _cachedDirtyPath;
        private SKPath? _lastDirtyPath;
        private SKPath CalculateThisFramesDirtyClipPath()
        {
            // Clear the old cached path
            _cachedDirtyPath?.Dispose();

            // Create new clip path
            var clipPath = new SKPath();

            // If the window is dirty, add everything to clip path
            if (Window._isDirty)
            {
                clipPath.AddRect(SKRect.Create(0, 0, Window.Shape.ClientSize.x, Window.Shape.ClientSize.y));
                _cachedDirtyPath = clipPath;
                return new SKPath(clipPath); // Return copy
            }

            // Get all UIObjects
            foreach (var component in GetAllUIObjects())
            {
                // Check if wants to redraw, is active and visible
                if (component.WindowRedrawThisObject && component.GlobalEnabled && component.GlobalVisible)
                {
                    // Define a small padding
                    int pad = 4;

                    // Get object's bounds
                    var bounds = component.Shape.GlobalBounds;

                    // Adding the padding
                    bounds.Inflate(pad, pad);

                    // Add to clip path
                    clipPath.AddRect(bounds);

                    // Getting the UIObject's bounds of last frame
                    // This is useful for fast moving elements,
                    // so their last position get's cleared as well
                    var lastbounds = component.Shape.LastGlobalBounds;

                    // Also add padding
                    lastbounds.Inflate(pad, pad);

                    // Add to clip path
                    clipPath.AddRect(lastbounds);
                }

                // Reset redraw flag
                component.WindowRedrawThisObject = false;
            }

            // Setting last path to null
            SKPath lastPath = null!;

            // Setting last path to dirty clip path of last frame
            if (_lastDirtyPath != null) lastPath = new SKPath(_lastDirtyPath);

            // Disposing the path of last frame
            _lastDirtyPath?.Dispose();

            // Setting last path to the current one
            _lastDirtyPath = new SKPath(clipPath);

            // Checking if last path is not null,
            // if that is the case, add to current dirty clip path as well
            if (lastPath != null)
                clipPath.AddPath(lastPath, SKPathAddMode.Append);

            // Dispose old cache
            if (_cachedDirtyPath != null) _cachedDirtyPath.Dispose();

            // Cache current path
            _cachedDirtyPath = new SKPath(clipPath);

            return clipPath;
        }

        internal bool MouseHitTest(Vector2 vector2)
        {
            var list = RootViewPane?.Composition.GetZOrderedListOfChildren(RootViewPane);
            if (list == null) return false;

            foreach (var x in list)
                if (x.Shape.GlobalBounds.Contains(new SKPoint(vector2.x, vector2.y))) return true;

            return false;
        }
    }
}