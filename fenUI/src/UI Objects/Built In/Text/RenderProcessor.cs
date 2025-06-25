using FenUISharp.Objects.Text.Model;
using SkiaSharp;

namespace FenUISharp.Objects.Text.Rendering
{
    public class RenderProcessor : TextRenderer
    {
        public TextRenderer InnerRenderer { get; init; }

        public RenderProcessor(FText parent, TextRenderer innerRenderer) : base(parent)
        {
            InnerRenderer = innerRenderer;
        }

        public override void DrawText(SKCanvas canvas, TextModel model, List<Glyph> glyphs, SKPaint paint)
        {
            InnerRenderer.DrawText(canvas, model, glyphs, paint);
        }
    }
}