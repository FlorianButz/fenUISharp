using FenUISharp.Components.Text.Model;
using FenUISharp.Mathematics;
using SkiaSharp;

namespace FenUISharp.Components.Text.Layout
{
    public class BlurLayoutProcessor : LayoutProcessor
    {
        private List<Glyph>? oldLayout;
        private List<SKPoint>? oldLayoutPositions;
        private List<Glyph>? newLayout;
        private List<SKPoint>? newLayoutPositions;

        private AnimatorComponent animatorOut;
        private AnimatorComponent animatorIn;

        public BlurLayoutProcessor(FText parent, TextLayout innerLayout) : base(parent, innerLayout)
        {
            parent.OnModelChanged += () =>
            {
                oldLayout = newLayout;

                oldLayoutPositions = new();
                oldLayout?.ForEach(x => oldLayoutPositions.Add(x.Position));

                newLayout = null;
                animatorOut?.Restart();
            };

            const float duration = 0.5f;

            animatorOut = new(parent, t => t);
            animatorOut.Duration = duration;

            animatorOut.onComplete += OnAnimCompleteOut;
            animatorOut.onValueUpdate += OnAnimValueUpdated;

            animatorIn = new(parent, t => t);
            animatorIn.Duration = duration;

            animatorIn.onComplete += OnAnimCompleteIn;
            animatorIn.onValueUpdate += OnAnimValueUpdated;
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
            Parent.MarkInvalidated();
        }


        public override List<Glyph> ProcessModel(TextModel model)
        {
            if (newLayout == null)
            {
                newLayout = base.ProcessModel(model);
             
                newLayoutPositions = new();
                newLayout?.ForEach(x => newLayoutPositions.Add(x.Position));   
            }

            Parent.MarkInvalidated();

            if (oldLayout != null)
            {
                float fadeLength = 0.3f;
                int maxCount = Math.Max(oldLayout.Count, newLayout.Count);
                float t = animatorIn.IsRunning 
                    ? animatorIn.Time 
                    : animatorOut.Time;
                
                float adjustedTime = t * (2.5f + fadeLength);

                float glyphOffset = 5;
                float blurRadius = 6;
                        var easing = Easing.EaseInCubic;

                for (int i = 0; i < maxCount; i++)
                {
                    float charPosition = maxCount > 1 
                        ? (float)i / (maxCount - 1) 
                        : 0;
                    float charProgress = (adjustedTime - charPosition) / fadeLength;
                    charProgress = RMath.Clamp(charProgress, 0f, 1f);

                    if (i < oldLayout.Count && animatorOut.IsRunning)
                    {
                        charProgress = 1 - charProgress;

                        var easedTime = easing(1 - charProgress);

                        if (oldLayoutPositions == null) continue;

                        oldLayout[i].Position = new(
                            oldLayoutPositions[i].X,
                            oldLayoutPositions[i].Y - (easedTime) * glyphOffset
                        );

                        float scale = RMath.Remap(charProgress, 0, 1, 0.75f, 1f);
                        oldLayout[i].Scale = new SKSize(scale, scale);

                        oldLayout[i].Style.BlurRadius = (1 - charProgress) * blurRadius;
                        oldLayout[i].Style.Opacity = RMath.Clamp(charProgress * 2, 0, 1);
                    }

                    if (i < newLayout.Count && animatorIn.IsRunning)
                    {
                        var easedTime = easing(1 - charProgress);
                        
                        if (newLayoutPositions == null) continue;

                        newLayout[i].Position = new(
                            newLayoutPositions[i].X,
                            newLayoutPositions[i].Y - (easedTime) * -glyphOffset
                        );

                        float scale = RMath.Remap(charProgress, 0, 1, 0.75f, 1f);
                        newLayout[i].Scale = new SKSize(scale, scale);

                        newLayout[i].Style.BlurRadius = (1 - charProgress) * blurRadius;
                        newLayout[i].Style.Opacity = RMath.Clamp(charProgress * 2, 0, 1);
                    }
                    else if (i < newLayout.Count && !animatorIn.IsRunning)
                    {

                        newLayout[i].Style.Opacity = 0;
                    }
                }

                return [.. oldLayout, .. newLayout];
            }
            else
                return newLayout;
        }
    }
}