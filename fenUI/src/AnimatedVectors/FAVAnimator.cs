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

                // Make sure easing is isolated to single animation (affects e.g. springs)
                Func<float, float> AnimationEasing = animation.CreateEasing();

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

                    // Calculate neighbors and get t for inbetween
                    x = animation.PerKeyframeEase ? animationTime() : AnimationEasing(animationTime());
                    var interpolationNeighbors = FindInterpolationNeighbors(animation.Keyframes, x);
                    float interpolationTime = RMath.Remap(x, interpolationNeighbors.from?.time ?? 0, interpolationNeighbors.to?.time ?? 0, 0, 1);

                    // Calculate time value for interpolation
                    var fromAttrs = interpolationNeighbors.from?.attributes;
                    var toAttrs = interpolationNeighbors.to?.attributes;
                    float t = animation.PerKeyframeEase ? AnimationEasing(interpolationTime) : interpolationTime;

                    // Interpolate all values
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

                    // Apply to all affected paths
                    for (int i = 0; i < animatedVector.Paths.Count; i++)
                    {
                        if (!animation.AffectedPathIDs.Contains(i)) continue;
                        var path = animatedVector.Paths[i];

                        // Get override reference
                        display.GetOrCreatePathOverride(i, out AVPathAnimationOverride pathOverride);

                        // Apply overrides
                        if (anchorX != DefaultAttributeValues["anchor-x"]) pathOverride.Anchor.x = anchorX;
                        if (anchorY != DefaultAttributeValues["anchor-y"]) pathOverride.Anchor.y = anchorY;
                        if (scaleX != DefaultAttributeValues["scale-x"]) pathOverride.Scale.x = scaleX;
                        if (scaleY != DefaultAttributeValues["scale-y"]) pathOverride.Scale.y = scaleY;
                        if (translateX != DefaultAttributeValues["translate-x"]) pathOverride.Translation.x = translateX;
                        if (translateY != DefaultAttributeValues["translate-y"]) pathOverride.Translation.y = translateY;
                        if (rotation != DefaultAttributeValues["rotate"]) pathOverride.Rotation = rotation;
                        if (opacity != DefaultAttributeValues["opacity"]) pathOverride.Opacity = opacity;
                        if (blurRadius != DefaultAttributeValues["blur-radius"]) pathOverride.BlurRadius = blurRadius;
                        if (strokeTrace != DefaultAttributeValues["stroke-trace"]) pathOverride.StrokeTrace = strokeTrace;

                        if (animation.UseObjectAnchor) pathOverride.UseObjectAnchor = animation.UseObjectAnchor;
                        if (animation.UseObjectSizeTranslation) pathOverride.UseObjectSizeTranslation = animation.UseObjectSizeTranslation;
                    }

                    // Invalidate
                    display.Invalidate(Objects.UIObject.Invalidation.SurfaceDirty);
                };

                // Create reset values function
                var resetValues = (bool isEarlyEnding) =>
                {
                    if (!animation.DontResetValues)
                    {
                        for (int i = 0; i < animatedVector.Paths.Count; i++)
                        {
                            if (!animation.AffectedPathIDs.Contains(i)) continue;
                            var path = animatedVector.Paths[i];

                            // Get override reference
                            display.GetOrCreatePathOverride(i, out AVPathAnimationOverride pathOverride);

                            // Reset overrides
                            pathOverride.Anchor = new(DefaultAttributeValues["anchor-x"], DefaultAttributeValues["anchor-y"]);
                            pathOverride.Translation = new(DefaultAttributeValues["translate-x"], DefaultAttributeValues["translate-y"]);
                            pathOverride.Scale = new(DefaultAttributeValues["scale-x"], DefaultAttributeValues["scale-y"]);
                            pathOverride.Rotation = DefaultAttributeValues["rotate"];

                            pathOverride.Opacity = DefaultAttributeValues["opacity"];
                            pathOverride.BlurRadius = DefaultAttributeValues["blur-radius"];
                            pathOverride.StrokeTrace = DefaultAttributeValues["stroke-trace"];
                        }
                    }

                    if (!isEarlyEnding || AnimationEasing(1f) > 0.95f) // Check if spring is 1, reset if needed
                        AnimationEasing = animation.CreateEasing();
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

                // Begin animation
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