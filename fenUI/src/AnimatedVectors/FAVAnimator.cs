using FenUISharp.Behavior;
using FenUISharp.Mathematics;

namespace FenUISharp.AnimatedVectors
{
    internal class FAVAnimator
    {
        static Dictionary<string, float> DefaultAttributeValues = new()
        {
            ["anchor-x"] = 0.5f,
            ["anchor-y"] = 0.5f,
            ["scale-x"] = 1f,
            ["scale-y"] = 1f,
            ["translate-x"] = 0f,
            ["translate-y"] = 0f,
            ["rotate"] = 0f,
            ["opacity"] = 1f,
            ["blur-radius"] = 0f,
            ["stroke-trace"] = 1f
        };

        private List<(int, AnimatorComponent anim, Action<bool> resetVals)> animators = new();

        public void StopAnimation()
        {
            animators.ToList().ForEach(x => { x.resetVals(true); x.anim.Break(); x.anim.Dispose(); });
            animators = new();
        }

        public void PlayAnimation(FAVDisplay display, AnimatedVector animatedVector, string id, Action? onComplete = null)
        {
            int activeAnimators = 0;
            foreach (var animationTuple in animatedVector.Animations)
            {
                if (animationTuple.id != id) continue;
                AVAnimation animation = animationTuple.animation;

                AnimatorComponent animComponent = new(display, (x) => x); // Easing is applied manually later
                animComponent.Duration = animation.Duration + animation.ExtendDuration;

                animComponent.OnValueUpdate += (x) =>
                {
                    // Get the animation time without the extended duration
                    var animationTime = () =>
                    {
                        if (animComponent.Duration - animation.ExtendDuration <= 0f) return 1f;
                        float t = Math.Clamp(animComponent.TimePassed / (animComponent.Duration - animation.ExtendDuration), 0f, 1f);
                        return t;
                    };

                    x = animation.PerKeyframeEase ? animationTime() : animation.Easing(animationTime());
                    var interpolationNeighbors = FindInterpolationNeighbors(animation.Keyframes, x);
                    float interpolationTime = RMath.Remap(x, interpolationNeighbors.from?.time ?? 0, interpolationNeighbors.to?.time ?? 0, 0, 1);

                    var fromAttrs = interpolationNeighbors.from?.attributes;
                    var toAttrs = interpolationNeighbors.to?.attributes;
                    float t = animation.PerKeyframeEase ? animation.Easing(interpolationTime) : interpolationTime;

                    float anchorX = InterpolateAttribute(fromAttrs, toAttrs, "anchor-x", t);
                    float anchorY = InterpolateAttribute(fromAttrs, toAttrs, "anchor-y", t);
                    float scaleX = InterpolateAttribute(fromAttrs, toAttrs, "scale-x", t);
                    float scaleY = InterpolateAttribute(fromAttrs, toAttrs, "scale-y", t);
                    float translateX = InterpolateAttribute(fromAttrs, toAttrs, "translate-x", t);
                    float translateY = InterpolateAttribute(fromAttrs, toAttrs, "translate-y", t);
                    float rotation = InterpolateAttribute(fromAttrs, toAttrs, "rotate", t);
                    float opacity = RMath.Clamp(InterpolateAttribute(fromAttrs, toAttrs, "opacity", t), 0, 1);
                    float blurRadius = MathF.Abs(InterpolateAttribute(fromAttrs, toAttrs, "blur-radius", t));
                    float strokeTrace = RMath.Clamp(InterpolateAttribute(fromAttrs, toAttrs, "stroke-trace", t), 0, 1);

                    for (int i = 0; i < animatedVector.Paths.Count; i++)
                    {
                        if (!animation.AffectedPathIDs.Contains(i)) continue;
                        var path = animatedVector.Paths[i];

                        if (anchorX != DefaultAttributeValues["anchor-x"]) path.Anchor.x = anchorX;
                        if (anchorY != DefaultAttributeValues["anchor-y"]) path.Anchor.y = anchorY;
                        if (scaleX != DefaultAttributeValues["scale-x"]) path.Scale.x = scaleX;
                        if (scaleY != DefaultAttributeValues["scale-y"]) path.Scale.y = scaleY;
                        if (translateX != DefaultAttributeValues["translate-x"]) path.Translation.x = translateX;
                        if (translateY != DefaultAttributeValues["translate-y"]) path.Translation.y = translateY;
                        if (rotation != DefaultAttributeValues["rotate"]) path.Rotation = rotation;
                        if (opacity != DefaultAttributeValues["opacity"]) path.Opacity = opacity;
                        if (blurRadius != DefaultAttributeValues["blur-radius"]) path.BlurRadius = blurRadius;
                        if (strokeTrace != DefaultAttributeValues["stroke-trace"]) path.StrokeTrace = strokeTrace;

                        if (animation.UseObjectAnchor) path.UseObjectAnchor = animation.UseObjectAnchor;
                        if (animation.UseObjectSizeTranslation) path.UseObjectSizeTranslation = animation.UseObjectSizeTranslation;
                    }

                    display.Invalidate(Objects.UIObject.Invalidation.SurfaceDirty);
                };

                var resetValues = (bool isEarlyEnding) =>
                {
                    if (!animation.DontResetValues)
                    {
                        for (int i = 0; i < animatedVector.Paths.Count; i++)
                        {
                            if (!animation.AffectedPathIDs.Contains(i)) continue;
                            var path = animatedVector.Paths[i];

                            path.Anchor = new(DefaultAttributeValues["anchor-x"], DefaultAttributeValues["anchor-y"]);
                            path.Translation = new(DefaultAttributeValues["translate-x"], DefaultAttributeValues["translate-y"]);
                            path.Scale = new(DefaultAttributeValues["scale-x"], DefaultAttributeValues["scale-y"]);
                            path.Rotation = DefaultAttributeValues["rotate"];

                            path.Opacity = DefaultAttributeValues["opacity"];
                            path.BlurRadius = DefaultAttributeValues["blur-radius"];
                            path.StrokeTrace = DefaultAttributeValues["stroke-trace"];
                        }
                    }

                    if(!isEarlyEnding || animation.Easing(1f) > 0.95f) // Check if spring is 1, reset if needed
                        animation.RecreateEasing?.Invoke();
                };

                animComponent.OnComplete += () =>
                {
                    activeAnimators--;
                    if (activeAnimators <= 0)
                    {
                        animators = new();
                        onComplete?.Invoke();
                    }

                    resetValues(false);
                    animComponent.Dispose();
                };

                animComponent.Start();
                animators.Add((activeAnimators, animComponent, resetValues));
                activeAnimators++;
            }
        }

        private float GetDefault(string id) => DefaultAttributeValues.TryGetValue(id, out var value) ? value : 0f;

        private float InterpolateAttribute(List<(string id, object value)>? fromAttrs, List<(string id, object value)>? toAttrs, string id, float t)
            => RMath.Lerp(
                fromAttrs?.LastOrDefault(a => a.id == id).value as float? ?? GetDefault(id),
                toAttrs?.LastOrDefault(a => a.id == id).value as float? ?? GetDefault(id),
                t
            );


        private (AVKeyframe? from, AVKeyframe? to) FindInterpolationNeighbors(List<AVKeyframe> keyframes, float currentTime)
        {
            float fromBiggestTime = float.NegativeInfinity;
            float toLowestTime = float.PositiveInfinity;

            AVKeyframe? lastFromKeyframe = null;
            AVKeyframe? fromKeyframe = null;
            AVKeyframe? toKeyframe = null;

            // Find the current two closest keyframes which currentTime is inbetween
            foreach (var keyframe in keyframes)
            {
                if (keyframe.time <= currentTime && keyframe.time >= fromBiggestTime)
                {
                    lastFromKeyframe = fromKeyframe;
                    fromBiggestTime = keyframe.time;
                    fromKeyframe = keyframe;
                }
                else if (keyframe.time > currentTime && keyframe.time < toLowestTime)
                {
                    toLowestTime = keyframe.time;
                    toKeyframe = keyframe;
                }
            }

            // If the currentTime is past the last keyframe, treat as if the time was still in between the last two keyframes to allow for extrapolation
            if (toKeyframe == null && fromKeyframe != null && keyframes.IndexOf(fromKeyframe) == (keyframes.Count - 1))
            {
                toKeyframe = fromKeyframe;
                fromKeyframe = lastFromKeyframe;
            }

            return (fromKeyframe, toKeyframe);
        }
    }
}