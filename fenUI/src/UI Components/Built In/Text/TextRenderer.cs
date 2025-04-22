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
                canvas.Translate(Parent.Transform.BoundsPadding.Value, Parent.Transform.BoundsPadding.Value);

                var fontPaint = paint.Clone();

                var glyph = glyphs[i];
                fontPaint.Color = glyph.Style.Color.Value;

                canvas.Scale(glyph.Scale.Width, glyph.Scale.Height, glyph.Position.X + glyph.Bounds.Width * glyph.Anchor.X / 2, glyph.Position.Y + -glyph.Bounds.Height * glyph.Anchor.Y / 2);

                using (var blur = SKImageFilter.CreateBlur(glyph.Style.BlurRadius, glyph.Style.BlurRadius))
                using (var font = CreateFont(model.Typeface, glyph.Style))
                {
                    if (glyph.Style.BlurRadius > 0) fontPaint.ImageFilter = blur;

                    if (glyph.Style.Opacity < 1 && glyph.Style.Opacity >= 0)
                    {
                        float[] alphaMatrix = new float[]{
                            1, 0, 0, 0, 0,
                            0, 1, 0, 0, 0,
                            0, 0, 1, 0, 0,
                            0, 0, 0, glyph.Style.Opacity, 0
                        };

                        using (var colorFilter = SKColorFilter.CreateColorMatrix(alphaMatrix))
                        using (var opacityFilter = SKImageFilter.CreateColorFilter(colorFilter))
                        {
                            if (blur != null)
                            {
                                using (var compose = SKImageFilter.CreateCompose(opacityFilter, blur))
                                    fontPaint.ImageFilter = compose;
                            }
                            else
                            {
                                fontPaint.ImageFilter = opacityFilter;
                            }
                        }
                    }

                    var position = glyph.Position;
                    canvas.DrawText(glyph.Character.ToString(), position, SKTextAlign.Center, font, fontPaint);
                }

                if (glyph.Style.Underlined)
                    DrawUnderline(canvas, glyph, fontPaint);

                canvas.RestoreToCount(save);
            }
        }

        public virtual void DrawUnderline(SKCanvas canvas, Glyph glyph, SKPaint paint)
        {
            if (glyph.Character == ' ') return;

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