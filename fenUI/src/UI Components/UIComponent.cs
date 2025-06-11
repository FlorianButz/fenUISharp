using FenUISharp.Mathematics;
using FenUISharp.WinFeatures;
using SkiaSharp;

namespace FenUISharp.Components
{
    public abstract class UIComponent : IDisposable
    {
        public Window WindowRoot { get; set; }

        public Transform Transform { get; set; }
        public SKPaint SkPaint { get; set; }

        internal List<BehaviorComponent> Components { get; private set; } = new List<BehaviorComponent>();

        private bool _forceDisableInvalidation = false;
        public bool ForceDisableInvalidation { get => _forceDisableInvalidation || ((Transform.Parent != null) ? Transform.Parent.ParentComponent.ForceDisableInvalidation : false); set => _forceDisableInvalidation = value; }

        public bool Enabled { get; set; } = true;
        public bool Visible { get; set; } = true;
        public bool CareAboutInteractions { get; set; } = true;
        public bool CanInteractVisualIndicator { get; set; } = false;
        public bool PixelSnapping { get; set; } = true;

        public ImageEffect ImageEffect { get; init; }

        public SKPath? ChildClipPath { get; set; }

        public static UIComponent? CurrentlySelected { get; set; } = null;

        protected float GRenderQuality => RenderQuality.Value * ((Transform.Parent != null) ? Transform.Parent.ParentComponent.GRenderQuality : 1);
        public MultiAccess<float> RenderQuality = new MultiAccess<float>(1);
        protected SKImageInfo? cachedImageInfo = null;
        protected SKSurface? cachedSurface = null;

        public SKRect InteractionBounds
        {
            get
            {
                var interactionBounds = Transform.Bounds;
                interactionBounds.Inflate(Transform.InteractionPadding, Transform.InteractionPadding);
                return interactionBounds;
            }
        }

        protected bool _isRendering = false;
        protected bool _isMouseHovering { get; private set; } = false;
        private bool _isThisGloballyInvalidated;
        public bool SelfInvalidated => _isThisGloballyInvalidated;
        public bool GloballyInvalidated
        {
            get
            {
                if (!Enabled || !Visible) return false;
                if (SelfInvalidated) return true;
                if (Transform.Children.Any(x => x.ParentComponent.GloballyInvalidated && !x.ParentComponent.IsOutsideClip())) return true;
                return false;
            }
            set { _isThisGloballyInvalidated = value; }
        }

        private bool _isMarkedInvalid = false;

        public UIComponent(Window rootWindow, Vector2 position, Vector2 size)
        {
            if (rootWindow == null) throw new Exception("Root window cannot be null.");
            WindowRoot = rootWindow;

            Transform = new(this);
            Transform.LocalPosition = position;
            Transform.Size = size;

            SkPaint = CreatePaint();

            WindowFeatures.GlobalHooks.OnMouseMove += OnMouseMove;
            rootWindow.OnUpdate += Update;
            rootWindow.MouseAction += OnMouseAction;

            WindowRoot.WindowThemeManager.ThemeChanged += Invalidate;
            WindowRoot.AddUIComponent(this);

            ImageEffect = new(this);
        }

        private void OnMouseAction(MouseInputCode inputCode)
        {
            if (!Enabled || !CareAboutInteractions) return;

            if (RMath.ContainsPoint(InteractionBounds, WindowRoot.ClientMousePosition) && GetTopmostComponentAtPosition(WindowRoot.ClientMousePosition) == this)
            {
                switch (inputCode.button)
                {
                    case 0:
                        {
                            if (inputCode.state == 1)
                            {
                                var oldSelected = CurrentlySelected;
                                if (CurrentlySelected != this) CurrentlySelected?.SelectedLost();

                                CurrentlySelected = this;
                                CurrentlySelected?.Selected();

                                // if (oldSelected != this) oldSelected?.Invalidate();
                                // Invalidate();
                            }

                            break;
                        }
                }

                MouseAction(inputCode);
                Components.ForEach(x => x.MouseAction(inputCode));
            }

            GlobalMouseAction(inputCode);
            Components.ForEach(x => x.GlobalMouseAction(inputCode));
        }

        private void OnMouseMove(Vector2 pos)
        {
            if (!Enabled || !CareAboutInteractions) return;

            Vector2 mousePos = WindowRoot.GlobalPointToClient(pos);

            if (RMath.ContainsPoint(InteractionBounds, mousePos) && !_isMouseHovering && GetTopmostComponentAtPosition(mousePos) == this)
            {
                _isMouseHovering = true;
                MouseEnter();

                Components.ForEach(z => z.MouseEnter());
            }
            else if ((RMath.ContainsPoint(InteractionBounds, mousePos) && _isMouseHovering && GetTopmostComponentAtPosition(mousePos) != this)
                || !RMath.ContainsPoint(InteractionBounds, mousePos) && _isMouseHovering)
            {
                _isMouseHovering = false;
                MouseExit();

                Components.ForEach(z => z.MouseExit());
            }

            MouseMove(mousePos);
            Components.ForEach(z => z.MouseMove(mousePos));
        }

        protected SKPaint CreatePaint()
        {
            SkPaint = CreateSurfacePaint();
            return SkPaint;
        }

        protected virtual SKPaint CreateSurfacePaint()
        {
            return new SKPaint()
            {
                Color = SKColors.White,
                IsAntialias = true
            };
        }

        public bool IsOutsideClip()
        {
            return Transform.ClipWhenFullyOutsideParent && (Transform.Parent != null && (!RMath.IsRectPartiallyInside(Transform.Parent.Bounds, Transform.Bounds))) ||
                (!RMath.IsRectPartiallyInside(SKRect.Create(0, 0, WindowRoot.WindowSize.x, WindowRoot.WindowSize.y), Transform.Bounds));
        }

        public void DrawToScreen(SKCanvas canvas)
        {
            if (!Visible || !Enabled) return;
            if (IsOutsideClip()) return;
            _isRendering = true;

            // Render quality
            float quality = RMath.Clamp(GRenderQuality, 0.05f, 2);
            var bounds = Transform.FullBounds;

            int c = canvas.Save();
            canvas.RotateDegrees(Transform.Rotation, Transform.Position.x + bounds.Width * Transform.Anchor.x, Transform.Position.y + bounds.Height * Transform.Anchor.y);
            canvas.Scale(Transform.Scale.x, Transform.Scale.y, Transform.Position.x + bounds.Width * Transform.Anchor.x, Transform.Position.y + bounds.Height * Transform.Anchor.y);

            // Applying custom transform
            if (Transform.Matrix != null)
                canvas.Concat(Transform.Matrix.Value);

            int scaledWidth = RMath.Clamp((int)(bounds.Width * quality), 1, int.MaxValue);
            int scaledHeight = RMath.Clamp((int)(bounds.Height * quality), 1, int.MaxValue);

            if (cachedSurface == null || cachedImageInfo == null)
            {
                cachedSurface?.Dispose(); // Dispose of old surface before creating a new one

                if (cachedImageInfo == null || cachedImageInfo?.Width != scaledWidth || cachedImageInfo?.Height != scaledHeight)
                    cachedImageInfo = new SKImageInfo(scaledWidth, scaledHeight);

                // Create an offscreen surface for this component
                cachedSurface = WindowRoot.RenderContext.CreateAdditional(cachedImageInfo.Value);

                if (cachedSurface != null)
                {
                    cachedSurface.Canvas.Scale(quality, quality);
                    Components.ForEach(x => x.OnBeforeRender(cachedSurface.Canvas));

                    int layerRestoreCount = 0;
                    using (var effectPaint = ImageEffect.ApplyInsideCacheImageEffect(new SKPaint() { Color = SKColors.White }))
                    {
                        layerRestoreCount = cachedSurface.Canvas.SaveLayer(effectPaint);
                        DrawToSurface(cachedSurface.Canvas);
                    }
                    cachedSurface.Canvas.RestoreToCount(layerRestoreCount);

                    Components.ForEach(x => x.OnAfterRender(cachedSurface.Canvas));

                    // DrawSelectionEffect(cachedSurface.Canvas);

                    cachedSurface.Flush();
                    cachedSurface.Context?.Dispose();
                    cachedSurface.SurfaceProperties?.Dispose();
                    cachedSurface.Canvas.Dispose();
                }
            }

            int opacityLayerRestore = 0;

            if (cachedSurface != null)
            {
                // Draw the cached surface onto the main canvas
                using (var snapshot = cachedSurface.Snapshot())
                {
                    canvas.Scale(1 / quality, 1 / quality); // Scale for proper rendering

                    if (Transform.Matrix == null)
                    {
                        if (PixelSnapping)
                            canvas.Translate((float)Math.Round(Transform.Position.x * quality), (float)Math.Round(Transform.Position.y * quality));
                        else
                            canvas.Translate(Transform.Position.x * quality, Transform.Position.y * quality);
                    }

                    Components.ForEach(x => x.OnBeforeRenderCache(cachedSurface.Canvas));

                    using (var effectPaint = ImageEffect.ApplyImageEffect(new SKPaint() { Color = SKColors.White, IsAntialias = true }))
                    {
                        // Applying a faster opacity effect. Avoid using color matrix to speed up render time
                        if (ImageEffect.Opacity != 1)
                            opacityLayerRestore = canvas.SaveLayer(new() { Color = new(255, 255, 255, (byte)(Math.Clamp(ImageEffect.Opacity * 255, 0, 255))) });

                        try
                        {
                            // Drawing the cached image
                            canvas.DrawImage(snapshot, 0, 0, WindowRoot.RenderContext.SamplingOptions, effectPaint);
                        }
                        catch (Exception e) { }

                        if (ImageEffect.Opacity != 1)
                            canvas.RestoreToCount(opacityLayerRestore);
                    }

                    Components.ForEach(x => x.OnAfterRenderCache(cachedSurface.Canvas));

                    // Always move back to 0;0. Translate always happen, no matter if rotation matrix is set or not.
                    if (PixelSnapping)
                        canvas.Translate(-(float)Math.Round(Transform.Position.x * quality), -(float)Math.Round(Transform.Position.y * quality));
                    else
                        canvas.Translate(-Transform.Position.x * quality, -Transform.Position.y * quality);

                    canvas.Scale(quality, quality); // Scale back for proper rendering

                    snapshot.Dispose();
                }
            }

            Components.ForEach(x => x.OnBeforeRenderChildren(canvas));
            Transform.OrderTransforms(Transform.Children).ForEach(c =>
            {
                int save = canvas.Save();
                if (c.IgnoreCustomParentMatrix) canvas.ResetMatrix();

                if (ChildClipPath != null) canvas.ClipPath(ChildClipPath, antialias: true);
                c.ParentComponent.DrawToScreen(canvas);
                canvas.RestoreToCount(save);
            });
            Components.ForEach(x => x.OnAfterRenderChildren(canvas));

            // if (ImageEffect.ThisOpacity != 1)
            //     canvas.RestoreToCount(opacityLayerRestore);

            canvas.RestoreToCount(c);
            _isRendering = false;
        }

        public void DrawSelectionEffect(SKCanvas canvas)
        {
            if (UIComponent.CurrentlySelected == this)
            {
                using (var paint = new SKPaint() { IsAntialias = true, Color = WindowRoot.WindowThemeManager.GetColor(t => t.OnBackground).Value, IsStroke = true, StrokeWidth = 0.5f })
                {
                    float[] intervals = { 2, 6 };
                    paint.PathEffect = SKPathEffect.CreateDash(intervals, 0);

                    paint.IsStroke = true;
                    paint.StrokeCap = SKStrokeCap.Round;
                    paint.StrokeJoin = SKStrokeJoin.Round;

                    var rect = SKRect.Create((float)Math.Round(Transform.LocalBounds.Left) + 0.5f, (float)Math.Round(Transform.LocalBounds.Top) + 0.5f, Transform.LocalBounds.Width, Transform.LocalBounds.Height);
                    rect.Inflate(3f, 3f);
                    canvas.DrawPath(SKSquircle.CreateSquircle(rect, 10), paint);
                }
            }
        }

        public void Invalidate()
        {
            if (ForceDisableInvalidation) { GloballyInvalidated = true; return; }
            if (_isRendering) { MarkInvalidated(); return; }

            cachedSurface?.Dispose();
            cachedSurface = null; // Mark for redraw

            cachedImageInfo = null;
            GloballyInvalidated = true;
        }

        public void SoftInvalidate()
        {
            GloballyInvalidated = true;
        }

        public void MarkInvalidated()
        {
            _isMarkedInvalid = true;
            GloballyInvalidated = true;
        }

        public void RecursiveInvalidate()
        {
            Invalidate();
            Transform.Children.ForEach(x => x.ParentComponent.RecursiveInvalidate());
        }

        protected abstract void DrawToSurface(SKCanvas canvas);

        private void Update()
        {
            if (Enabled)
            {
                if (_isMarkedInvalid)
                {
                    _isMarkedInvalid = false;
                    Invalidate();
                }

                OnUpdate();
                Components.ToList().ForEach(x => x.CmpUpdate());
            }

            if (CanInteractVisualIndicator)
            {
                ImageEffect.Opacity = CareAboutInteractions ? 1 : 0.5f;
                ImageEffect.Saturation =  CareAboutInteractions ? 1 : 0.75f;
            }
        }

        protected virtual void OnUpdate() { }
        protected virtual void ComponentDestroy() { }

        protected virtual void Selected() { }
        protected virtual void SelectedLost() { }

        protected virtual void MouseEnter() { }
        protected virtual void MouseExit() { }
        protected virtual void MouseAction(MouseInputCode inputCode) { }
        protected virtual void GlobalMouseAction(MouseInputCode inputCode) { }
        protected virtual void MouseMove(Vector2 pos) { }

        public void Dispose()
        {
            ComponentDestroy();

            RenderQuality.onValueUpdated -= OnRenderQualityUpdated;
            WindowFeatures.GlobalHooks.OnMouseMove -= OnMouseMove;
            WindowRoot.OnUpdate -= Update;
            WindowRoot.MouseAction -= OnMouseAction;
            WindowRoot.WindowThemeManager.ThemeChanged -= Invalidate;

            if (CurrentlySelected == this) CurrentlySelected = null;
            if (WindowRoot.GetUIComponents().Contains(this)) WindowRoot.RemoveUIComponent(this);

            new List<BehaviorComponent>(Components).ForEach(x => x.Dispose());
            new List<Transform>(Transform.Children).ForEach(x => x.ParentComponent.Dispose());

            Transform.Dispose();
        }

        public UIComponent? GetTopmostComponentAtPosition(Vector2 pos)
        {
            var searchList = WindowRoot.OrderUIComponents(WindowRoot.GetUIComponents()).ToList();

            return searchList
                .Where(x => x.Enabled && x.CareAboutInteractions && RMath.ContainsPoint(x.InteractionBounds, pos))
                .LastOrDefault();
        }

        public UIComponent? GetTopmostComponentAtPositionWithComponent<T>(Vector2 pos)
        {
            var searchList = WindowRoot.OrderUIComponents(WindowRoot.GetUIComponents()).ToList();

            return searchList
                .Where(x => x.Enabled && x.CareAboutInteractions && RMath.ContainsPoint(x.InteractionBounds, pos) && x.Components.Any(x => x is T && x.Enabled))
                .LastOrDefault();
        }

        public void SetColor(SKColor color)
        {
            SkPaint.Color = color;
            Invalidate();
        }

        public void OnRenderQualityUpdated(float v)
        {
            Invalidate();
        }
    }
}