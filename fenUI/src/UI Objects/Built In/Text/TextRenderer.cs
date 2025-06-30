using FenUISharp.Mathematics;
using FenUISharp.Objects.Text.Model;
using SkiaSharp;

namespace FenUISharp.Objects.Text.Rendering
{
    public class TextRenderer
    {
        protected FText Parent { get; init; }

        public TextRenderer(FText parent)
        {
            Parent = parent;
        }

        public virtual void DrawText(SKCanvas canvas, TextModel model, List<Glyph> glyphs, SKRect localBounds, SKPaint paint)
        {
            for (int i = 0; i < glyphs.Count; i++)
            {
                if (!RMath.IsRectPartiallyInside(localBounds, glyphs[i].Bounds)) continue;
                int save = canvas.Save();
                // canvas.Translate(Parent.Padding.CachedValue, Parent.Padding.CachedValue); Edit: Not needed anymore

                using var fontPaint = paint.Clone();
                using var backgroundPaint = paint.Clone();

                var glyph = glyphs[i];

                // Set glyph color and background color
                backgroundPaint.Color = glyph.Style.BackgroundColor();
                fontPaint.Color = glyph.Style.Color();

                canvas.Scale(glyph.Scale.Width, glyph.Scale.Height, glyph.Position.X + glyph.Bounds.Width * glyph.Anchor.X / 2, glyph.Position.Y + -glyph.Bounds.Height * glyph.Anchor.Y / 2);

                // Batching together backgrounds with same color
                // Test if should draw text background
                SKColor backgroundColor = backgroundPaint.Color;
                if (backgroundColor.Alpha != 0)
                {
                    // Make sure to not draw if already drawn
                    if (i == 0 || glyphs[i - 1].Style.BackgroundColor() != backgroundColor)
                    {
                        // Start with the first bounds
                        var startingGlyphBounds = glyph.Bounds;
                        startingGlyphBounds.Offset(0, startingGlyphBounds.Height / 2);

                        SKRect drawRect = startingGlyphBounds;

                        int addition = 0;
                        var currentGlyph = glyph;
                        do
                        {
                            var currentGlyphBounds = currentGlyph.Bounds;
                            currentGlyphBounds.Offset(0, currentGlyphBounds.Height / 2);

                            // Combine them
                            drawRect = new(
                                MathF.Min(drawRect.Left, currentGlyph.Bounds.Left),
                                MathF.Min(drawRect.Top, currentGlyph.Bounds.Top),
                                MathF.Max(drawRect.Right, currentGlyph.Bounds.Right),
                                MathF.Max(drawRect.Bottom, currentGlyph.Bounds.Bottom)
                            );

                            addition++;
                            if ((i + addition) >= glyphs.Count) break;
                            currentGlyph = glyphs[i + addition];
                        } while (currentGlyph.Style.BackgroundColor() == backgroundColor);

                        // Create a rounded rect out of the bounds
                        using var roundRect = new SKRoundRect(drawRect, 5);
                        canvas.DrawRoundRect(roundRect, backgroundPaint);
                    }
                }

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
            if (char.IsWhiteSpace(glyph.Character)) return;

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