using FenUISharp.Behavior;
using FenUISharp.Mathematics;
using FenUISharp.Objects;
using FenUISharp.States;
using SkiaSharp;

namespace FenUISharp.Behavior
{
    public class Rotation3DTransformComponent : BehaviorComponent
    {
        public State<float> RotationX { get; init; }
        public State<float> RotationY { get; init; }
        public State<float> RotationZ { get; init; }
        public State<float> DepthScale { get; init; }

        public Rotation3DTransformComponent(UIObject owner) : base(owner)
        {
            RotationX = new(() => 0, Owner, Owner);
            RotationY = new(() => 0, Owner, Owner);
            RotationZ = new(() => 0, Owner, Owner);
            DepthScale = new(() => 1, Owner, Owner);
        }

        public override void HandleEvent(BehaviorEventType type, object? data = null)
        {
            base.HandleEvent(type, data);

            if (RMath.Approximately(RotationX.CachedValue, 0) && RMath.Approximately(RotationY.CachedValue, 0) && RMath.Approximately(RotationZ.CachedValue, 0))
                return;

            if (type == BehaviorEventType.BeforeRender)
            {
                if (data == null) return;
                SKCanvas canvas = (SKCanvas)data;

                Apply3DRotationMatrix(canvas, RotationX.CachedValue, RotationY.CachedValue, RotationZ.CachedValue);
            }
        }

        public void Apply3DRotationMatrix(SKCanvas canvas, float rotationX = 0, float rotationY = 0, float rotationZ = 0, float depthScale = 1)
        {
            // Use the object's anchor point
            Vector2 size = Owner.Layout.ApplyLayoutToSize(Owner.Transform.Size.CachedValue);

            // Dynamically calculate z. Might break at larger or smaller values, maybe fix that later.
            float z = (3f * (0.1f / size.Magnitude)) / depthScale;

            // Create and apply transformations in correct order
            var matrix = SKMatrix.CreateIdentity();

            var pivot = Owner.Transform.Pivot;

            // First translate to make anchor point the origin
            matrix = SKMatrix.Concat(matrix, SKMatrix.CreateTranslation(pivot.x, pivot.y));

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
            matrix = SKMatrix.Concat(matrix, SKMatrix.CreateTranslation(-pivot.x, -pivot.y));

            canvas.Concat(matrix);
        }
    }
}