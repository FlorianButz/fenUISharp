using FenUISharp.Behavior;
using FenUISharp.Materials;
using FenUISharp.Mathematics;
using FenUISharp.Objects;
using FenUISharp.States;
using SkiaSharp;

namespace FenUISharp.AnimatedVectors
{
    public class FAVDisplay : FDisplayableType
    {
        public State<AnimatedVector> AnimatedVector { get; private set; }
        private AnimatedVector currentAnimVector;
        private FAVAnimator? currentAnimation;

        private Dictionary<int, AVPathAnimationOverride> PathOverrides { get; set; } = new();

        bool ignoreAnimSwap = false;

        public void GetOrCreatePathOverride(int pathID, out AVPathAnimationOverride ovrd)
        {
            if (PathOverrides.Any(x => x.Key == pathID))
            {
                ovrd = PathOverrides.Last(x => x.Key == pathID).Value;
                return;
            }
            else
            {
                ovrd = new();
                PathOverrides.Add(pathID, ovrd);
                return;
            }
        }

        public FAVDisplay(Func<AnimatedVector> animatedVector, bool dynamicColor = false, Func<Vector2>? position = null, Func<Vector2>? size = null) : base(position, size, dynamicColor: dynamicColor)
        {
            AnimatedVector = new(animatedVector, this, this);
            currentAnimVector = AnimatedVector.CachedValue;
            Padding.SetResponsiveState(() => AnimatedVector.CachedValue.ExtendBounds);

            RenderMaterial.SetStaticState(new EmptyDefaultMaterial() { BaseColor = () => SKColors.White });

            AnimatedVector.Subscribe((x) =>
            {
                if (ignoreAnimSwap)
                {
                    ignoreAnimSwap = false;
                    return;
                }

                var transition = () =>
                {
                    // Animators need atleast one frame before running. Making sure there's no flickering while transitioning
                    ObjectSurface.LockInvalidation = true;
                    FContext.GetCurrentDispatcher().InvokeLater(() => ObjectSurface.LockInvalidation = false, 1L);

                    currentAnimVector = x;
                    PlayAnimation("in");
                };

                if (currentAnimVector != null)
                    PlayAnimation("out", transition);
                else
                    transition();
            });
        }

        public override void Render(SKCanvas canvas)
        {
            // base.Render(canvas);

            foreach (var path in currentAnimVector.Paths)
            {
                // int save = canvas.Save();
                using var displayedPath = new SKPath(path.SKPath);

                // Get or create path override
                AVPathAnimationOverride? pathOverride;
                if (PathOverrides.Any(x => x.Key == currentAnimVector.Paths.IndexOf(path)))
                    pathOverride = PathOverrides.Last(x => x.Key == currentAnimVector.Paths.IndexOf(path)).Value;
                else
                {
                    pathOverride = new();
                    PathOverrides.Add(currentAnimVector.Paths.IndexOf(path), pathOverride);
                }

                using var layerPaint = new SKPaint { Color = SKColors.White.WithAlpha((byte)(pathOverride.Opacity * 255)) };
                if (pathOverride.BlurRadius > 0)
                    using (var blur = SKImageFilter.CreateBlur(pathOverride.BlurRadius, pathOverride.BlurRadius))
                        layerPaint.ImageFilter = blur;

                int layer = canvas.SaveLayer(layerPaint);

                // Scale to fit content
                var viewBox = currentAnimVector.ViewBox;
                float scaleX = (float)Shape.LocalBounds.Width / viewBox.Width;
                float scaleY = (float)Shape.LocalBounds.Height / viewBox.Height;
                float scale = Math.Min(scaleX, scaleY);

                displayedPath.GetBounds(out SKRect pathBounds);

                // Scale the path, not the canvas
                var matrix = SKMatrix.CreateScale(scale * 2, scale * 2);

                // Animated values
                Vector2 pivot = new(pathOverride.Anchor.x * pathBounds.Width + pathBounds.Left, pathOverride.Anchor.y * pathBounds.Height + pathBounds.Top);
                if (pathOverride.UseObjectAnchor)
                {
                    pivot = new(pathOverride.Anchor.x * Shape.LocalBounds.Width, pathOverride.Anchor.y * Shape.LocalBounds.Height);
                    pivot /= scale * 2;
                }

                matrix = SKMatrix.Concat(matrix, SKMatrix.CreateRotationDegrees(pathOverride.Rotation, pivot.x, pivot.y));
                // canvas.RotateDegrees(path.Rotation, pivot.x, pivot.y);

                matrix = SKMatrix.Concat(matrix, SKMatrix.CreateScale(RMath.Clamp(pathOverride.Scale.x, 0, 99999), RMath.Clamp(pathOverride.Scale.y, 0, 99999), pivot.x, pivot.y));
                // canvas.Scale(RMath.Clamp(path.Scale.x, 0, 99999), RMath.Clamp(path.Scale.y, 0, 99999), pivot.x, pivot.y);

                if (pathOverride.UseObjectSizeTranslation)
                {
                    matrix = SKMatrix.Concat(matrix, SKMatrix.CreateTranslation(pathOverride.Translation.x * Shape.LocalBounds.Width, pathOverride.Translation.y * Shape.LocalBounds.Height));
                    // canvas.Translate(path.Translation.x * Shape.LocalBounds.Width, path.Translation.y * Shape.LocalBounds.Height);
                }

                matrix = SKMatrix.Concat(matrix, SKMatrix.CreateTranslation(-viewBox.Left, -viewBox.Top));
                // canvas.Translate(-viewBox.Left, -viewBox.Top);

                if (!pathOverride.UseObjectSizeTranslation)
                {
                    matrix = SKMatrix.Concat(matrix, SKMatrix.CreateTranslation(pathOverride.Translation.x * pathBounds.Width, pathOverride.Translation.y * pathBounds.Height));
                    // canvas.Translate(path.Translation.x * pathBounds.Width, path.Translation.y * pathBounds.Height);
                }

                displayedPath.Transform(matrix);

                // Draw path
                using var paint = GetRenderPaint();
                paint.Color = path.Fill;

                if (pathOverride.StrokeTrace != 1f)
                {
                    float totalLength = path.ApproximateLength();
                    float visibleLength = totalLength * pathOverride.StrokeTrace;

                    paint.PathEffect = SKPathEffect.CreateDash(new float[] { visibleLength, totalLength }, 0);
                }

                RenderMaterial.CachedValue.DrawWithMaterial(canvas, displayedPath, this, paint);

                // Draw blend
                paint.Color = TintColor.CachedValue;
                paint.BlendMode = TintBlendMode.CachedValue;
                RenderMaterial.CachedValue.DrawWithMaterial(canvas, displayedPath, this, paint);

                if (path.StrokeWidth > 0)
                {
                    paint.BlendMode = SKBlendMode.SrcOver;

                    paint.IsStroke = true;
                    paint.Color = path.Stroke;
                    paint.StrokeWidth = path.StrokeWidth;
                    paint.StrokeCap = currentAnimVector.LineCap;
                    paint.StrokeJoin = currentAnimVector.LineJoin;

                    RenderMaterial.CachedValue.DrawWithMaterial(canvas, displayedPath, this, paint);

                    // Draw blend
                    paint.Color = TintColor.CachedValue;
                    paint.BlendMode = TintBlendMode.CachedValue;
                    RenderMaterial.CachedValue.DrawWithMaterial(canvas, displayedPath, this, paint);
                }

                canvas.RestoreToCount(layer);
                // canvas.RestoreToCount(save);
            }
        }

        public void SilentSetFAV(AnimatedVector fav)
        {
            ignoreAnimSwap = true;

            currentAnimVector = fav;
            AnimatedVector.SetStaticState(fav);
            Invalidate(Invalidation.SurfaceDirty);
        }

        public void PlayAnimation(string id, Action? onComplete = null)
        {
            if (!currentAnimVector.Animations.Any(x => x.id == id)) // No animation found in FAV, skip
            {
                onComplete?.Invoke();
                return;
            }

            // Stop animation early if needed
            if (currentAnimation != null)
            {
                currentAnimation.StopAnimation();
                currentAnimation = null;
            }

            onComplete += () => currentAnimation = null;

            currentAnimation = new();
            currentAnimation.PlayAnimation(this, currentAnimVector, id, onComplete);
        }
    }
}