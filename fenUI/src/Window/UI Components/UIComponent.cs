using System.Numerics;
using System.Security.Cryptography.X509Certificates;
using SkiaSharp;

namespace FenUISharp
{
    public abstract class UIComponent : IDisposable
    {
        public FTransform transform { get; set; }
        protected SKPaint skPaint { get; set; }

        public UIComponent(float x, float y, float width, float height)
        {
            transform = new FTransform();
            transform.localPosition = new Vector2(x, y);
            transform.size = new Vector2(width, height);

            CreatePaint();

            FWindow.onWindowUpdate += OnUpdate;
        }

        protected void CreatePaint(){
            skPaint = new SKPaint()
            {
                Color = SKColors.White,
                IsAntialias = true
            };
        }

        public virtual void DrawToScreen(SKCanvas canvas)
        {
        }

        protected virtual void OnUpdate() {}
        protected virtual void OnComponentDestroy() {}
        protected virtual void OnMouseEnter() {}
        protected virtual void OnMouseExit() {}
        protected virtual void OnMouseDown() {}
        protected virtual void OnMouseUp() {}

        public void Dispose()
        {
            OnComponentDestroy();
            FWindow.onWindowUpdate -= OnUpdate;
        }
    }

    public class FTransform {
        public FTransform? parent;

        public Vector2 position { get => GetGlobalPosition(localPosition); }
        public Vector2 localPosition { get; set; }
        public Vector2 anchor { get; set; } = new Vector2(0.5f, 0.5f);
        public Vector2 size { get; set; }

        public Vector2 scale { get; set; } = new Vector2(1, 1);
        public float rotation { get; set; } = 0;
        
        public SKRect fullBounds { get => GetBounds(0); }
        public SKRect bounds { get => GetBounds(1); }
        public SKRect localBounds { get => GetBounds(2); }

        public MultiAccess<int> boundsPadding = new MultiAccess<int>(0);

        public Vector2 alignment { get; set; } = new Vector2(0.5f, 0.5f); // Place object in the middle of parent
    
        private Vector2 GetGlobalPosition(Vector2 localPosition){
            var pBounds = (parent != null) ? parent.bounds : FWindow.bounds;

            return new Vector2(
                        pBounds.Location.X + pBounds.Size.Width * alignment.x + localPosition.x - size.x * anchor.x,
                        pBounds.Location.Y + pBounds.Size.Height * alignment.y + localPosition.y - size.y * anchor.y
                    );
        }

        private SKRect GetBounds(int id) {
            var padding = id == 0 ? boundsPadding.Value : 0;
            var pos = id <= 1 ? position : new Vector2(0, 0);
            return new SKRect(pos.x - padding, pos.y - padding, pos.x + size.x + padding, pos.y + size.y + padding);
        }
    }

    public struct Vector2 {
        public float x, y;

        public Vector2(float x, float y){
            this.x = x;
            this.y = y;
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
    }
}