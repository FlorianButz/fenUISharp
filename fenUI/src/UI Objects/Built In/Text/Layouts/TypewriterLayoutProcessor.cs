using FenUISharp.Behavior;
using FenUISharp.Mathematics;
using FenUISharp.Objects.Text;
using FenUISharp.Objects.Text.Layout;
using FenUISharp.Objects.Text.Model;
using SkiaSharp;

namespace FenUISharp.Components.Text.Layout
{
    public class TypewriterLayoutProcessor : LayoutProcessor
    {
        private List<Glyph>? currentLayout;
        private List<TypewriterGlyphState>? glyphStates;
        private AnimatorComponent typewriterAnimator;
        private Random random = new Random();

        // Animation properties
        public float Duration { get; init; } = 2.0f;
        public float CharacterDelay { get; init; } = 0.08f; // Delay between each character
        public float MechanicalBounce { get; init; } = 3.0f; // How much characters bounce when they appear
        public float RotationVariance { get; init; } = 2.0f; // Slight rotation for mechanical feel
        public float ScaleImpact { get; init; } = 0.15f; // Scale bounce effect
        public float TypewriterSound { get; init; } = 0.5f; // Intensity of the "strike" effect
        public bool RandomizeTimings { get; init; } = true;

        public bool LowerQualityOnAnimate = true;

        private class TypewriterGlyphState
        {
            public float StartTime;
            public float BounceOffset;
            public float RotationOffset;
            public float ScaleMultiplier;
            public bool HasAppeared;
            public SKPoint OriginalPosition;
        }

        public TypewriterLayoutProcessor(FText parent, TextLayout innerLayout) : base(parent, innerLayout)
        {
            parent.OnModelChanged += OnModelChanged;

            typewriterAnimator = new(parent, t => t);
            typewriterAnimator.Duration = Duration;
            typewriterAnimator.OnValueUpdate += OnAnimValueUpdated;
            typewriterAnimator.OnComplete += OnAnimComplete;
        }

        private void OnModelChanged()
        {
            currentLayout = null;
            glyphStates = null;
            typewriterAnimator.Restart();

            if (LowerQualityOnAnimate)
                Owner.Quality.SetStaticState(0.9f);
        }

        private void OnAnimValueUpdated(float time)
        {
            Owner.Invalidate(Objects.UIObject.Invalidation.SurfaceDirty);
        }

        private void OnAnimComplete()
        {
            Owner.Quality.SetStaticState(1f);
        }

        public override List<Glyph> ProcessModel(TextModel model, SKRect bounds)
        {
            currentLayout = base.ProcessModel(model, bounds);

            if (currentLayout == null || currentLayout.Count == 0)
                return currentLayout ?? new List<Glyph>();

            // Initialize glyph states on first run
            if (glyphStates == null || glyphStates.Count != currentLayout.Count)
            {
                InitializeGlyphStates();
            }

            if (typewriterAnimator.IsRunning)
            {
                ProcessTypewriterAnimation();
            }

            return currentLayout;
        }

        private void InitializeGlyphStates()
        {
            if (currentLayout == null) return;

            glyphStates = new List<TypewriterGlyphState>();
            
            for (int i = 0; i < currentLayout.Count; i++)
            {
                var glyph = currentLayout[i];
                var state = new TypewriterGlyphState
                {
                    OriginalPosition = glyph.Position,
                    HasAppeared = false
                };

                // Calculate when this character should start appearing
                float baseDelay = i * CharacterDelay;
                if (RandomizeTimings)
                {
                    // Add some randomness to make it feel more organic
                    baseDelay += (float)(random.NextDouble() - 0.5) * CharacterDelay * 0.3f;
                }
                state.StartTime = Math.Max(0, baseDelay / Duration);

                state.BounceOffset = (float)(random.NextDouble() - 0.5) * MechanicalBounce * 0.5f;
                state.RotationOffset = (float)(random.NextDouble() - 0.5) * RotationVariance;
                state.ScaleMultiplier = 1.0f + (float)(random.NextDouble() - 0.5) * 0.1f;

                glyphStates.Add(state);
            }
        }

        private void ProcessTypewriterAnimation()
        {
            if (currentLayout == null || glyphStates == null) return;

            float currentTime = typewriterAnimator.Time;

            for (int i = 0; i < currentLayout.Count; i++)
            {
                var glyph = currentLayout[i];
                var state = glyphStates[i];

                if (currentTime >= state.StartTime)
                {
                    // Character should be visible
                    float charProgress = (currentTime - state.StartTime) / (CharacterDelay / Duration);
                    charProgress = RMath.Clamp(charProgress, 0f, 1f);

                    if (!state.HasAppeared)
                    {
                        state.HasAppeared = true;
                    }

                    // Apply typewriter effects
                    ApplyTypewriterEffects(glyph, state, charProgress);
                }
                else
                {
                    glyph.Style.Opacity = 0;
                }
            }
        }

        private void ApplyTypewriterEffects(Glyph glyph, TypewriterGlyphState state, float progress)
        {
            // Easing functions for different effects
            var bounceEasing = Easing.EaseOutBounce;
            var impactEasing = Easing.EaseOutBack;
            var fadeEasing = Easing.EaseOutQuart;

            // Opacity fade-in
            glyph.Style.Opacity = fadeEasing(progress);

            // Mechanical bounce effect
            float bounceProgress = bounceEasing(progress);
            float verticalOffset = (1 - bounceProgress) * (MechanicalBounce + state.BounceOffset);
            
            glyph.Position = new SKPoint(
                state.OriginalPosition.X,
                state.OriginalPosition.Y - verticalOffset
            );

            float scaleProgress = impactEasing(progress);
            float scaleEffect = 1.0f + (1 - scaleProgress) * ScaleImpact * state.ScaleMultiplier;
            glyph.Scale = new SKSize(scaleEffect, scaleEffect);

            if (progress < 0.3f)
            {
                float rotationProgress = progress / 0.3f;
                float rotation = (1 - Easing.EaseOutCubic(rotationProgress)) * state.RotationOffset;
                // Note: Rotation would need to be implemented in the rendering system
                // This is a placeholder for the concept
            }

            if (progress < 0.2f)
            {
                float blurProgress = progress / 0.2f;
                glyph.Style.BlurRadius = (1 - Easing.EaseOutQuad(blurProgress)) * TypewriterSound;
            }
            else
            {
                glyph.Style.BlurRadius = 0;
            }

            if (progress < 0.1f)
            {
                float colorProgress = progress / 0.1f;
                float brightness = 1.0f + (1 - Easing.EaseOutQuad(colorProgress)) * 0.3f;
                // Note: This would require color manipulation in the rendering system
                // This is conceptual for the typewriter "flash" effect
            }
        }
    }
}