using System.Text.RegularExpressions;
using FenUISharp.Components.Text.Model;
using FenUISharp.Components.Text.Rendering;
using SkiaSharp;

namespace FenUISharp.Components.Text.Layout
{
    public class WrapLayout : TextLayout
    {
        public char EllipsisChar { get; set; } = '\u2026';
        public bool AllowLinebreakChar { get; set; } = true;
        public bool AllowLinebreakOnOverflow { get; set; } = true;

        public WrapLayout(FText Parent) : base(Parent)
        {
        }

        public virtual string[] SplitWords(string content) => Regex.Split(content, @"(\s+|\n)");

        public override List<Glyph> ProcessModel(TextModel model, SKRect bounds)
        {
            var lines = new List<TextLine>();
            var returnList = new List<Glyph>();

            float currentLineY = 0;
            float lineHeight = 0;
            float ascent = 0;
            float descent = 0;
            float leading = 0;
            float baselineOff = 0;

            lines.Add(new TextLine());

            foreach (var part in model.TextParts)
            {
                var font = TextRenderer.CreateFont(model.Typeface, part.Style);

                ascent = font.Metrics.Ascent;
                descent = font.Metrics.Descent;
                leading = font.Metrics.Leading;
                lineHeight = -ascent + descent + leading;
                baselineOff = -ascent;

                var line = lines[^1];
                line.LineHeight = lineHeight;

                var words = SplitWords(part.Content);

                foreach (var word in words)
                {
                    if (string.IsNullOrEmpty(word))
                        continue;
                    if (word.Contains("\r\n") || word.Contains("\n"))
                    {
                        if (AllowLinebreakChar && AllowLinebreakOnOverflow)
                        {
                            currentLineY += lineHeight;
                            lines.Add(new TextLine { LineHeight = lineHeight });
                            line = lines[^1];
                        }
                        continue;
                    }

                    bool stopProcessing = false;

                    float wordWidth = 0;
                    foreach (char c in word)
                    {
                        if (c == '\n' || c == '\r')
                            continue;

                        wordWidth += font.MeasureText(c.ToString()) + part.CharacterSpacing;
                    }

                    if (line.LineWidth + wordWidth > bounds.Width)
                    {
                        if (!AllowLinebreakOnOverflow)
                        {
                            AppendEllipsis(line, font, part, bounds.Width, returnList, baselineOff);
                            stopProcessing = true;
                        }
                        else
                        {
                            if (line.Glyphs.Count > 0)
                            {
                                currentLineY += lineHeight;
                                if (currentLineY + lineHeight > bounds.Height)
                                {
                                    AppendEllipsis(line, font, part, bounds.Width, returnList, baselineOff);
                                    stopProcessing = true;
                                }
                                else
                                {
                                    lines.Add(new TextLine { LineHeight = lineHeight });
                                }
                            }

                            line = lines[^1];

                            foreach (char c in word)
                            {
                                if (stopProcessing) break;

                                float charWidth = font.MeasureText(c.ToString()) + part.CharacterSpacing;

                                if (line.LineWidth + charWidth > bounds.Width)
                                {
                                    currentLineY += lineHeight;
                                    if (currentLineY + lineHeight > bounds.Height)
                                    {
                                        AppendEllipsis(line, font, part, bounds.Width, returnList, baselineOff);
                                        stopProcessing = true;
                                        break;
                                    }
                                    lines.Add(new TextLine { LineHeight = lineHeight });
                                    line = lines[^1];
                                }

                                AddGlyph(c, font, part, line, currentLineY, baselineOff, returnList);
                            }
                        }
                    }
                    else
                    {
                        foreach (char c in word)
                            AddGlyph(c, font, part, line, currentLineY, baselineOff, returnList);

                    }

                    if (stopProcessing)
                        break;
                }
            }

            ApplyHorizontalAlign(model, bounds, lines);
            ApplyVerticalAlign(model, bounds, lines);
            return returnList;
        }

        protected virtual void ApplyHorizontalAlign(TextModel model, SKRect bounds, List<TextLine> lines)
        {
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
        }

        protected virtual void ApplyVerticalAlign(TextModel model, SKRect bounds, List<TextLine> lines)
        {
            if (model.Align.VerticalAlign != TextAlign.AlignType.Start)
            {
                float fullHeight = lines.Sum(l => l.LineHeight);

                float offsetY = model.Align.VerticalAlign == TextAlign.AlignType.Middle
                    ? (bounds.Height - fullHeight) / 2
                    : bounds.Height - fullHeight;

                foreach (var l in lines)
                    foreach (var g in l.Glyphs)
                        g.Position = new SKPoint(g.Position.X, g.Position.Y + offsetY);
            }
        }

        protected virtual void AddGlyph(char c, SKFont font, TextSpan part, TextLine line, float currentLineY, float baselineOff, List<Glyph> outList)
        {
            float halfW = font.MeasureText(c.ToString()) / 2;
            float halfSpace = part.CharacterSpacing / 2;
            float x = line.LineWidth + halfW + halfSpace;
            float y = currentLineY + baselineOff;
            var glyph = new Glyph(
                c,
                new SKPoint(x, y),
                new SKSize(1, 1),
                new SKPoint(0.5f, 0.5f),
                new(part.Style),
                new SKSize(font.MeasureText(c.ToString()), -font.Metrics.Ascent + font.Metrics.Descent + font.Metrics.Leading)
            );
            outList.Add(glyph);
            line.Glyphs.Add(glyph);
            line.LineWidth += font.MeasureText(c.ToString()) + part.CharacterSpacing;
        }

        protected virtual void AppendEllipsis(TextLine line, SKFont font, TextSpan part, float maxWidth, List<Glyph> outList, float baselineOff)
        {
            float ellWidth = font.MeasureText(EllipsisChar.ToString());

            while (line.Glyphs.Count > 0 && line.LineWidth + ellWidth > maxWidth)
            {
                var last = line.Glyphs[^1];
                line.LineWidth -= last.Size.Width + part.CharacterSpacing;
                outList.Remove(last);
                line.Glyphs.RemoveAt(line.Glyphs.Count - 1);
            }

            AddGlyph(EllipsisChar, font, part, line, line.Glyphs.Count > 0 ? line.Glyphs[^1].Position.Y - baselineOff : 0, -font.Metrics.Ascent, outList);
        }
    }
}