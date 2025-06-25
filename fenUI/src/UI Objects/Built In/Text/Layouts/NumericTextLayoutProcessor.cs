
using FenUISharp.Behavior;
using FenUISharp.Mathematics;
using FenUISharp.Objects.Text;
using FenUISharp.Objects.Text.Layout;
using FenUISharp.Objects.Text.Model;
using SkiaSharp;

namespace FenUISharp.Components.Text.Layout
{
    public class NumericTextLayoutProcessor : LayoutProcessor
    {
        private List<Glyph>? oldLayout;
        private List<Glyph>? newLayout;
        private Dictionary<char, SKPoint> oldGlyphPositions;
        private Dictionary<char, int> oldGlyphCounts;
        private Dictionary<char, int> newGlyphCounts;

        private AnimatorComponent mainAnimator;
        private Dictionary<char, Spring> positionSprings;
        private Dictionary<char, Spring> scaleSpringsDeparting;
        private Dictionary<char, Spring> scaleStringArriving;

        // Animation parameters - customizable
        public float Duration { get; init; } = 1f;
        public float DurationLengthAddition { get; init; } = 0.1f;
        public float BlurRadius { get; init; } = 5f;
        public float VerticalOffset { get; init; } = -8f;
        public float SpringSpeed { get; init; } = 3f;
        public float SpringSpringiness { get; init; } = 2f;
        public float ScaleDownAmount { get; init; } = 0.6f;
        public float GlyphDelay { get; init; } = 0.085f;
        public Func<float, float> VerticalPositionEasing { get; init; } = Easing.CombineInOut(Easing.EaseInBackDramatic, Easing.EaseOutBackDramatic);
        public Func<float, float> SlidePositionEasing { get; init; } = Easing.EaseOutExpo;
        public Func<float, float> FadeEasing { get; init; } = Easing.EaseInCubic;
        public bool LowerQualityOnAnimate { get; init; } = false;

        private int totalGlyphs; // for computing total delay
        private float totalDelay;

        private enum GlyphTransitionType
        {
            Unchanged,
            Moving,
            Departing,
            Arriving
        }

        public NumericTextLayoutProcessor(FText parent, TextLayout innerLayout) : base(parent, innerLayout)
        {
            oldGlyphPositions = new Dictionary<char, SKPoint>();
            oldGlyphCounts = new Dictionary<char, int>();
            newGlyphCounts = new Dictionary<char, int>();
            positionSprings = new Dictionary<char, Spring>();
            scaleSpringsDeparting = new Dictionary<char, Spring>();
            scaleStringArriving = new Dictionary<char, Spring>();

            parent.OnModelChanged += OnModelChanged;

            mainAnimator = new(parent, t => t);
            mainAnimator.OnValueUpdate += OnAnimValueUpdated;
            mainAnimator.OnComplete += OnAnimComplete;
        }

        private void OnModelChanged()
        {
            if (newLayout != null)
            {
                float glyphAnimTime = 1.0f;

                int glyphCount = newLayout.Count;
                float requiredDuration = GlyphDelay * Math.Max(0, glyphCount - 1) + glyphAnimTime;
                mainAnimator.Duration = Math.Max(Duration, requiredDuration);

                oldLayout = new List<Glyph>(newLayout.Select(g => new Glyph(
                    g.Character, g.Position, g.Scale, g.Anchor, new Objects.Text.Model.TextStyle(g.Style), g.Size)));

                oldGlyphPositions.Clear();
                oldGlyphCounts.Clear();

                foreach (var glyph in oldLayout)
                {
                    oldGlyphPositions[glyph.Character] = glyph.Position;
                    oldGlyphCounts[glyph.Character] = oldGlyphCounts.GetValueOrDefault(glyph.Character, 0) + 1;
                }
            }

            newLayout = null;
            positionSprings.Clear();
            scaleSpringsDeparting.Clear();
            scaleStringArriving.Clear();

            mainAnimator.Restart();

            if (LowerQualityOnAnimate)
                Owner.Quality.SetStaticState(0.85f);
        }

        private void OnAnimValueUpdated(float time)
        {
            Owner.Invalidate(Objects.UIObject.Invalidation.SurfaceDirty);
        }

        private void OnAnimComplete()
        {
            oldLayout = null;
            Owner.Quality.SetStaticState(1f);
        }

        private float GetGlyphTime(float animationTime, int index)
        {
            if (totalGlyphs <= 1 || GlyphDelay <= 0)
                return animationTime;

            float start = index * GlyphDelay;
            // Ensure the last glyph starts at or before the end
            float maxStart = (totalGlyphs - 1) * GlyphDelay;
            if (maxStart > 1f)
            {
                // Scale down GlyphDelay so last glyph starts at 1
                float scale = 1f / maxStart;
                start = index * GlyphDelay * scale;
            }
            float t = (animationTime - start) / (1f - start);
            return RMath.Clamp(t, 0f, 1f);
        }

        private GlyphTransitionType GetTransitionType(Glyph oldGlyph, Glyph newGlyph, int oldIndex, int newIndex)
        {
            if (oldGlyph == null && newGlyph != null)
                return GlyphTransitionType.Arriving;
            if (oldGlyph != null && newGlyph == null)
                return GlyphTransitionType.Departing;
            if (oldGlyph != null && newGlyph != null)
            {
                if (oldGlyph.Character == newGlyph.Character && oldIndex == newIndex)
                    return GlyphTransitionType.Unchanged;
                return GlyphTransitionType.Moving;
            }
            return GlyphTransitionType.Unchanged;
        }

        public override List<Glyph> ProcessModel(TextModel model, SKRect bounds)
        {
            newLayout = base.ProcessModel(model, bounds);

            // Setup delay metrics
            totalGlyphs = newLayout.Count;
            totalDelay = GlyphDelay * (totalGlyphs);

            if (oldLayout == null)
                return newLayout;

            newGlyphCounts.Clear();
            foreach (var glyph in newLayout)
                newGlyphCounts[glyph.Character] = newGlyphCounts.GetValueOrDefault(glyph.Character, 0) + 1;

            float rawTime = Easing.EaseOutCubic(mainAnimator.Time);

            var processedGlyphs = new List<Glyph>();
            var oldGlyphsUsed = new HashSet<int>();

            for (int i = 0; i < oldLayout.Count; i++)
            {
                var oldGlyph = oldLayout[i];
                Glyph? matchingNewGlyph = null;
                int matchingNewIndex = -1;

                // Try to find a matching new glyph
                for (int j = 0; j < newLayout.Count; j++)
                {
                    if (newLayout[j].Character == oldGlyph.Character && !oldGlyphsUsed.Contains(j))
                    {
                        matchingNewGlyph = newLayout[j];
                        matchingNewIndex = j;
                        oldGlyphsUsed.Add(j);
                        break;
                    }
                }

                // If no match, treat as departing
                var type = GetTransitionType(oldGlyph, matchingNewGlyph, i, matchingNewIndex);
                float t = GetGlyphTime(rawTime, i);
                var glyph = ProcessGlyph(oldGlyph, matchingNewGlyph, type, t, i);
                if (glyph != null)
                    processedGlyphs.Add(glyph);
            }

            for (int i = 0; i < newLayout.Count; i++)
            {
                if (oldGlyphsUsed.Contains(i)) continue;
                var newGlyph = newLayout[i];
                float t = GetGlyphTime(rawTime, i);
                var glyph = ProcessGlyph(null, newGlyph, GlyphTransitionType.Arriving, t, i);
                if (glyph != null)
                    processedGlyphs.Add(glyph);
            }

            return processedGlyphs;
        }

        private Glyph? ProcessGlyph(Glyph? oldGlyph, Glyph? newGlyph, GlyphTransitionType type, float t, int index)
        {
            switch (type)
            {
                case GlyphTransitionType.Unchanged:
                    return newGlyph;

                case GlyphTransitionType.Moving:
                    if (oldGlyph == null || newGlyph == null) return newGlyph;

                    Vector2 start = new Vector2(oldGlyph.Position.X, oldGlyph.Position.Y);
                    Vector2 end = new Vector2(newGlyph.Position.X, newGlyph.Position.Y);

                    var pos = Vector2.Lerp(start, end, SlidePositionEasing(t));

                    return new Glyph(newGlyph.Character, new SKPoint(pos.x, pos.y), newGlyph.Scale, newGlyph.Anchor, new TextStyle(newGlyph.Style), newGlyph.Size);

                case GlyphTransitionType.Departing:
                    if (oldGlyph == null) return null;

                    float ease = VerticalPositionEasing(t);
                    float fade = FadeEasing(t);

                    var dp = new SKPoint(oldGlyph.Position.X, oldGlyph.Position.Y - ease * VerticalOffset);
                    float sd = RMath.Lerp(1f, ScaleDownAmount, fade);

                    var dg = new Glyph(oldGlyph.Character, dp, new SKSize(sd, sd), oldGlyph.Anchor, new TextStyle(oldGlyph.Style), oldGlyph.Size)
                    {
                        Style = { BlurRadius = fade * BlurRadius, Opacity = 1f - fade }
                    };

                    return dg;

                case GlyphTransitionType.Arriving:
                    if (newGlyph == null) return null;

                    float revEase = VerticalPositionEasing(1f - t);
                    float revFade = FadeEasing(1f - t);

                    var ap = new SKPoint(newGlyph.Position.X, newGlyph.Position.Y + revEase * VerticalOffset);
                    float sa = RMath.Lerp(1f, ScaleDownAmount, revFade);

                    var ag = new Glyph(newGlyph.Character, ap, new SKSize(sa, sa), newGlyph.Anchor, new TextStyle(newGlyph.Style), newGlyph.Size)
                    {
                        Style = { BlurRadius = revFade * BlurRadius, Opacity = 1f - revFade }
                    };

                    return ag;

                default:
                    return newGlyph;
            }
        }
    }
}