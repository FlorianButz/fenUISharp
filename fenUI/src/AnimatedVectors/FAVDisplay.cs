using FenUISharp.Behavior;
using FenUISharp.Mathematics;
using FenUISharp.Objects;
using FenUISharp.States;
using SkiaSharp;

namespace FenUISharp.AnimatedVectors
{
    public class FAVDisplay : UIObject
    {
        public State<AnimatedVector> AnimatedVector { get; private set; }
        private FAVAnimator? currentAnimation;

        private AnimatedVector currentAnimVector;

        public FAVDisplay(Func<AnimatedVector> animatedVector, Func<Vector2>? position = null, Func<Vector2>? size = null) : base(position, size)
        {
            AnimatedVector = new(animatedVector, this, this);
            currentAnimVector = AnimatedVector.CachedValue;
            Padding.SetResponsiveState(() => AnimatedVector.CachedValue.ExtendBounds);

            AnimatedVector.Subscribe((x) =>
            {
                Console.WriteLine("Init swap");
                var transition = () =>
                {
                    Console.WriteLine("Transition");
                    // Animators need atleast one frame before running. Making sure there's no flickering while transitioning
                    ObjectSurface.LockInvalidation = true;
                    FContext.GetCurrentDispatcher().InvokeLater(() => ObjectSurface.LockInvalidation = false, 1L);

                    currentAnimVector = x;
                    PlayAnimation("in", () => Console.WriteLine("Done"));
                };

                Console.WriteLine("Play out");
                if (currentAnimVector != null)
                    PlayAnimation("out", transition);
                else
                    transition();
            });
        }

        public override void Render(SKCanvas canvas)
        {
            base.Render(canvas);

            foreach (var path in currentAnimVector.Paths)
            {
                // int save = canvas.Save();

                using var layerPaint = new SKPaint { Color = SKColors.White.WithAlpha((byte)(path.Opacity * 255)) };
                if (path.BlurRadius > 0)
                    using (var blur = SKImageFilter.CreateBlur(path.BlurRadius, path.BlurRadius))
                        layerPaint.ImageFilter = blur;

                int layer = canvas.SaveLayer(layerPaint);

                // Animated values
                path.SKPath.GetBounds(out SKRect pathBounds);
                Vector2 pivot = new(path.Anchor.x * pathBounds.Width + pathBounds.Left, path.Anchor.y * pathBounds.Height + pathBounds.Top);
                if (path.UseObjectAnchor) pivot = new(path.Anchor.x * Shape.LocalBounds.Width, path.Anchor.y * Shape.LocalBounds.Height);

                // Scale to fit content
                var viewBox = currentAnimVector.ViewBox;
                float scaleX = (float)Shape.LocalBounds.Width / viewBox.Width;
                float scaleY = (float)Shape.LocalBounds.Height / viewBox.Height;
                float scale = Math.Min(scaleX, scaleY);

                if (!path.UseObjectAnchor)
                {
                    canvas.Scale(scale * 2);
                    canvas.Translate(-viewBox.Left, -viewBox.Top);
                }

                canvas.RotateDegrees(path.Rotation, pivot.x, pivot.y);
                canvas.Scale(RMath.Clamp(path.Scale.x, 0, 99999), RMath.Clamp(path.Scale.y, 0, 99999), pivot.x, pivot.y);

                if (path.UseObjectSizeTranslation)
                    canvas.Translate(path.Translation.x * Shape.LocalBounds.Width, path.Translation.y * Shape.LocalBounds.Height);

                if (path.UseObjectAnchor)
                {
                    canvas.Scale(scale * 2);
                    canvas.Translate(-viewBox.Left, -viewBox.Top);
                }

                if (!path.UseObjectSizeTranslation)
                    canvas.Translate(path.Translation.x * pathBounds.Width, path.Translation.y * pathBounds.Height);

                // Draw path
                using var paint = GetRenderPaint();
                paint.Color = path.Fill;

                if (path.StrokeTrace != 1f)
                {
                    float totalLength = path.ApproximateLength();
                    float visibleLength = totalLength * path.StrokeTrace;

                    paint.PathEffect = SKPathEffect.CreateDash(new float[] { visibleLength, totalLength }, 0);
                }

                canvas.DrawPath(path.SKPath, paint);

                paint.IsStroke = true;
                paint.Color = path.Stroke;
                paint.StrokeWidth = path.StrokeWidth;
                paint.StrokeCap = currentAnimVector.LineCap;
                paint.StrokeJoin = currentAnimVector.LineJoin;

                canvas.DrawPath(path.SKPath, paint);

                canvas.RestoreToCount(layer);
                // canvas.RestoreToCount(save);
            }
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