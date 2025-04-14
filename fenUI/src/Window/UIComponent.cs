using SkiaSharp;

namespace FenUISharp
{
    public abstract class UIComponent : IDisposable
    {
        public Window WindowRoot { get; set; }

        public Transform Transform { get; set; }
        public SKPaint SkPaint { get; set; }

        public List<Component> Components { get; set; } = new List<Component>();

        public bool Enabled { get; set; } = true;
        public bool Visible { get; set; } = true;
        public bool CareAboutInteractions { get; set; } = true;

        public ImageEffect ImageEffect { get; init; }

        public static UIComponent? CurrentlySelected { get; set; } = null;

        public MultiAccess<float> renderQuality = new MultiAccess<float>(1);
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
        public bool GloballyInvalidated
        {
            get
            {
                if (!Enabled || !Visible) return false;
                if (_isThisGloballyInvalidated) return true;
                if (Transform.Children.Any(x => x.ParentComponent._isThisGloballyInvalidated)) return true;
                return false;
            }
            set { _isThisGloballyInvalidated = value; }
        }

        public UIComponent(Window rootWindow, Vector2 position, Vector2 size)
        {
            if (rootWindow == null) throw new Exception("Root window cannot be null.");
            WindowRoot = rootWindow;

            Transform = new(this);
            Transform.LocalPosition = position;
            Transform.Size = size;

            CreatePaint();

            WindowFeatures.GlobalHooks.onMouseMove += OnMouseMove;
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
                                if (CurrentlySelected != this) CurrentlySelected?.SelectedLost();

                                CurrentlySelected = this;
                                CurrentlySelected?.Selected();
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
            float quality = RMath.Clamp(renderQuality.Value * ((Transform.Parent != null) ? Transform.Parent.ParentComponent.renderQuality.Value : 1), 0.05f, 2);
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

        protected abstract void DrawToSurface(SKCanvas canvas);

        private void Update()
        {
            if (Enabled)
            {
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

            renderQuality.onValueUpdated -= OnRenderQualityUpdated;
            WindowFeatures.GlobalHooks.onMouseMove -= OnMouseMove;
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

    public class ImageEffect
    {
        public UIComponent Parent { get; init; }

        public bool InheritValues { get; set; } = true;

        // Effects which are only applied to the cached version

        private float _blurRadius = 0f;
        public float BlurRadius
        {
            get => (Parent.Transform.Parent != null && InheritValues) ? Math.Max(_blurRadius, Parent.Transform.Parent.ParentComponent.ImageEffect.BlurRadius) : _blurRadius;
            set { _blurRadius = value; OnRequireInvalidation(); }
        }

        // Effects which are applied every frame

        private float _opacity = 1f;
        public float Opacity
        {
            get => (Parent.Transform.Parent != null && InheritValues) ? _opacity * Parent.Transform.Parent.ParentComponent.ImageEffect.Opacity : _opacity;
            set { _opacity = value; }
        }

        private float _brightness = 1f;
        public float Brightness
        {
            get => (Parent.Transform.Parent != null && InheritValues) ? _brightness * Parent.Transform.Parent.ParentComponent.ImageEffect.Brightness : _brightness;
            set { _brightness = value; }
        }

        private float _contrast = 1f;
        public float Contrast
        {
            get => (Parent.Transform.Parent != null && InheritValues) ? _contrast * Parent.Transform.Parent.ParentComponent.ImageEffect.Contrast : _contrast;
            set { _contrast = value; }
        }

        private float _saturation = 1f;
        public float Saturation
        {
            get => (Parent.Transform.Parent != null && InheritValues) ? _saturation * Parent.Transform.Parent.ParentComponent.ImageEffect.Saturation : _saturation;
            set { _saturation = value; }
        }

        private SKColor _tint = SKColors.White;
        public SKColor Tint
        {
            get => (Parent.Transform.Parent != null && InheritValues) ? RMath.MulMix(_tint, Parent.Transform.Parent.ParentComponent.ImageEffect.Tint) : _tint;
            set { _tint = value; }
        }

        private SKColor _add = SKColors.Black;
        public SKColor Add
        {
            get => (Parent.Transform.Parent != null && InheritValues) ? RMath.MulMix(_add, Parent.Transform.Parent.ParentComponent.ImageEffect.Add) : _add;
            set { _add = value; }
        }

        public ImageEffect(UIComponent parent)
        {
            Parent = parent;
        }

        public void OnRequireInvalidation()
        {
            Parent.Invalidate();
        }

        public SKPaint ApplyImageEffect(in SKPaint paint)
        {
            SKImageFilter? finalImageFilter = null;
            SKColorFilter? finalColorFilter = null;

            if (Opacity != 1)
            {
                var opacityColor = OpacityColor(Opacity);
                finalColorFilter = ComposeColorFilter(finalColorFilter, opacityColor);
            }

            if (Brightness != 1)
            {
                var lightnessColor = BrightnessColor(Brightness);
                finalColorFilter = ComposeColorFilter(finalColorFilter, lightnessColor);
            }

            if (Contrast != 1)
            {
                var contrastColor = ContrastFilter(Contrast);
                finalColorFilter = ComposeColorFilter(finalColorFilter, contrastColor);
            }

            if (Saturation != 1)
            {
                var saturationColor = SaturationFilter(Saturation);
                finalColorFilter = ComposeColorFilter(finalColorFilter, saturationColor);
            }

            if (Tint != SKColors.White || Add != SKColors.Black)
            {
                var tintAddColor = TintAddColor(Tint, Add);
                finalColorFilter = ComposeColorFilter(finalColorFilter, tintAddColor);
            }

            if (finalImageFilter != null)
                paint.ImageFilter = finalImageFilter;
            if (finalColorFilter != null)
                paint.ColorFilter = finalColorFilter;

            return paint;
        }

        public SKPaint ApplyInsideCacheImageEffect(in SKPaint paint)
        {
            SKImageFilter? finalImageFilter = null;
            SKColorFilter? finalColorFilter = null;

            if (BlurRadius > 0)
                using (var blur = SKImageFilter.CreateBlur(BlurRadius, BlurRadius))
                    finalImageFilter = ComposeImageFilter(finalImageFilter, blur);

            if (finalImageFilter != null)
                paint.ImageFilter = finalImageFilter;
            if (finalColorFilter != null)
                paint.ColorFilter = finalColorFilter;

            return paint;
        }

        SKImageFilter ComposeImageFilter(SKImageFilter? inner, SKImageFilter outer)
        {
            if (inner == null) return outer;
            else return SKImageFilter.CreateCompose(inner, outer);
        }

        SKColorFilter? ComposeColorFilter(SKColorFilter? inner, SKColorFilter? outer)
        {
            if (inner == null && outer != null) return outer;
            else if (outer == null && inner != null) return inner;
            else if (outer == null && inner == null) return null;
            else return SKColorFilter.CreateCompose(inner, outer);
        }

        static SKColorFilter OpacityColor(float value)
        {
            return SKColorFilter.CreateColorMatrix(new float[]
            {
                1, 0, 0, 0, 0,
                0, 1, 0, 0, 0,
                0, 0, 1, 0, 0,
                0, 0, 0, value, 0
            });
        }

        static SKColorFilter ContrastFilter(float value)
        {
            float avgLuminance = (1 - value) * 0.5f;

            return SKColorFilter.CreateColorMatrix(new float[]
            {
                value, 0, 0, 0, avgLuminance,
                0, value, 0, 0, avgLuminance,
                0, 0, value, 0, avgLuminance,
                0, 0, 0, 1, 0
            });
        }

        static SKColorFilter BrightnessColor(float brightness)
        {
            brightness = Math.Clamp(brightness, 0f, 2f);

            return SKColorFilter.CreateColorMatrix(new float[]
            {
                brightness, 0, 0, 0, 0,
                0, brightness, 0, 0, 0,
                0, 0, brightness, 0, 0,
                0, 0, 0, 1, 0
            });
        }

        static SKColorFilter TintAddColor(SKColor tint, SKColor add)
        {
            return SKColorFilter.CreateColorMatrix(new float[]
            {
                (float)tint.Red / 255, 0, 0, 0, (float)add.Red,
                0, (float)tint.Blue / 255, 0, 0, (float)add.Green,
                0, 0, (float)tint.Green / 255, 0, (float)add.Blue,
                0, 0, 0, 1, 0
            });
        }

        static SKColorFilter SaturationFilter(float saturation)
        {
            float lumR = 0.2126f;
            float lumG = 0.7152f;
            float lumB = 0.0722f;

            return SKColorFilter.CreateColorMatrix(new float[]
            {
                lumR + (1 - lumR) * saturation, lumG - lumG * saturation,     lumB - lumB * saturation,     0, 0,
                lumR - lumR * saturation,     lumG + (1 - lumG) * saturation, lumB - lumB * saturation,     0, 0,
                lumR - lumR * saturation,     lumG - lumG * saturation,     lumB + (1 - lumB) * saturation, 0, 0,
                0, 0, 0, 1, 0
            });
        }
    }

    public class Transform : IDisposable
    {
        public UIComponent ParentComponent { get; private set; }

        public Transform? Root { get; private set; }
        public Transform? Parent { get; private set; }
        public List<Transform> Children { get; private set; } = new List<Transform>();

        public int ZIndex { get; set; } = 0;
        public int CreationIndex { get; init; } = 0; // Stores the actual order
        private static int _lastCreationIndex = 0;

        public SKMatrix? Matrix { get; set; }

        public Vector2 Position { get { var pos = _localPosition; if (Parent != null && !IgnoreParentOffset) pos += Parent.ChildOffset; return GetGlobalPosition(pos); } }
        public Vector2 LocalPosition { get => _localPosition + BoundsPadding.Value; set => _localPosition = value; }
        public Vector2 LocalPositionExcludeBounds { get => _localPosition; set => _localPosition = value; }
        private Vector2 _localPosition { get; set; }
        public Vector2 ChildOffset { get => _childOffset; set => _childOffset = value; }
        private Vector2 _childOffset { get; set; }
        public Vector2 Anchor { get; set; } = new Vector2(0.5f, 0.5f);
        private Vector2 _size { get; set; }
        public Vector2 Size { get => GetSize(); set => _size = value; }

        public Vector2 Scale { get; set; } = new Vector2(1, 1);
        public float Rotation { get; set; } = 0;

        public float InteractionPadding { get; set; } = 0;

        public float MarginHorizontal { get; set; } = 15;
        public float MarginVertical { get; set; } = 15;
        public bool StretchHorizontal { get; set; } = false;
        public bool StretchVertical { get; set; } = false;

        public bool ParentIgnoreLayout { get; set; } = false;
        public bool IgnoreParentOffset { get; set; } = false;

        public bool ClipWhenFullyOutsideParent { get; set; } = true;

        public SKRect FullBounds { get => GetBounds(0); }
        public SKRect Bounds { get => GetBounds(1); }
        public SKRect LocalBounds { get => GetBounds(2); }

        public MultiAccess<int> BoundsPadding = new MultiAccess<int>(0);

        public Vector2 Alignment { get; set; } = new Vector2(0.5f, 0.5f); // Place object in the middle of parent

        public Transform(UIComponent component)
        {
            ParentComponent = component;
            
            CreationIndex = _lastCreationIndex;
            _lastCreationIndex++;
        }

        public List<Transform> OrderTransforms(List<Transform> transforms)
        {
            return transforms.AsEnumerable().OrderBy(e => e.ZIndex).ThenBy(e => e.CreationIndex).ToList();
        }

        public Vector2 GetSize()
        {
            var sp = ParentComponent.WindowRoot.Bounds;
            var ss = Parent?.Size;

            var s = _size;

            var x = (Parent == null) ? sp.Width : ss.Value.x;
            var y = (Parent == null) ? sp.Height : ss.Value.y;

            if (StretchHorizontal) s.x = x - MarginHorizontal * 2;
            if (StretchVertical) s.y = y - MarginVertical * 2;

            return s;
        }

        public void SetParent(Transform transform)
        {
            Parent = transform;

            if (Parent.Parent == null)
                Root = Parent;
            else
                Root = Parent.Root;

            Parent.AddChild(this);
            Parent.ParentComponent.renderQuality.onValueUpdated += ParentComponent.OnRenderQualityUpdated;

            // UpdateLayout();
        }

        public void ClearParent()
        {
            if (Parent != null)
                Parent.ParentComponent.renderQuality.onValueUpdated -= ParentComponent.OnRenderQualityUpdated;

            Root = null;

            Parent?.RemoveChild(this);
            Parent = null;

            // UpdateLayout();
        }

        public void AddChild(Transform transform)
        {
            Children.Add(transform);
            // UpdateLayout();
        }

        public void RemoveChild(Transform transform)
        {
            Children.Remove(transform);
        }

        public void UpdateLayout()
        {
            List<StackContentComponent> layoutComponents = new List<StackContentComponent>();

            if (Root != null)
                layoutComponents = SearchForLayoutComponentsRecursive(Root);
            else layoutComponents = SearchForLayoutComponentsRecursive(this);

            layoutComponents.Reverse();

            layoutComponents.ForEach(x => x.FullUpdateLayout());
        }

        private List<StackContentComponent> SearchForLayoutComponentsRecursive(Transform transform)
        {
            List<StackContentComponent> returnList = new();

            transform.ParentComponent.Components.ForEach((x) => { if (x is StackContentComponent) returnList.Add((StackContentComponent)x); });
            transform.Children.ForEach((x) => x.SearchForLayoutComponentsRecursive(x).ForEach((y) =>
            {
                if (!returnList.Contains((StackContentComponent)y)) returnList.Add((StackContentComponent)y);
            }));

            return returnList;
        }

        private Vector2 GetGlobalPosition(Vector2 localPosition)
        {
            var pBounds = (Parent != null) ? Parent.Bounds : ParentComponent.WindowRoot.Bounds;
            var padding = BoundsPadding.Value;

            return new Vector2(
                        pBounds.Left + pBounds.Width * Alignment.x + localPosition.x - Size.x * Anchor.x - padding,
                        pBounds.Top + pBounds.Height * Alignment.y + localPosition.y - Size.y * Anchor.y - padding
                    );
        }

        private SKRect GetBounds(int id)
        {
            var pad = BoundsPadding.Value;
            var pos = Position;

            switch (id)
            {
                case 0: // Full
                    return new SKRect(pos.x, pos.y, pos.x + Size.x + pad * 2, pos.y + Size.y + pad * 2);
                case 1: // Global
                    return new SKRect(pos.x + pad, pos.y + pad, pos.x + Size.x + pad, pos.y + Size.y + pad);
                default: // Local or any other
                    return new SKRect(pad, pad, Size.x + pad, Size.y + pad);
            }
        }

        public Vector2 TransformGlobalToLocal(Vector2 globalPoint)
        {
            var globalPosition = new Vector2(globalPoint);
            globalPosition = RMath.RotateVector2(globalPosition, new Vector2(Bounds.MidX, Bounds.MidY), -Rotation);
            globalPosition = RMath.ScaleVector2(globalPosition, new Vector2(Bounds.MidX, Bounds.MidY), 1 / Scale);
            globalPosition += -BoundsPadding.Value;
            globalPosition.x -= GetBounds(1).Left;
            globalPosition.y -= GetBounds(1).Top;
            return globalPosition;
        }

        public SKMatrix Create3DRotationMatrix(float rotationX = 0, float rotationY = 0, float rotationZ = 0, float depthScale = 1)
        {
            // Use the object's anchor point
            float anchorX = FullBounds.Width * Anchor.x;
            float anchorY = FullBounds.Height * Anchor.y;

            // Dynamically calculate z. Might break at larger or smaller values, maybe fix that later.
            float z = (3f * (0.1f / Size.Magnitude())) / depthScale;

            // Create and apply transformations in correct order
            var matrix = SKMatrix.CreateIdentity();

            // First translate to make anchor point the origin
            matrix = SKMatrix.Concat(matrix, SKMatrix.CreateTranslation(Position.x + anchorX, Position.y + anchorY));

            // Apply all rotations
            if (rotationZ != 0)
                matrix = SKMatrix.Concat(matrix, SKMatrix.CreateRotationDegrees(rotationZ));

            if (rotationX != 0)
            {
                float radians = rotationX * (float)Math.PI / 180;
                SKMatrix xRotate = SKMatrix.CreateIdentity();
                xRotate.ScaleY = (float)Math.Cos(radians);
                xRotate.Persp1 = -(float)Math.Sin(radians) * z;
                matrix = SKMatrix.Concat(matrix, xRotate);
            }

            if (rotationY != 0)
            {
                float radians = rotationY * (float)Math.PI / 180;
                SKMatrix yRotate = SKMatrix.CreateIdentity();
                yRotate.ScaleX = (float)Math.Cos(radians);
                yRotate.Persp0 = (float)Math.Sin(radians) * z;
                matrix = SKMatrix.Concat(matrix, yRotate);
            }

            // Translate back to original position
            matrix = SKMatrix.Concat(matrix, SKMatrix.CreateTranslation(-anchorX, -anchorY));

            return matrix;
        }

        public void Dispose()
        {
            if (Parent != null)
                Parent.ParentComponent.renderQuality.onValueUpdated -= ParentComponent.OnRenderQualityUpdated;
        }
    }

    public struct Vector2
    {
        public float x, y;

        public Vector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public Vector2(Vector2 v)
        {
            this.x = v.x;
            this.y = v.y;
        }

        public float Magnitude()
        {
            return (float)Math.Sqrt(Math.Abs(x * x + y * y));
        }

        public Vector2 Swapped()
        {
            return new Vector2(y, x);
        }

        public static Vector2 operator *(Vector2 c1, Vector2 c2)
        {
            return new Vector2(c1.x * c2.x, c1.y * c2.y);
        }

        public static Vector2 operator +(Vector2 c1, Vector2 c2)
        {
            return new Vector2(c1.x + c2.x, c1.y + c2.y);
        }

        public static Vector2 operator -(Vector2 c1, Vector2 c2)
        {
            return new Vector2(c1.x - c2.x, c1.y - c2.y);
        }

        public static Vector2 operator /(Vector2 c1, Vector2 c2)
        {
            return new Vector2(c1.x / c2.x, c1.y / c2.y);
        }

        public static Vector2 operator +(Vector2 c1, float c2)
        {
            return new Vector2(c1.x + c2, c1.y + c2);
        }

        public static Vector2 operator *(Vector2 c1, float c2)
        {
            return new Vector2(c1.x * c2, c1.y * c2);
        }

        public static Vector2 operator *(float c2, Vector2 c1)
        {
            return new Vector2(c1.x * c2, c1.y * c2);
        }

        public static Vector2 operator /(Vector2 c1, float c2)
        {
            return new Vector2(c1.x / c2, c1.y / c2);
        }

        public static Vector2 operator -(Vector2 c1, float c2)
        {
            return new Vector2(c1.x - c2, c1.y - c2);
        }

        public static Vector2 operator /(float c1, Vector2 c2)
        {
            return new Vector2(c1 / c2.x, c1 / c2.y);
        }

        public static Vector2 operator -(float c1, Vector2 c2)
        {
            return new Vector2(c1 - c2.x, c1 - c2.y);
        }

        public override bool Equals(object obj)
        {
            if (obj is Vector2)
                return x == ((Vector2)obj).x && y == ((Vector2)obj).y;

            return false;
        }

        public static bool operator ==(Vector2 c1, Vector2 c2)
        {
            return c1.x == c2.x && c1.y == c2.y;
        }

        public static bool operator !=(Vector2 c1, Vector2 c2)
        {
            return c1.x != c2.x || c1.y != c2.y;
        }

        public override string ToString()
        {
            return $"({x}, {y})";
        }
    }
}