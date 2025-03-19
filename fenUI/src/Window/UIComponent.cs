using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using SkiaSharp;

namespace FenUISharp
{
    public abstract class UIComponent : IDisposable
    {
        public readonly Window WindowRoot; 

        public Transform transform { get; set; }
        public SKPaint skPaint { get; set; }
        protected SKPaint drawImageFromCachePaint { get; set; }

        public List<Component> components { get; set; } = new List<Component>();

        public bool enabled { get; set; } = true;
        public bool careAboutInteractions { get; set; } = true;

        public static UIComponent? currentlySelected { get; set; } = null;

        public MultiAccess<float> renderQuality = new MultiAccess<float>(1);
        protected SKImageInfo? cachedImageInfo = null;
        protected SKSurface? cachedSurface = null;

        private bool _isMouseHovering = false;
        public bool _isGloballyInvalidated { get; set; }

        public UIComponent(Window rootWindow, Vector2 position, Vector2 size)
        {
            if(rootWindow == null) throw new Exception("Root window cannot be null."); 

            transform = new Transform(this);
            transform.localPosition = position;
            transform.size = size;

            CreatePaint();

            // rootWindow.onWindowUpdate += Update;
            rootWindow.MouseMove += OnMouseMove;
            rootWindow.MouseAction += OnMouseAction;
        }

        private void OnMouseAction(MouseInputCode inputCode){
            if (RMath.ContainsPoint(transform.bounds, new Vector2(Window.instance.MousePosition)) && GetTopmostComponentAtPosition(new Vector2(Window.instance.MousePosition)) == this)
            {
                
            }
        }

        private void OnMouseRightUp()
        {
            if (RMath.ContainsPoint(transform.bounds, new Vector2(Window.instance.MousePosition)) && GetTopmostComponentAtPosition(new Vector2(Window.instance.MousePosition)) == this)
            {
                OnMouseRight();
                components.ForEach(x => x.OnMouseRight());
            }
        }

        private void OnMouseLeftUp()
        {
            if (RMath.ContainsPoint(transform.bounds, new Vector2(Window.instance.MousePosition)) && GetTopmostComponentAtPosition(new Vector2(Window.instance.MousePosition)) == this)
            {
                if (currentlySelected != this) currentlySelected?.OnSelectedLost();

                currentlySelected = this;
                currentlySelected?.OnSelected();
                OnMouseUp();
                components.ForEach(x => x.OnMouseUp());
            }
        }

        private void OnMouseLeftDown()
        {
            if (RMath.ContainsPoint(transform.bounds, new Vector2(Window.instance.MousePosition)) && GetTopmostComponentAtPosition(new Vector2(Window.instance.MousePosition)) == this)
            {
                OnMouseDown();
                components.ForEach(x => x.OnMouseDown());
            }
        }

        private void OnMouseMove(Vector2 pos)
        {
            if (RMath.ContainsPoint(transform.bounds, new Vector2(x, y)) && !_isMouseHovering && GetTopmostComponentAtPosition(new Vector2(x, y)) == this)
            {
                _isMouseHovering = true;
                OnMouseEnter();
            }
            else if ((RMath.ContainsPoint(transform.bounds, new Vector2(x, y)) && _isMouseHovering && GetTopmostComponentAtPosition(new Vector2(x, y)) != this)
                || !RMath.ContainsPoint(transform.bounds, new Vector2(x, y)) && _isMouseHovering)
            {
                _isMouseHovering = false;
                OnMouseExit();
            }

            OnMouseMove(pos);
            components.ForEach(z => z.OnMouseMove(pos));
        }

        protected void CreatePaint()
        {
            drawImageFromCachePaint = new SKPaint()
            {
                Color = SKColors.White,
                IsAntialias = true
            };

            CreateSurfacePaint();
        }

        protected virtual void CreateSurfacePaint(){
            skPaint = new SKPaint()
            {
                Color = SKColors.White,
                IsAntialias = true
            };
        }

        public void DrawToScreen(SKCanvas canvas)
        {
            // Render quality

            float quality = RMath.Clamp(renderQuality.Value * ((transform.parent != null) ? transform.parent.parentComponent.renderQuality.Value : 1), 0.05f, 1);
            var bounds = transform.fullBounds;

            int c = canvas.Save();
            canvas.RotateDegrees(transform.rotation, transform.position.x + bounds.Width * transform.anchor.x, transform.position.y + bounds.Height * transform.anchor.y);
            canvas.Scale(transform.scale.x, transform.scale.y, transform.position.x + bounds.Width * transform.anchor.x, transform.position.y + bounds.Height * transform.anchor.y);

            // Applying custom transform
            if (transform.matrix != null)
                canvas.Concat(transform.matrix.Value);

            int scaledWidth = RMath.Clamp((int)(bounds.Width * quality), 1, int.MaxValue);
            int scaledHeight = RMath.Clamp((int)(bounds.Height * quality), 1, int.MaxValue);

            if (cachedSurface == null || cachedImageInfo == null)
            {
                cachedSurface?.Dispose(); // Dispose of old surface before creating a new one

                if (cachedImageInfo == null || cachedImageInfo?.Width != scaledWidth || cachedImageInfo?.Height != scaledHeight)
                    cachedImageInfo = new SKImageInfo(scaledWidth, scaledHeight);

                // Create an offscreen surface for this component
                cachedSurface = SKSurface.Create(cachedImageInfo.Value);
                if (cachedSurface != null)
                {
                    cachedSurface.Canvas.Scale(quality, quality);
                    DrawToSurface(cachedSurface.Canvas);

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

                    if (transform.matrix == null)
                        canvas.Translate(transform.position.x * quality, transform.position.y * quality);

                    canvas.DrawImage(snapshot, 0, 0, WindowRoot.FGRCONTEXT.samplingOptions, drawImageFromCachePaint);

                    canvas.Translate(-(transform.position.x * quality), -(transform.position.y * quality)); // Always move back to 0;0. Translate always happen, no matter if rotation matrix is set or not.
                    canvas.Scale(quality, quality); // Scale back for proper rendering

                    snapshot.Dispose();
                }
            }

            transform.childs.ForEach(c => c.parentComponent.DrawToScreen(canvas));
            canvas.RestoreToCount(c);
            _isGloballyInvalidated = false;
        }

        public void Invalidate()
        {
            cachedSurface?.Dispose();
            cachedSurface = null; // Mark for redraw

            cachedImageInfo = null;
            _isGloballyInvalidated = true;
        }

        public void SoftInvalidate()
        {
            _isGloballyInvalidated = true;
        }

        protected abstract void DrawToSurface(SKCanvas canvas);

        private void Update()
        {
            if (enabled)
            {
                OnUpdate();
                components.ForEach(x => x.OnComponentUpdate());
            }
        }

        protected virtual void OnUpdate() { }
        protected virtual void OnComponentDestroy() { }

        protected virtual void OnSelected() { }
        protected virtual void OnSelectedLost() { }

        protected virtual void OnMouseEnter() { }
        protected virtual void OnMouseExit() { }
        protected virtual void OnMouseActionCalled(MouseInputCode inputCode) { }
        protected virtual void OnMouseMoved(Vector2 pos) { }

        public void Dispose()
        {
            OnComponentDestroy();
            renderQuality.onValueUpdated -= OnRenderQualityUpdated;

            Console.WriteLine("DISPOSE ACTIONS INSIDE UICOMPONENT DISPOSE");

            if (currentlySelected == this) currentlySelected = null;
            if (Window.GetUIComponents().Contains(this)) Window.GetUIComponents().Remove(this);

            components.ForEach(x => x.Dispose());

            transform.Dispose();
        }

        public UIComponent? GetTopmostComponentAtPosition(Vector2 pos)
        {
            if (!Window.GetUIComponents().Any(x => x.enabled && x.careAboutInteractions && RMath.ContainsPoint(x.transform.bounds, pos))) return null;
            return Window.GetUIComponents().Last(x => x.enabled && x.careAboutInteractions && RMath.ContainsPoint(x.transform.bounds, pos));
        }

        public void SetColor(SKColor color)
        {
            skPaint.Color = color;
            Invalidate();
        }

        public void OnRenderQualityUpdated(float v)
        {
            Invalidate();
        }
    }

    public class Transform : IDisposable
    {
        public UIComponent parentComponent { get; private set; }

        public Transform? parent { get; private set; }
        public List<Transform> childs { get; private set; } = new List<Transform>();

        public SKMatrix? matrix { get; set; }

        public Vector2 position { get => GetGlobalPosition(_localPosition); }
        public Vector2 localPosition { get => _localPosition + boundsPadding.Value; set => _localPosition = value; }
        private Vector2 _localPosition { get; set; }
        public Vector2 anchor { get; set; } = new Vector2(0.5f, 0.5f);
        private Vector2 _size { get; set; }
        public Vector2 size { get => GetSize(); set => _size = value; }

        public Vector2 scale { get; set; } = new Vector2(1, 1);
        public float rotation { get; set; } = 0;

        public float marginHorizontal { get; set; } = 15;
        public float marginVertical { get; set; } = 15;
        public bool stretchHorizontal { get; set; } = false;
        public bool stretchVertical { get; set; } = false;

        public SKRect fullBounds { get => GetBounds(0); }
        public SKRect bounds { get => GetBounds(1); }
        public SKRect localBounds { get => GetBounds(2); }

        public MultiAccess<int> boundsPadding = new MultiAccess<int>(0);

        public Vector2 alignment { get; set; } = new Vector2(0.5f, 0.5f); // Place object in the middle of parent

        public Vector2 GetSize()
        {
            var sp = Window.bounds;
            var ss = parent?.size;

            var s = _size;

            var x = (parent == null) ? sp.Width : ss.Value.x;
            var y = (parent == null) ? sp.Height : ss.Value.y;

            if (stretchHorizontal) s.x = x - marginHorizontal * 2;
            if (stretchVertical) s.y = y - marginVertical * 2;

            return s;
        }

        public void SetParent(Transform transform)
        {
            parent = transform;
            parent.SetChild(this);

            parent.parentComponent.renderQuality.onValueUpdated += parentComponent.OnRenderQualityUpdated;
        }

        public void ClearParent()
        {
            if (parent != null)
                parent.parentComponent.renderQuality.onValueUpdated -= parentComponent.OnRenderQualityUpdated;

            parent?.RemoveChild(this);
            parent = null;
        }

        public void SetChild(Transform transform)
        {
            childs.Add(transform);
        }

        public void RemoveChild(Transform transform)
        {
            childs.Remove(transform);
        }

        public Transform(UIComponent component)
        {
            parentComponent = component;
        }

        private Vector2 GetGlobalPosition(Vector2 localPosition)
        {
            var pBounds = (parent != null) ? parent.bounds : Window.bounds;
            var padding = boundsPadding.Value;

            return new Vector2(
                        pBounds.Left + pBounds.Width * alignment.x + localPosition.x - size.x * anchor.x - padding,
                        pBounds.Top + pBounds.Height * alignment.y + localPosition.y - size.y * anchor.y - padding
                    );
        }

        private SKRect GetBounds(int id)
        {
            // var pad = (parent == null || id == 0) ? boundsPadding.Value : 0;
            var pad = boundsPadding.Value;
            var pos = position;

            // if (id == 0){
            //     return new SKRect(pos.x, pos.y, pos.x + size.x + pad * 2, pos.y + size.y + pad * 2);
            // }
            // else
            //     return new SKRect(pos.x + pad, pos.y + pad, pos.x + size.x + pad, pos.y + size.y + pad);

            switch (id)
            {
                case 0: // Full
                    return new SKRect(pos.x, pos.y, pos.x + size.x + pad * 2, pos.y + size.y + pad * 2);
                case 1: // Global
                    return new SKRect(pos.x + pad, pos.y + pad, pos.x + size.x + pad, pos.y + size.y + pad);
                default: // Local or any other
                    return new SKRect(pad, pad, size.x + pad, size.y + pad);
            }
        }

        public Vector2 TransformGlobalToLocal(Vector2 globalPoint)
        {
            var globalPosition = new Vector2(globalPoint);
            globalPosition = RMath.RotateVector2(globalPosition, new Vector2(bounds.MidX, bounds.MidY), -rotation);
            globalPosition = RMath.ScaleVector2(globalPosition, new Vector2(bounds.MidX, bounds.MidY), 1 / scale);
            globalPosition += -boundsPadding.Value;
            globalPosition.x -= GetBounds(1).Left;
            globalPosition.y -= GetBounds(1).Top;
            return globalPosition;
        }

        public SKMatrix Create3DRotationMatrix(float rotationX = 0, float rotationY = 0, float rotationZ = 0, float depthScale = 1)
        {
            // Use the object's anchor point
            float anchorX = fullBounds.Width * anchor.x;
            float anchorY = fullBounds.Height * anchor.y;

            // Dynamically calculate z. Might break at larger or smaller values, maybe fix that later.
            float z = (3f * (0.1f / size.Magnitude())) / depthScale;

            // Create and apply transformations in correct order
            var matrix = SKMatrix.CreateIdentity();

            // First translate to make anchor point the origin
            matrix = SKMatrix.Concat(matrix, SKMatrix.CreateTranslation(position.x + anchorX, position.y + anchorY));

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
            if(parent != null)
                parent.parentComponent.renderQuality.onValueUpdated -= parentComponent.OnRenderQualityUpdated;

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
    }
}