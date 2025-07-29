using FenUISharp.Mathematics;
using FenUISharp.Objects;
using FenUISharp.States;
using SkiaSharp;

namespace FenUISharp.RuntimeEffects
{
    public class FGradientSwipeEffect : IPostProcessEffect
    {
        public State<float> GradientPosition { get; private set; }

        public State<SKColor> Primary { get; private set; }
        public State<SKColor> Secondary { get; private set; }

        /// <summary>
        /// This multiplies the range of the modulo operation for the gradient position. 
        /// Can be useful for adding pauses in continuously moving gradients.
        /// </summary>
        public float ModuloRangeMultiplicator { get; set; } = 1f;

        /// <summary>
        /// 1 is the width of the surface
        /// </summary>
        public float GradientWidth { get; set; } = 1f;

        /// <summary>
        /// Uses the repeating mode on the gradient shader.
        /// </summary>
        public bool RepeatGradient { get; set; } = false;

        /// <summary>
        /// Maps the gradient's position to range of -1 to 1.
        /// Values outside of the range get repeated.
        /// </summary>
        public bool MapGradientPosition { get; set; } = true;

        public float RotationDegrees { get; set; } = 0;
        public float GradientRotation { get; set; } = 0;

        public FGradientSwipeEffect(UIObject owner)
        {
            GradientPosition = new(() => 0, owner, (x) => owner.Invalidate(UIObject.Invalidation.SurfaceDirty));
            Primary = new(() => SKColors.Gray, owner, (x) => owner.Invalidate(UIObject.Invalidation.SurfaceDirty));
            Secondary = new(() => SKColors.Transparent, owner, (x) => owner.Invalidate(UIObject.Invalidation.SurfaceDirty));
        }

        public void OnAfterRender(PPInfo info)
        {
            using var snapshop = info.source.Snapshot();

            // Horizontal offset
            float xOffset = RMath.Remap(MapGradientPosition ? WrapToRange(GradientPosition.CachedValue) : GradientPosition.CachedValue, -1f, 1f,
                                       -info.owner.Shape.LocalBounds.Width,
                                       info.owner.Shape.LocalBounds.Width);

            var bounds = info.owner.Shape.SurfaceDrawRect;
            float centerX = bounds.MidX;
            float halfWidth = info.owner.Shape.LocalBounds.Width * GradientWidth / 2f;
            float xLeft = centerX - halfWidth + xOffset;
            float xRight = centerX + halfWidth + xOffset;

            var left = new SKPoint(xLeft, 0);
            var right = new SKPoint(xRight, 0);

            left = RMath.RotatePoint(left, new SKPoint(bounds.MidX, bounds.MidY), GradientRotation);
            right = RMath.RotatePoint(right, new SKPoint(bounds.MidX, bounds.MidY), GradientRotation);

            using var gradient = SKShader.CreateLinearGradient(
                left,
                right,
                new[] { Secondary.CachedValue, Primary.CachedValue, Secondary.CachedValue },
                new float[] { 0.0f, 0.5f, 1f },
                RepeatGradient ? SKShaderTileMode.Repeat : SKShaderTileMode.Clamp
            );

            var layerPaint = new SKPaint
            {
                BlendMode = SKBlendMode.Plus // Will mask based on source alpha
            };

            info.target.Canvas.SaveLayer(SKRect.Create(0, 0, info.sourceInfo.Width, info.sourceInfo.Height), layerPaint);

            using var drawPaint = new SKPaint { Shader = gradient, BlendMode = SKBlendMode.SrcATop };

            info.target.Canvas.Save();
            info.target.Canvas.ResetMatrix();
            info.target.Canvas.DrawImage(snapshop, 0, 0);
            info.target.Canvas.Restore();

            info.target.Canvas.RotateDegrees(RotationDegrees, bounds.MidX, bounds.MidY);
            info.target.Canvas.DrawRect(SKRect.Create(0, 0, info.sourceInfo.Width, info.sourceInfo.Height), drawPaint);

            info.target.Canvas.Restore();
        }

        public float WrapToRange(float value)
        {
            if (value == 0.0f)
                return 0.0f;

            float scaledValue = value / ModuloRangeMultiplicator;
            float normalized = ((scaledValue + 1.0f) % 2.0f + 2.0f) % 2.0f;
            return (normalized - 1.0f) * ModuloRangeMultiplicator;
        }

        public void OnBeforeRender(PPInfo info)
        {

        }

        public void OnLateAfterRender(PPInfo info)
        {

        }
    }
}