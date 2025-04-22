using FenUISharp.Components.Text.Model;
using FenUISharp.Mathematics;
using SkiaSharp;

namespace FenUISharp.Components.Text.Layout
{
    public class WiggleCharsLayoutProcessor : LayoutProcessor
    {
        private bool needsFullRebuild = true;
        private List<Glyph>? cachedLayout;

        public Vector2 Strength { get; set; } = new Vector2(1, 1);
        public Vector2 PeriodMultiplier { get; set; } = new Vector2(1.15f, 2.85f);
        public float Speed { get; set; } = 0.15f;

        public WiggleCharsLayoutProcessor(FText parent, TextLayout innerLayout) : base(parent, innerLayout)
        {
            parent.OnAnyChange += () => needsFullRebuild = true;
        }

        float time = 0;

        public override List<Glyph> ProcessModel(TextModel model, SKRect bounds)
        {
            time += Speed;

            if (needsFullRebuild || cachedLayout == null)
            {
                needsFullRebuild = false;
                cachedLayout = base.ProcessModel(model, bounds);
            }

            var offsetLayout = new List<Glyph>(cachedLayout);

            for (int i = 0; i < offsetLayout.Count; i++)
            {
                // offsetLayout[i].Position = new SkiaSharp.SKPoint(offsetLayout[i].Position.X + (float)Math.Sin(time + (float)i * 1.238f + 0.5f) * 4, offsetLayout[i].Position.Y + (float)Math.Sin(time + (float)i * 2.421f) * 4);
                offsetLayout[i].Position = new SkiaSharp.SKPoint(
                    offsetLayout[i].Position.X + (float)Math.Sin(time + (float)i * PeriodMultiplier.x) * Strength.x,
                    offsetLayout[i].Position.Y + (float)Math.Sin(time + (float)i * PeriodMultiplier.y) * Strength.y);
                needsFullRebuild = true;
            }

            Parent.MarkInvalidated();
            return offsetLayout;
        }
    }
}