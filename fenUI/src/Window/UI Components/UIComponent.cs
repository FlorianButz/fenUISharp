using System.Numerics;
using System.Security.Cryptography.X509Certificates;
using SkiaSharp;

namespace FenUISharp
{
    public abstract class UIComponent : IDisposable
    {
        public FTransform transform { get; set; }
        public SKPaint skPaint { get; set; }

        public bool enabled { get; set; } = true;
        public bool careAboutInteractions { get; set; } = true;

        public static UIComponent? currentlySelected { get; set; } = null;

        protected SKImageInfo? cachedImageInfo = null;
        protected SKSurface? cachedSurface = null;
        // public bool useSurfaceCaching { get; set; } = true;

        public MultiAccess<float> renderQuality = new MultiAccess<float>(1);

        protected Win32Helper.Cursors hoverCursor = Win32Helper.Cursors.IDC_ARROW;

        private bool _isMouseHovering = false;

        public UIComponent(float x, float y, float width, float height)
        {
            transform = new FTransform();
            transform.localPosition = new Vector2(x, y);
            transform.size = new Vector2(width, height);

            CreatePaint();

            FWindow.onWindowUpdate += Update;
            FWindow.onMouseMove += OnMouseMove;
            FWindow.onMouseLeftDown += OnMouseLeftDown;
            FWindow.onMouseLeftUp += OnMouseLeftUp;
            FWindow.onMouseRightUp += OnMouseRightUp;

            renderQuality.onValueUpdated += (x) => { Invalidate(); };
        }

        private void OnMouseRightUp()
        {
            OnMouseRight();
        }

        private void OnMouseLeftUp()
        {
            if (FMath.ContainsPoint(transform.bounds, new Vector2(FWindow.MousePosition)) && GetTopmostComponentAtPosition(new Vector2(FWindow.MousePosition)) == this)
            {
                if (currentlySelected != this) currentlySelected?.OnSelectedLost();

                currentlySelected = this;
                currentlySelected?.OnSelected();
            }

            OnMouseUp();
        }

        private void OnMouseLeftDown()
        {
            OnMouseDown();
        }

        private void OnMouseMove(int x, int y)
        {
            if (FMath.ContainsPoint(transform.bounds, new Vector2(x, y)) && !_isMouseHovering && GetTopmostComponentAtPosition(new Vector2(x, y)) == this)
            {
                _isMouseHovering = true;
                FWindow.ActiveCursor.SetValue(this, hoverCursor, 15);

                OnMouseEnter();
            }
            else if ((FMath.ContainsPoint(transform.bounds, new Vector2(x, y)) && _isMouseHovering && GetTopmostComponentAtPosition(new Vector2(x, y)) != this)
                || !FMath.ContainsPoint(transform.bounds, new Vector2(x, y)) && _isMouseHovering)
            {
                _isMouseHovering = false;
                FWindow.ActiveCursor.DissolveValue(this);

                OnMouseExit();
            }

            OnMouseMove(new Vector2(x, y));
        }

        protected void CreatePaint()
        {
            skPaint = new SKPaint()
            {
                Color = SKColors.White,
                IsAntialias = true
            };
        }

        public void DrawToScreen(SKCanvas canvas)
        {
            // Render quality

            float quality = FMath.Clamp(renderQuality.Value, 0.05f, 1);
            var bounds = transform.fullBounds;

            int c = canvas.Save();
            canvas.RotateDegrees(transform.rotation, bounds.MidX, bounds.MidY);
            canvas.Scale(transform.scale.x, transform.scale.y, bounds.MidX, bounds.MidY);

            // if (useSurfaceCaching)
            // {
            int scaledWidth = FMath.Clamp((int)(bounds.Width * quality), 1, int.MaxValue);
            int scaledHeight = FMath.Clamp((int)(bounds.Height * quality), 1, int.MaxValue);

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
                }
            }

            if (cachedSurface != null)
            {
                using (var snapshot = cachedSurface.Snapshot())
                {
                    // Draw the cached surface onto the main canvas
                    canvas.Scale(1 / quality, 1 / quality); // Scale for proper rendering
                    canvas.DrawImage(snapshot, transform.position.x * quality, transform.position.y * quality, FWindow.samplingOptions, null);
                    canvas.Scale(quality, quality); // Scale for proper rendering

                    snapshot.Dispose();
                }
            }
            // }
            
            canvas.RestoreToCount(c);
        }

        public void Invalidate()
        {
            cachedSurface?.Dispose();
            cachedSurface = null; // Mark for redraw

            cachedImageInfo = null;
        }
        
        protected abstract void DrawToSurface(SKCanvas canvas);

        private void Update()
        {
            if (enabled) OnUpdate();
        }

        protected virtual void OnUpdate() { }
        protected virtual void OnComponentDestroy() { }
        protected virtual void OnSelected() { }
        protected virtual void OnSelectedLost() { }
        protected virtual void OnMouseEnter() { }
        protected virtual void OnMouseExit() { }
        protected virtual void OnMouseDown() { }
        protected virtual void OnMouseUp() { }
        protected virtual void OnMouseRight() { }
        protected virtual void OnMouseMove(Vector2 pos) { }

        public void Dispose()
        {
            OnComponentDestroy();
            FWindow.onWindowUpdate -= OnUpdate;
            FWindow.onMouseMove -= OnMouseMove;
            FWindow.onMouseLeftDown -= OnMouseLeftDown;

            if (currentlySelected == this) currentlySelected = null;
        }

        public UIComponent? GetTopmostComponentAtPosition(Vector2 pos)
        {
            return FWindow.uiComponents.Last(x => x.enabled && x.careAboutInteractions && FMath.ContainsPoint(x.transform.fullBounds, pos)); ;
        }
    }

    public class FTransform
    {
        public FTransform? parent;

        public Vector2 position { get => GetGlobalPosition(_localPosition); }
        public Vector2 localPosition { get => _localPosition + boundsPadding.Value; set => _localPosition = value; }
        private Vector2 _localPosition { get; set; }
        public Vector2 anchor { get; set; } = new Vector2(0.5f, 0.5f);
        public Vector2 size { get; set; }

        public Vector2 scale { get; set; } = new Vector2(1, 1);
        public float rotation { get; set; } = 0;

        public SKRect fullBounds { get => GetBounds(0); }
        public SKRect bounds { get => GetBounds(1); }
        public SKRect localBounds { get => GetBounds(2); }

        public MultiAccess<int> boundsPadding = new MultiAccess<int>(0);

        public Vector2 alignment { get; set; } = new Vector2(0.5f, 0.5f); // Place object in the middle of parent

        private Vector2 GetGlobalPosition(Vector2 localPosition)
        {
            var pBounds = (parent != null) ? parent.bounds : FWindow.bounds;
            var padding = (parent == null) ? boundsPadding.Value : 0;

            return new Vector2(
                        pBounds.Left + pBounds.Width * alignment.x + localPosition.x - size.x * anchor.x - padding,
                        pBounds.Top + pBounds.Height * alignment.y + localPosition.y - size.y * anchor.y - padding
                    );
        }

        private SKRect GetBounds(int id)
        {
            var pad = boundsPadding.Value;

            var pos = id <= 1 ? position : new Vector2(0, 0);
            if (id != 2)
                return new SKRect(pos.x - pad, pos.y - pad, pos.x + size.x + pad, pos.y + size.y + pad);
            else
                return new SKRect(pos.x + pad, pos.y + pad, pos.x + size.x + pad, pos.y + size.y + pad);
        }

        public Vector2 TransformGlobalToLocal(Vector2 globalPoint)
        {
            var globalPosition = new Vector2(globalPoint);
            globalPosition = FMath.RotateVector2(globalPosition, new Vector2(bounds.MidX, bounds.MidY), -rotation);
            globalPosition = FMath.ScaleVector2(globalPosition, new Vector2(bounds.MidX, bounds.MidY), 1 / scale);
            globalPosition += -boundsPadding.Value;
            globalPosition.x -= GetBounds(1).Left;
            globalPosition.y -= GetBounds(1).Top;
            return globalPosition;
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