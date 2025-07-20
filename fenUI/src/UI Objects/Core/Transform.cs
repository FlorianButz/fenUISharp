using FenUISharp.Mathematics;
using FenUISharp.States;
using SkiaSharp;

namespace FenUISharp.Objects
{
    public class Transform : IDisposable, IStateListener
    {
        public UIObject Owner { get; private set; }

        public State<Vector2> LocalPosition { get; init; }
        public Vector2 Position { get; private set; }

        public Vector2 Pivot { get; private set; }

        internal Vector2 CalculatedLayoutOff;
        internal Vector2 CalculatedAnchorCorrection;

        public State<bool> SnapPositionToPixelGrid { get; init; }

        /// <summary>
        /// The actual size of the object taking min and max size from Layout into account
        /// </summary>
        public Vector2 VisibleSize { get => Owner.Layout.ClampSize(Size.CachedValue); }

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

            SnapPositionToPixelGrid = new(() => false, Owner, this);
            LocalPosition = new(() => new(0, 0), Owner, this);
            Size = new(() => new(0, 0), Owner, this);
            Scale = new(() => new(1, 1), Owner, this);
            Rotation = new(() => 0, Owner, this);
            Anchor = new(() => new(0.5f, 0.5f), Owner, this);
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
            var layoutSize = Owner.Layout.ApplyLayoutToSize(Owner.Transform.Size.CachedValue);
            Pivot = new(layoutSize.x * Anchor.CachedValue.x, layoutSize.y * Anchor.CachedValue.y);

            if (SnapPositionToPixelGrid.CachedValue)
                Position = new(MathF.Round(Position.x), MathF.Round(Position.y));
            if (SnapPositionToPixelGrid.CachedValue)
                Pivot = new(MathF.Round(Pivot.x), MathF.Round(Pivot.y));

            // Apply to matrix
            matrix = SKMatrix.Concat(matrix, SKMatrix.CreateTranslation(Position.x, Position.y));
            matrix = SKMatrix.Concat(matrix, SKMatrix.CreateRotationDegrees(Rotation.CachedValue, Pivot.x, Pivot.y));
            matrix = SKMatrix.Concat(matrix, SKMatrix.CreateScale(Scale.CachedValue.x, Scale.CachedValue.y, Pivot.x, Pivot.y));

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

        public virtual void OnInternalStateChanged<T>(T value)
        {
            Owner.Invalidate(UIObject.Invalidation.TransformDirty);
        }

        public void Dispose()
        {
            MatrixProcessor = null;
            Owner = null;
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