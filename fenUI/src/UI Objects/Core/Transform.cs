using FenUISharp.Mathematics;
using FenUISharp.States;
using SkiaSharp;

namespace FenUISharp.Objects
{
    public class Transform : IDisposable, IStateListener
    {
        public UIObject Owner { get; init; }

        public State<Vector2> LocalPosition { get; init; }
        public Vector2 Position { get; private set; }

        internal Vector2 CalculatedLayoutOff;
        internal Vector2 CalculatedAnchorCorrection;

        public State<bool> SnapPositionToPixelGrid { get; init; }

        public State<Vector2> Size { get; init; }
        public State<Vector2> Scale { get; init; }
        public State<float> Rotation { get; init; }
        public State<Vector2> Anchor { get; init; }

        public TransformMatrixProcessor? MatrixProcessor { get; set; }
        
        public SKMatrix DrawMatrix { get; internal set; }
        public SKMatrix RecursiveDrawMatrix { get; internal set; }

        public Transform(UIObject owner)
        {
            this.Owner = owner;

            SnapPositionToPixelGrid = new(() => false, this);
            LocalPosition = new(() => new(0, 0), this);
            Size = new(() => new(0, 0), this);
            Scale = new(() => new(1, 1), this);
            Rotation = new(() => 0, this);
            Anchor = new(() => new(0.5f, 0.5f), this);
        }

        public void UpdateTransform()
        {
            var matrix = SKMatrix.CreateIdentity();

            if (MatrixProcessor != null)
                matrix = SKMatrix.Concat(matrix, MatrixProcessor.ProcessMatrix(matrix));

            // Layout calculations
            var size = Owner.Layout.ApplyLayoutToSize(Size.CachedValue);
            Owner.Layout.ApplyLayoutToPositioning(size, out CalculatedLayoutOff, out CalculatedAnchorCorrection);

            Position = LocalPosition.CachedValue + (CalculatedLayoutOff - CalculatedAnchorCorrection);

            // Pivot calculations
            Vector2 pivot = new(Owner.Layout.GetSize(Owner.Transform.Size.CachedValue).x * Anchor.CachedValue.x, Owner.Layout.GetSize(Owner.Transform.Size.CachedValue).y * Anchor.CachedValue.y); // TODO: Check if that even works

            if (SnapPositionToPixelGrid.CachedValue)
                Position = new(MathF.Round(Position.x), MathF.Round(Position.y));
            if (SnapPositionToPixelGrid.CachedValue)
                pivot = new(MathF.Round(pivot.x), MathF.Round(pivot.y));

            // Apply to matrix
                matrix = SKMatrix.Concat(matrix, SKMatrix.CreateTranslation(Position.x, Position.y));
            matrix = SKMatrix.Concat(matrix, SKMatrix.CreateRotationDegrees(Rotation.CachedValue, pivot.x, pivot.y));
            matrix = SKMatrix.Concat(matrix, SKMatrix.CreateScale(Scale.CachedValue.x, Scale.CachedValue.y, pivot.x, pivot.y));

            // Cache matrices
            DrawMatrix = matrix;
            RecursiveDrawMatrix = GetRecursiveDrawMatrix();
        }

        public SKMatrix GetRecursiveDrawMatrix()
        {
            var matrix = DrawMatrix;
            var current = this.Owner.Parent;

            while (current != null)
            {
                matrix = SKMatrix.Concat(current.Transform.DrawMatrix, matrix);
                current = current.Parent;
            }

            return matrix;
        }

        public Vector2 DrawLocalToGlobal(Vector2 local, SKMatrix? matrix = null)
        {
            var point = (matrix ?? RecursiveDrawMatrix).MapPoint(local.x, local.y);
            return new(point.X, point.Y);
        }

        public SKRect DrawLocalToGlobal(SKRect local, SKMatrix? matrix = null)
        {
            return (matrix ?? RecursiveDrawMatrix).MapRect(local);
        }

        public Vector2 GlobalToDrawLocal(Vector2 local, SKMatrix? matrix = null)
        {
            var point = (matrix ?? RecursiveDrawMatrix).Invert().MapPoint(local.x, local.y);
            return new(point.X, point.Y);
        }
        
        public SKRect GlobalToDrawLocal(SKRect local, SKMatrix? matrix = null)
        {
            return (matrix ?? RecursiveDrawMatrix).Invert().MapRect(local);
        }

        public SKMatrix LocalToGlobalMatrix()
        {
            var matrix = Owner.Parent?.Transform.RecursiveDrawMatrix ?? SKMatrix.CreateIdentity();

            matrix = SKMatrix.Concat(matrix, SKMatrix.CreateTranslation(CalculatedLayoutOff.x, CalculatedLayoutOff.y));

            return matrix;
        }

        public SKMatrix GlobalToLocalMatrix()
        {
            var localToGlobal = LocalToGlobalMatrix();
            localToGlobal.TryInvert(out var inverse);
            return inverse;
        }

        public Vector2 LocalToGlobal(Vector2 local)
        {
            var matrix = LocalToGlobalMatrix();
            var point = matrix.MapPoint(local.x, local.y);
            
            return new(point.X, point.Y);
        }

        public SKRect LocalToGlobal(SKRect local)
        {
            return LocalToGlobalMatrix().MapRect(local);
        }

        public Vector2 GlobalToLocal(Vector2 global)
        {
            var point = GlobalToLocalMatrix().MapPoint(global.x, global.y);
            return new(point.X, point.Y);
        }

        public SKRect GlobalToLocal(SKRect global)
        {
            return GlobalToLocalMatrix().MapRect(global);
        }

        public void Dispose()
        {
            SnapPositionToPixelGrid.Dispose();
            LocalPosition.Dispose();
            Size.Dispose();
            Scale.Dispose();
            Rotation.Dispose();
            Anchor.Dispose();
        }

        public virtual void OnInternalStateChanged<T>(T value)
        {
            Owner.Invalidate(UIObject.Invalidation.TransformDirty);
        }
    }

    public abstract class TransformMatrixProcessor
    {
        private TransformMatrixProcessor? _inner;

        public TransformMatrixProcessor(TransformMatrixProcessor? inner = null)
        {
            this._inner = inner;
        }

        public SKMatrix ProcessMatrix(SKMatrix matrix)
        {
            return Process(_inner != null ? _inner.ProcessMatrix(matrix) : matrix);
        }

        protected abstract SKMatrix Process(SKMatrix matrix);
    }
}