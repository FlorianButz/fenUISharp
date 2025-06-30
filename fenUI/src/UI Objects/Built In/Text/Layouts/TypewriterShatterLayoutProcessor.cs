using FenUISharp.Behavior;
using FenUISharp.Mathematics;
using FenUISharp.Objects.Text;
using FenUISharp.Objects.Text.Layout;
using FenUISharp.Objects.Text.Model;
using SkiaSharp;

namespace FenUISharp.Components.Text.Layout
{
    public class TypewriterShatterLayoutProcessor : LayoutProcessor
    {
        private List<Glyph>? oldLayout;
        private List<ShatterFragment>? shatterFragments;
        private List<Glyph>? newLayout;
        private Random random = new();

        private AnimatorComponent shatterAnimator;
        private AnimatorComponent typewriterAnimator;

        public float ShatterDuration { get; init; } = 0.8f;
        public float TypewriterDuration { get; init; } = 0.6f;
        public float TypewriterDelay { get; init; } = 0.2f;
        public int ShatterFragmentCount { get; init; } = 3;
        public float ShatterVelocityMultiplier { get; init; } = 150f;
        public float ShatterRotationSpeed { get; init; } = 720f;
        public float TypewriterCharDelay { get; init; } = 0.05f;
        public bool LowerQualityOnAnimate { get; init; } = true;

        private struct ShatterFragment
        {
            public char Character;
            public SKPoint InitialPosition;
            public SKPoint Velocity;
            public float RotationVelocity;
            public float InitialRotation;
            public TextStyle Style;
            public SKSize Size;
            public SKPoint Anchor;
            public SKSize Scale;
        }

        public TypewriterShatterLayoutProcessor(FText parent, TextLayout innerLayout) : base(parent, innerLayout)
        {
            parent.OnModelChanged += () =>
            {
                oldLayout = newLayout;
                newLayout = null;
                CreateShatterFragments();
                
                shatterAnimator?.Restart();
                typewriterAnimator?.Break();

                if (LowerQualityOnAnimate)
                    Owner.Quality.SetStaticState(0.8f);
            };

            shatterAnimator = new(parent, t => t);
            shatterAnimator.Duration = ShatterDuration;
            shatterAnimator.OnComplete += OnShatterComplete;
            shatterAnimator.OnValueUpdate += OnAnimValueUpdated;

            typewriterAnimator = new(parent, t => t);
            typewriterAnimator.Duration = TypewriterDuration;
            typewriterAnimator.OnComplete += OnTypewriterComplete;
            typewriterAnimator.OnValueUpdate += OnAnimValueUpdated;
        }

        private void CreateShatterFragments()
        {
            if (oldLayout == null) return;

            shatterFragments = new();
            
            foreach (var glyph in oldLayout)
            {
                for (int i = 0; i < ShatterFragmentCount; i++)
                {
                    // Create multiple fragments per character for more dramatic effect
                    var fragment = new ShatterFragment
                    {
                        Character = glyph.Character,
                        InitialPosition = glyph.Position,
                        Velocity = new SKPoint(
                            (float)(random.NextDouble() - 0.5) * ShatterVelocityMultiplier,
                            (float)(random.NextDouble() - 0.8) * ShatterVelocityMultiplier
                        ),
                        RotationVelocity = (float)(random.NextDouble() - 0.5) * ShatterRotationSpeed,
                        InitialRotation = (float)random.NextDouble() * 360f,
                        Style = new TextStyle(glyph.Style),
                        Size = glyph.Size,
                        Anchor = glyph.Anchor,
                        Scale = glyph.Scale
                    };
                    
                    shatterFragments.Add(fragment);
                }
            }
        }

        private void OnShatterComplete()
        {
            // Start typewriter effect after a short delay
            // Parent.WindowRoot.Dispatcher.InvokeLater(() => typewriterAnimator.Restart(), TypewriterDelay);
            typewriterAnimator.Restart();
        }

        private void OnTypewriterComplete()
        {
            oldLayout = null;
            shatterFragments = null;
            Owner.Quality.SetStaticState(1f);
        }

        private void OnAnimValueUpdated(float time)
        {
            Owner.Invalidate(Objects.UIObject.Invalidation.SurfaceDirty);
        }

        public override List<Glyph> ProcessModel(TextModel model, SKRect bounds)
        {
            newLayout = base.ProcessModel(model, bounds);
            Owner.Invalidate(Objects.UIObject.Invalidation.SurfaceDirty);

            var result = new List<Glyph>();

            // Add shatter fragments if they exist
            if (shatterFragments != null && shatterAnimator.IsRunning)
            {
                float t = shatterAnimator.Time;
                float easedTime = Easing.EaseInQuart(t);
                
                foreach (var fragment in shatterFragments)
                {
                    var glyph = new Glyph(
                        fragment.Character,
                        new SKPoint(
                            fragment.InitialPosition.X + fragment.Velocity.X * easedTime,
                            fragment.InitialPosition.Y + fragment.Velocity.Y * easedTime + 0.5f * 300f * easedTime * easedTime // Gravity
                        ),
                        fragment.Scale,
                        fragment.Anchor,
                        new TextStyle(fragment.Style),
                        fragment.Size
                    );

                    // Apply rotation (would need to be handled in rendering)
                    // This is a conceptual rotation value that the renderer would use
                    
                    // Fade out and scale down fragments
                    glyph.Style.Opacity = RMath.Clamp((1 - easedTime) * 2, 0, 1);
                    glyph.Scale = new SKSize(
                        fragment.Scale.Width * (1 - easedTime * 0.5f),
                        fragment.Scale.Height * (1 - easedTime * 0.5f)
                    );

                    result.Add(glyph);
                }
            }

            // Add typewriter effect for new text
            if (newLayout != null && typewriterAnimator.IsRunning)
            {
                float t = typewriterAnimator.Time;
                
                for (int i = 0; i < newLayout.Count; i++)
                {
                    var glyph = newLayout[i];
                    
                    // Calculate when this character should appear
                    float charStartTime = i * TypewriterCharDelay;
                    float charEndTime = charStartTime + TypewriterCharDelay * 2;
                    
                    if (t >= charStartTime)
                    {
                        var newGlyph = new Glyph(
                            glyph.Character,
                            glyph.Position,
                            glyph.Scale,
                            glyph.Anchor,
                            new TextStyle(glyph.Style),
                            glyph.Size
                        );

                        if (t < charEndTime)
                        {
                            // Character is appearing - typewriter effect
                            float charProgress = (t - charStartTime) / (charEndTime - charStartTime);
                            charProgress = RMath.Clamp(charProgress, 0, 1);
                            
                            // Typewriter "pop" effect
                            float popProgress = Easing.EaseOutElastic(charProgress);
                            newGlyph.Scale = new SKSize(
                                glyph.Scale.Width * (0.3f + 0.7f * popProgress),
                                glyph.Scale.Height * (0.3f + 0.7f * popProgress)
                            );
                            
                            // Slight blur during appearance
                            newGlyph.Style.BlurRadius = (1 - charProgress) * 2f;
                            
                            // Opacity fade in
                            newGlyph.Style.Opacity = Easing.EaseOutCubic(charProgress);
                            
                            // Slight vertical offset during appearance
                            newGlyph.Position = new SKPoint(
                                glyph.Position.X,
                                glyph.Position.Y - (1 - charProgress) * 3f
                            );
                        }

                        result.Add(newGlyph);
                    }
                }
            }
            else if (newLayout != null && !typewriterAnimator.IsRunning && !shatterAnimator.IsRunning)
            {
                // No animation, return normal layout
                result.AddRange(newLayout);
            }

            return result;
        }
    }
}