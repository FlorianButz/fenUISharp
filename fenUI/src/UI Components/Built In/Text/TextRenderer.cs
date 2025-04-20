using FenUISharp.Components.Text.Model;
using SkiaSharp;

namespace FenUISharp.Components.Text.Rendering
{
    public class TextRenderer
    {
        protected FText Parent { get; init; }

        public TextRenderer(FText parent)
        {
            Parent = parent;
        }

        public virtual void DrawText(SKCanvas canvas, TextModel model, List<Glyph> glyphs, SKPaint paint)
        {
            for (int i = 0; i < glyphs.Count; i++)
            {
                int save = canvas.Save();
                canvas.Translate(0.5f, 0.5f);

                var glyph = glyphs[i];
                paint.Color = glyph.Style.Color;

                canvas.Scale(glyph.Scale.Width, glyph.Scale.Height, glyph.Position.X + glyph.Size.Width * glyph.Anchor.X, glyph.Position.Y + glyph.Size.Height * glyph.Anchor.Y);

                using (var font = CreateFont(model.Typeface, glyphs[i].Style))
                    canvas.DrawText(glyph.Character.ToString(), glyph.Position, SKTextAlign.Center, font, paint);

                if (glyph.Style.Underlined)
                    DrawUnderline(canvas, glyph, paint);

                canvas.RestoreToCount(save);
            }
        }

        public virtual void DrawUnderline(SKCanvas canvas, Glyph glyph, SKPaint paint)
        {
            int yOffset = 2;
            canvas.DrawLine(new(glyph.Bounds.Left, glyph.Bounds.Bottom + yOffset), new(glyph.Bounds.Right, glyph.Bounds.Bottom + yOffset), paint);            
        }

        public static SKFont CreateFont(FTypeface typeface, TextStyle style)
        {
            SKFont font = new SKFont(typeface.CreateSKTypeface(style.Weight, style.Slant), style.FontSize);
            font.Subpixel = true;
            font.ForceAutoHinting = true;
            font.Edging = SKFontEdging.SubpixelAntialias;
            
            return font;
        }
    }
}