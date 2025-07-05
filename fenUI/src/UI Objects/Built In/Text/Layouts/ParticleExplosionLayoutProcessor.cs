using FenUISharp.Behavior;
using FenUISharp.Objects.Text.Layout;
using FenUISharp.Mathematics;
using FenUISharp.Objects.Text.Model;
using SkiaSharp;

namespace FenUISharp.Objects.Text.Layout
{
    public class ParticleExplosionLayoutProcessor : LayoutProcessor
    {
        private List<Glyph>? oldLayout;
        private List<Glyph>? newLayout;
        private List<ParticleCluster>? oldParticles;
        private List<ParticleCluster>? newParticles;

        private AnimatorComponent animatorOut;
        private AnimatorComponent animatorIn;
        private Random random = new Random();

        public float Duration { get; init; } = 1.2f;
        public int ParticlesPerGlyph { get; init; } = 12;
        public float BlurMultiplier { get; init; } = 1f;
        public float ExplosionRadius { get; init; } = 80f;
        public float ParticleStartSize { get; init; } = 1f;
        public float ParticleEndSize { get; init; } = 4f;
        public float RotationIntensity { get; init; } = 720f; // degrees
        public float GravityStrength { get; init; } = 15f;
        public float VelocityRandomness { get; init; } = 0.6f;
        public float FormationDelay { get; init; } = 0.3f; // delay before new particles start forming
        public float PreDelay { get; init; } = 0f; // negative values make reformation start before explosion ends

        public bool LowerQualityOnAnimate = true;

        public ParticleExplosionLayoutProcessor(FText parent, TextLayout innerLayout) : base(parent, innerLayout)
        {
            parent.OnModelChanged += () =>
            {
                oldLayout = newLayout;
                
                // Convert old glyphs to particle clusters
                if (oldLayout != null)
                {
                    oldParticles = new();
                    foreach (var glyph in oldLayout)
                    {
                        oldParticles.Add(CreateParticleCluster(glyph, false));
                    }
                }

                newLayout = null;
                newParticles = null;
                
                animatorOut?.Restart();
                animatorIn?.Break();

                if (LowerQualityOnAnimate)
                    Owner.Quality.SetStaticState(0.75f);
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

        private ParticleCluster CreateParticleCluster(Glyph glyph, bool isFormation = false)
        {
            var particles = new List<Particle>();
            var center = glyph.Position;
            
            for (int i = 0; i < ParticlesPerGlyph; i++)
            {
                // Create particles in a rough glyph shape pattern
                float angle = (float)(random.NextDouble() * Math.PI * 2);
                float distance = (float)(random.NextDouble() * glyph.Size.Width * 0.4f);
                
                var localPos = new SKPoint(
                    (float)(Math.Cos(angle) * distance),
                    (float)(Math.Sin(angle) * distance * 0.6f) // Make it more horizontally oriented like text
                );

                SKPoint initialPosition;
                SKPoint velocity;

                if (isFormation)
                {
                    // For formation, start particles at explosion radius distance
                    float explosionDistance = ExplosionRadius * (0.7f + (float)random.NextDouble() * 0.6f);
                    var explosionPos = new SKPoint(
                        center.X + (float)(Math.Cos(angle) * explosionDistance),
                        center.Y + (float)(Math.Sin(angle) * explosionDistance * 0.6f) + GravityStrength * 0.8f
                    );
                    
                    initialPosition = explosionPos;
                    velocity = new SKPoint(0, 0); // No initial velocity for formation
                }
                else
                {
                    // For explosion, start at character center
                    initialPosition = new SKPoint(center.X + localPos.X, center.Y + localPos.Y);
                    velocity = new SKPoint(
                        localPos.X * (2f + (float)random.NextDouble() * VelocityRandomness),
                        localPos.Y * (1.5f + (float)random.NextDouble() * VelocityRandomness) - GravityStrength * 0.3f
                    );
                }

                particles.Add(new Particle
                {
                    InitialPosition = initialPosition,
                    Position = initialPosition,
                    Velocity = velocity,
                    Rotation = 0f,
                    RotationVelocity = (float)((random.NextDouble() - 0.5) * RotationIntensity),
                    Color = glyph.Style.Color,
                    Size = ParticleStartSize + (float)random.NextDouble() * (ParticleEndSize - ParticleStartSize) * 0.3f,
                    Life = 1f
                });
            }

            return new ParticleCluster
            {
                SourceGlyph = glyph,
                Particles = particles
            };
        }

        private void OnAnimCompleteOut()
        {
            // Only start the in animator if PreDelay is 0 or positive
            // For negative PreDelay, the formation already started during explosion
            if (PreDelay >= 0)
            {
                animatorIn.Restart();
            }
        }

        private void OnAnimCompleteIn()
        {
            oldLayout = null;
            oldParticles = null;
            newParticles = null;
        }

        private void OnAnimValueUpdated(float time)
        {
            Owner.Invalidate(UIObject.Invalidation.SurfaceDirty);
        }

        public override List<Glyph> ProcessModel(TextModel model, SKRect bounds)
        {
            newLayout = base.ProcessModel(model, bounds);
            Owner.Invalidate(UIObject.Invalidation.SurfaceDirty);

            var result = new List<Glyph>();

            // Handle explosion phase (old text breaking apart)
            if (oldParticles != null && animatorOut.IsRunning)
            {
                float t = animatorOut.Time;
                var easingOut = Easing.EaseInQuart;
                
                foreach (var cluster in oldParticles)
                {
                    var particleGlyphs = UpdateExplosionParticles(cluster, t, easingOut);
                    result.AddRange(particleGlyphs);
                }
            }

            // Handle formation phase (new text forming from particles)
            // Check if we should start formation early due to PreDelay
            bool shouldStartFormation = false;
            float formationTime = 0f;
            
            if (PreDelay < 0 && animatorOut.IsRunning)
            {
                // Start formation during explosion phase
                float explosionProgress = animatorOut.Time;
                float overlapStart = 1f + PreDelay; // PreDelay is negative, so this is < 1
                
                if (explosionProgress >= overlapStart)
                {
                    shouldStartFormation = true;
                    formationTime = (explosionProgress - overlapStart) / (1f - overlapStart);
                    
                    // Create new particles if they don't exist yet
                    if (newParticles == null && newLayout != null)
                    {
                        newParticles = new();
                        foreach (var glyph in newLayout)
                        {
                            newParticles.Add(CreateParticleCluster(glyph, true));
                        }
                    }
                }
            }
            else if (PreDelay >= 0 && animatorIn.IsRunning)
            {
                shouldStartFormation = true;
                formationTime = animatorIn.Time;
                
                // Create new particles if they don't exist yet
                if (newParticles == null && newLayout != null)
                {
                    newParticles = new();
                    foreach (var glyph in newLayout)
                    {
                        newParticles.Add(CreateParticleCluster(glyph, true));
                    }
                }
            }

            if (newParticles != null && shouldStartFormation)
            {
                float delayedT = Math.Max(0, (formationTime - FormationDelay) / (1 - FormationDelay));
                var easingIn = Easing.EaseOutQuart;
                
                foreach (var cluster in newParticles)
                {
                    var particleGlyphs = UpdateFormationParticles(cluster, delayedT, easingIn);
                    result.AddRange(particleGlyphs);
                }
            }

            // Return final result or normal layout
            if (result.Count > 0)
                return result;
            else
                return newLayout ?? base.ProcessModel(model, bounds);
        }

        private List<Glyph> UpdateExplosionParticles(ParticleCluster cluster, float t, Func<float, float> easing)
        {
            var result = new List<Glyph>();
            float easedT = easing(t);
            
            foreach (var particle in cluster.Particles)
            {
                // Update particle physics
                particle.Position = new SKPoint(
                    particle.InitialPosition.X + particle.Velocity.X * easedT * (ExplosionRadius / 50f),
                    particle.InitialPosition.Y + particle.Velocity.Y * easedT * (ExplosionRadius / 50f) + GravityStrength * easedT * easedT
                );
                
                particle.Rotation += particle.RotationVelocity * (1f / 60f); // Assuming 60fps
                particle.Life = 1f - easedT;

                // Animate particle size from start to end during explosion
                float currentSize = RMath.Lerp(ParticleStartSize, ParticleEndSize, easedT);

                // Create glyph for this particle
                var particleGlyph = new Glyph(
                    '•', // Bullet character as particle
                    particle.Position,
                    new SKSize(currentSize / 16f, currentSize / 16f), // Scale down
                    new SKPoint(0.5f, 0.5f), // Center anchor
                    new TextStyle(cluster.SourceGlyph.Style)
                    {
                        FontSize = currentSize,
                        Opacity = particle.Life * 0.8f,
                        BlurRadius = ((1f - particle.Life) * 2f) * BlurMultiplier
                    },
                    new SKSize(currentSize, currentSize)
                );

                result.Add(particleGlyph);
            }

            return result;
        }

        private List<Glyph> UpdateFormationParticles(ParticleCluster cluster, float t, Func<float, float> easing)
        {
            var result = new List<Glyph>();
            
            if (t <= 0) return result; // Not started yet due to delay
            
            float easedT = easing(Math.Min(t, 1f));
            var targetPos = new SKPoint(cluster.SourceGlyph.Position.X, cluster.SourceGlyph.Position.Y - cluster.SourceGlyph.Size.Height / 6);
            
            foreach (var particle in cluster.Particles)
            {
                // Particles converge to form the character
                particle.Position = new SKPoint(
                    RMath.Lerp(particle.InitialPosition.X, targetPos.X, easedT),
                    RMath.Lerp(particle.InitialPosition.Y, targetPos.Y, easedT)
                );
                
                particle.Life = easedT;
                // Animate particle size from end back to start during formation
                float currentSize = RMath.Lerp(ParticleEndSize, ParticleStartSize, easedT);

                // Create glyph for this particle
                var particleGlyph = new Glyph(
                    '•',
                    particle.Position,
                    new SKSize(currentSize / 16f, currentSize / 16f),
                    new SKPoint(0.5f, 0.5f),
                    new TextStyle(cluster.SourceGlyph.Style)
                    {
                        FontSize = currentSize,
                        Opacity = particle.Life * 0.9f,
                        BlurRadius = ((1f - particle.Life) * 2f + 1.5f) * BlurMultiplier
                    },
                    new SKSize(currentSize, currentSize)
                );

                result.Add(particleGlyph);
            }

            // Add the forming character with increasing opacity
            if (easedT > 0.7f)
            {
                var formingChar = new Glyph(
                    cluster.SourceGlyph.Character,
                    cluster.SourceGlyph.Position,
                    cluster.SourceGlyph.Scale,
                    cluster.SourceGlyph.Anchor,
                    new TextStyle(cluster.SourceGlyph.Style)
                    {
                        Opacity = RMath.Clamp((easedT - 0.7f) / 0.3f, 0f, 1f),
                        BlurRadius = ((1f - easedT) * 4f) * BlurMultiplier
                    },
                    cluster.SourceGlyph.Size
                );
                
                result.Add(formingChar);
            }

            return result;
        }

        private class Particle
        {
            public SKPoint InitialPosition { get; set; }
            public SKPoint Position { get; set; }
            public SKPoint Velocity { get; set; }
            public float Rotation { get; set; }
            public float RotationVelocity { get; set; }
            public Func<SKColor> Color { get; set; }
            public float Size { get; set; }
            public float Life { get; set; } // 0 to 1
        }

        private class ParticleCluster
        {
            public Glyph SourceGlyph { get; set; }
            public List<Particle> Particles { get; set; }
        }
    }
}