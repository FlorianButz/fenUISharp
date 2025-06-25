using FenUISharp.Behavior;
using FenUISharp.Components.Text.Layout;
using FenUISharp.Mathematics;
using FenUISharp.Objects.Text;
using FenUISharp.Objects.Text.Model;
using SkiaSharp;

namespace FenUISharp.Objects.Text.Layout
{
    public class BlurLayoutProcessor : LayoutProcessor
    {
        private List<Glyph>? oldLayout;
        private List<SKPoint>? oldLayoutPositions;
        private List<Glyph>? newLayout;

        private AnimatorComponent animatorOut;
        private AnimatorComponent animatorIn;

        public float Duration { get; init; } = 0.7f;
        public float BlurRadius { get; init; } = 4f;
        public float GlyphOffset { get; init; } = 5;
        public float FadeLength { get; init; } = 0.5f;

        public bool LowerQualityOnAnimate = true;

        public BlurLayoutProcessor(FText parent, TextLayout innerLayout) : base(parent, innerLayout)
        {
            parent.OnModelChanged += () =>
            {
                if (newLayout == null)
                    return;

                animatorIn?.Break();
                animatorOut?.Break();

                oldLayout = newLayout.ToList();
                oldLayoutPositions = new(newLayout.Count); // pre-alloc

                foreach (var x in newLayout)
                    oldLayoutPositions.Add(x.Position);

                newLayout = null;

                animatorOut?.Restart();
                animatorIn?.Break();

                if (LowerQualityOnAnimate)
                    Owner.Quality.SetStaticState(0.85f);
            };

            animatorOut = new(parent, t => t);
            animatorOut.Duration = Duration;

            animatorOut.OnComplete += OnAnimCompleteOut;
            animatorOut.OnValueUpdate += OnAnimValueUpdated;

            animatorIn = new(parent, t => t);
            animatorIn.Duration = Duration;

            animatorIn.OnComplete += OnAnimCompleteIn;
            animatorIn.OnValueUpdate += OnAnimValueUpdated;

            animatorIn.OnComplete += () => Owner.Quality.SetStaticState(1f);
        }

        private void OnAnimCompleteOut()
        {
            animatorIn.Restart();
        }

        private void OnAnimCompleteIn()
        {
            oldLayout = null;
        }

        private void OnAnimValueUpdated(float time)
        {
            Owner.Invalidate(UIObject.Invalidation.SurfaceDirty);
        }


        public override List<Glyph> ProcessModel(TextModel model, SKRect bounds)
        {
            newLayout = base.ProcessModel(model, bounds);
            Owner.Invalidate(UIObject.Invalidation.SurfaceDirty);

            if (oldLayout != null && newLayout != null)
            {
                float fadeLength = FadeLength;
                int maxCount = Math.Max(oldLayout.Count, newLayout.Count);
                float t = (animatorIn.IsRunning
                    ? animatorIn.Time
                    : animatorOut.Time);

                float adjustedTime = t * (1 + fadeLength);

                var easingIn = Easing.EaseOutQuint;
                var easingOut = Easing.EaseInQuint;

                for (int i = 0; i < maxCount; i++)
                {
                    float charPosition = maxCount > 1
                        ? (float)i / (maxCount - 1)
                        : 0;
                    float charProgress = (adjustedTime - charPosition) / fadeLength;
                    charProgress = RMath.Clamp(charProgress, 0f, 1f);
                    charProgress = animatorIn.IsRunning ? Easing.EaseInCubic(charProgress) : Easing.EaseOutCubic(charProgress);

                    if (i < oldLayout.Count && animatorOut.IsRunning)
                    {
                        charProgress = 1 - charProgress;

                        var easedTime = easingOut(1 - charProgress);
                        var easedTimeNonInversed = easingIn(charProgress);

                        if (oldLayoutPositions == null) continue;

                        oldLayout[i].Position = new(
                            oldLayoutPositions[i].X,
                            oldLayoutPositions[i].Y - (easedTime) * GlyphOffset
                        );

                        oldLayout[i].Style.BlurRadius = easedTime * BlurRadius;
                        oldLayout[i].Style.Opacity = RMath.Clamp(easedTimeNonInversed * 2, 0, 1);
                    }

                    if (i < newLayout.Count && newLayout[i] != null && animatorIn.IsRunning)
                    {
                        var easedTime = easingOut(1 - charProgress);
                        var easedTimeNonInversed = easingIn(charProgress);

                        newLayout[i].Position = new(
                            newLayout[i].Position.X,
                            newLayout[i].Position.Y - (easedTime) * -GlyphOffset
                        );

                        newLayout[i].Style.BlurRadius = easedTime * BlurRadius;
                        newLayout[i].Style.Opacity = RMath.Clamp(easedTimeNonInversed * 2, 0, 1);
                    }
                    else if (i < newLayout.Count && !animatorIn.IsRunning)
                        newLayout[i].Style.Opacity = 0;
                }

                return [.. oldLayout, .. newLayout];
            }
            else
                return newLayout ?? base.ProcessModel(model, bounds);
        }
    }
}