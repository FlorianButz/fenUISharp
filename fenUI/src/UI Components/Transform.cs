using FenUISharp.Components;
using FenUISharp.Mathematics;
using SkiaSharp;

namespace FenUISharp
{
    public class Transform : IDisposable
    {
        public UIComponent ParentComponent { get; private set; }

        public Transform? Root { get; private set; }
        public Transform? Parent { get; private set; }
        public List<Transform> Children { get; private set; } = new List<Transform>();

        public int ZIndex { get; set; } = 0;
        public int CreationIndex { get; init; } = 0; // Stores the actual order
        private static int _lastCreationIndex = 0;

        public SKMatrix? Matrix { get; private set; }

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

            var x = (Parent == null) ? sp.Width : (ss ?? Vector2.Zero).x;
            var y = (Parent == null) ? sp.Height : (ss ?? Vector2.Zero).y;

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
            Parent.ParentComponent.RenderQuality.onValueUpdated += ParentComponent.OnRenderQualityUpdated;

            // UpdateLayout();
        }

        public void ClearParent()
        {
            if (Parent != null)
                Parent.ParentComponent.RenderQuality.onValueUpdated -= ParentComponent.OnRenderQualityUpdated;

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

        public void Apply3DRotationMatrix(float rotationX = 0, float rotationY = 0, float rotationZ = 0, float depthScale = 1)
        {
            // Use the object's anchor point
            float anchorX = FullBounds.Width * Anchor.x;
            float anchorY = FullBounds.Height * Anchor.y;

            // Dynamically calculate z. Might break at larger or smaller values, maybe fix that later.
            float z = (3f * (0.1f / Size.Magnitude)) / depthScale;

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

            this.Matrix = matrix;
        }

        public void ResetMatrix()
        {
            this.Matrix = null;
        }

        public void Dispose()
        {
            if (Parent != null)
                Parent.ParentComponent.RenderQuality.onValueUpdated -= ParentComponent.OnRenderQualityUpdated;
        }
    }
}