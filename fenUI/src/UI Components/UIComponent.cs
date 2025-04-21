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

        internal List<Component> Components { get; private set; } = new List<Component>();

        public bool Enabled { get; set; } = true;
        public bool Visible { get; set; } = true;
        public bool CareAboutInteractions { get; set; } = true;

        public ImageEffect ImageEffect { get; init; }

        public static UIComponent? CurrentlySelected { get; set; } = null;

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

        protected bool _isMouseHovering { get; private set; } = false;
        private bool _isThisGloballyInvalidated;
        public bool SelfInvalidated => _isThisGloballyInvalidated;
        public bool GloballyInvalidated
        {
            get
            {
                if (!Enabled || !Visible) return false;
                if (SelfInvalidated) return true;
                if (Transform.Children.Any(x => x.ParentComponent._isThisGloballyInvalidated)) return true;
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

            CreatePaint();

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

                                if (oldSelected != this) oldSelected?.Invalidate();
                                Invalidate();
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

        protected void CreatePaint()
        {
            SkPaint = CreateSurfacePaint();
        }

        protected virtual SKPaint CreateSurfacePaint()
        {
            return new SKPaint()
            {
                Color = SKColors.White,
                IsAntialias = true
            };
        }

        public void DrawToScreen(SKCanvas canvas)
        {
            if (!Visible || !Enabled) return;
            if (Transform.Parent != null && Transform.ClipWhenFullyOutsideParent && !RMath.IsRectPartiallyInside(Transform.Parent.Bounds, Transform.Bounds)) return;

            // Render quality
            float quality = RMath.Clamp(RenderQuality.Value * ((Transform.Parent != null) ? Transform.Parent.ParentComponent.RenderQuality.Value : 1), 0.05f, 2);
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

            if (cachedSurface != null)
            {
                // Draw the cached surface onto the main canvas
                using (var snapshot = cachedSurface.Snapshot())
                {
                    canvas.Scale(1 / quality, 1 / quality); // Scale for proper rendering

                    if (Transform.Matrix == null)
                        canvas.Translate(Transform.Position.x * quality, Transform.Position.y * quality);

                    Components.ForEach(x => x.OnBeforeRenderCache(cachedSurface.Canvas));

                    using (var effectPaint = ImageEffect.ApplyImageEffect(new SKPaint() { Color = SKColors.White, IsAntialias = true }))
                    {
                        canvas.DrawImage(snapshot, 0, 0, WindowRoot.RenderContext.SamplingOptions, effectPaint);
                    }

                    Components.ForEach(x => x.OnAfterRenderCache(cachedSurface.Canvas));

                    canvas.Translate(-(Transform.Position.x * quality), -(Transform.Position.y * quality)); // Always move back to 0;0. Translate always happen, no matter if rotation matrix is set or not.
                    canvas.Scale(quality, quality); // Scale back for proper rendering

                    snapshot.Dispose();
                }
            }

            Components.ForEach(x => x.OnBeforeRenderChildren(canvas));
            Transform.OrderTransforms(Transform.Children).ForEach(c => c.ParentComponent.DrawToScreen(canvas));
            Components.ForEach(x => x.OnAfterRenderChildren(canvas));

            canvas.RestoreToCount(c);
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
                Components.ForEach(x => x.CmpUpdate());
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

            new List<Component>(Components).ForEach(x => x.Dispose());

            Transform.Dispose();
        }

        public UIComponent? GetTopmostComponentAtPosition(Vector2 pos)
        {
            var searchList = WindowRoot.OrderUIComponents(WindowRoot.GetUIComponents());
            if (!searchList.Any(x => x.Enabled && x.CareAboutInteractions && RMath.ContainsPoint(x.InteractionBounds, pos))) return null;
            return searchList.Last(x => x.Enabled && x.CareAboutInteractions && RMath.ContainsPoint(x.InteractionBounds, pos));
        }

        public UIComponent? GetTopmostComponentAtPositionWithComponent<T>(Vector2 pos)
        {
            var searchList = WindowRoot.OrderUIComponents(WindowRoot.GetUIComponents());
            if (!searchList.Any(x => x.Enabled && x.CareAboutInteractions && RMath.ContainsPoint(x.InteractionBounds, pos) && x.Components.Any(x => x is T))) return null;
            return searchList.Last(x => x.Enabled && x.CareAboutInteractions && RMath.ContainsPoint(x.InteractionBounds, pos) && x.Components.Any(x => x is T));
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