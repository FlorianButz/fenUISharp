using System.Text.RegularExpressions;
using FenUISharp.Components.Text.Model;
using FenUISharp.Components.Text.Rendering;
using SkiaSharp;

namespace FenUISharp.Components.Text.Layout
{
    public class WrapLayout : TextLayout
    {
        public WrapLayout(FText Parent) : base(Parent)
        {
        }

        public override List<Glyph> ProcessModel(TextModel model)
        {
            var lines = new List<TextLine>();
            var returnList = new List<Glyph>();
            var bounds = Parent.Transform.LocalBounds;

            float currentLineY = 0;
            float lineHeight = 0;
            float ascent = 0;
            float descent = 0;
            float leading = 0;
            float baselineOff = 0;

            // start first line
            lines.Add(new TextLine());

            // for each styled run
            foreach (var part in model.TextParts)
            {
                var font = TextRenderer.CreateFont(model.Typeface, part.Style);

                // SKia: ascent is negative, descent positive
                ascent = font.Metrics.Ascent;
                descent = font.Metrics.Descent;
                leading = font.Metrics.Leading;
                lineHeight = -ascent + descent + leading;
                baselineOff = -ascent;

                // update the current line’s height
                var line = lines[^1];
                line.LineHeight = lineHeight;

                // split on whitespace but keep it
                var words = Regex.Split(part.Content, @"(\s+)");

                foreach (var word in words)
                {
                    float wordWidth = 0;
                    foreach (char c in word)
                        wordWidth += font.MeasureText(c.ToString()) + part.CharacterSpacing;

                    // Word doesn't fit
                    if (line.LineWidth + wordWidth > bounds.Width)
                    {
                        // if it's not the first thing on the line, start new line before splitting
                        if (line.Glyphs.Count > 0)
                        {
                            currentLineY += lineHeight;
                            lines.Add(new TextLine { LineHeight = lineHeight });
                            line = lines[^1];
                        }

                        // Try to fit the word char by char
                        foreach (char c in word)
                        {
                            float charWidth = font.MeasureText(c.ToString()) + part.CharacterSpacing;

                            if (line.LineWidth + charWidth > bounds.Width)
                            {
                                // wrap to new line
                                currentLineY += lineHeight;
                                lines.Add(new TextLine { LineHeight = lineHeight });
                                line = lines[^1];
                            }

                            float halfW = font.MeasureText(c.ToString()) / 2;
                            float halfSpacing = part.CharacterSpacing / 2;

                            float x = line.LineWidth + halfW + halfSpacing;
                            float y = currentLineY + baselineOff;

                            var glyph = new Glyph(
                                c,
                                new SKPoint(x, y),
                                new SKSize(1, 1),
                                new SKPoint(0.5f, 0.5f),
                                part.Style,
                                new SKSize(font.MeasureText(c.ToString()), lineHeight)
                            );

                            returnList.Add(glyph);
                            line.Glyphs.Add(glyph);

                            line.LineWidth += charWidth;
                        }
                    }
                    else
                    {
                        // Word fits, so add normally
                        foreach (char c in word)
                        {
                            float halfW = font.MeasureText(c.ToString()) / 2;
                            float halfSpacing = part.CharacterSpacing / 2;

                            float x = line.LineWidth + halfW + halfSpacing;
                            float y = currentLineY + baselineOff;

                            var glyph = new Glyph(
                                c,
                                new SKPoint(x, y),
                                new SKSize(1, 1),
                                new SKPoint(0.5f, 0.5f),
                                part.Style,
                                new SKSize(font.MeasureText(c.ToString()), lineHeight)
                            );

                            returnList.Add(glyph);
                            line.Glyphs.Add(glyph);

                            line.LineWidth += font.MeasureText(c.ToString()) + part.CharacterSpacing;
                        }
                    }
                }
            }


            // ========== HORIZONTAL ALIGN ==========
            if (model.Align.HorizontalAlign != TextAlign.AlignType.Start)
            {
                for (int i = 0; i < lines.Count; i++)
                {
                    float offsetX = model.Align.HorizontalAlign == TextAlign.AlignType.Middle
                        ? (bounds.Width - lines[i].LineWidth) / 2
                        : bounds.Width - lines[i].LineWidth;

                    foreach (var g in lines[i].Glyphs)
                        g.Position = new SKPoint(g.Position.X + offsetX, g.Position.Y);
                }
            }

            // ========== VERTICAL ALIGN ==========
            if (model.Align.VerticalAlign != TextAlign.AlignType.Start)
            {
                // total height is just how far we’ve stacked down + the last line’s height
                float fullHeight = currentLineY + lineHeight;

                float offsetY = model.Align.VerticalAlign == TextAlign.AlignType.Middle
                    ? (bounds.Height - fullHeight) / 2
                    : bounds.Height - fullHeight;

                foreach (var l in lines)
                    foreach (var g in l.Glyphs)
                        g.Position = new SKPoint(g.Position.X, g.Position.Y + offsetY);
            }

            return returnList;
        }
    }
}