using System.Text.RegularExpressions;
using FenUISharp.Objects.Text;
using FenUISharp.Objects.Text.Model;
using FenUISharp.Objects.Text.Rendering;
using SkiaSharp;

namespace FenUISharp.Components.Text.Layout
{
    public class WrapLayout : TextLayout
    {
        public char EllipsisChar { get; set; } = '\u2026';
        public bool AllowLinebreakChar { get; set; } = true;
        public bool AllowLinebreakOnOverflow { get; set; } = true;
        public bool AllowEllipsis { get; set; } = true;

        public WrapLayout(FText Parent) : base(Parent)
        {
        }

        public virtual string[] SplitWords(string content) => Regex.Split(content, @"(\s+|\n)");

        public override List<Glyph> ProcessModel(TextModel model, SKRect bounds)
        {
            // First pass: Calculate layout without positioning
            var layoutLines = CalculateLayout(model, bounds);
            
            // Second pass: Position glyphs with proper alignment
            return PositionGlyphs(model, bounds, layoutLines);
        }

        private List<LayoutLine> CalculateLayout(TextModel model, SKRect bounds)
        {
            var lines = new List<LayoutLine>();
            lines.Add(new LayoutLine());

            foreach (var part in model.TextParts)
            {
                var font = TextRenderer.CreateFont(model.Typeface, part.Style);
                var fontMetrics = new FontMetrics(font);

                var words = SplitWords(part.Content);

                foreach (var word in words)
                {
                    if (string.IsNullOrEmpty(word))
                        continue;

                    if (word.Contains("\r\n") || word.Contains("\n"))
                    {
                        if (AllowLinebreakChar && AllowLinebreakOnOverflow)
                        {
                            var newLine = new LayoutLine();
                            newLine.UpdateMetrics(fontMetrics);
                            lines.Add(newLine);
                        }
                        continue;
                    }

                    var currentLine = lines[^1];
                    currentLine.UpdateMetrics(fontMetrics);

                    // Calculate word width
                    float wordWidth = CalculateTextWidth(word, font, part.CharacterSpacing);

                    // Check if word fits on current line
                    if (currentLine.Width + wordWidth > bounds.Width)
                    {
                        if (!AllowLinebreakOnOverflow && AllowEllipsis)
                        {
                            // Truncate current line with ellipsis
                            TruncateLineWithEllipsis(currentLine, font, part, bounds.Width);
                            return lines; // Stop processing
                        }
                        else if (AllowLinebreakOnOverflow)
                        {
                            // Move to next line if current line has content
                            if (currentLine.Characters.Count > 0)
                            {
                                var newLine = new LayoutLine();
                                newLine.UpdateMetrics(fontMetrics);
                                lines.Add(newLine);
                                currentLine = lines[^1];
                            }

                            // Check if we exceed height bounds
                            float totalHeightL = CalculateTotalHeight(lines);
                            if (totalHeightL > bounds.Height && AllowEllipsis)
                            {
                                // Remove the last line and add ellipsis to previous line
                                if (lines.Count > 1)
                                {
                                    lines.RemoveAt(lines.Count - 1);
                                    var lastLine = lines[^1];
                                    TruncateLineWithEllipsis(lastLine, font, part, bounds.Width);
                                }
                                return lines;
                            }

                            // Try to fit word, character by character if needed
                            if (!TryAddWord(currentLine, word, font, part, bounds.Width))
                            {
                                AddWordCharacterByCharacter(lines, word, font, part, bounds, fontMetrics);
                            }
                        }
                        else
                        {
                            // Neither ellipsis nor line breaks allowed - force overflow
                            // Add the word anyway, allowing it to exceed bounds
                            AddWordToLine(currentLine, word, font, part);
                        }
                    }
                    else
                    {
                        // Word fits on current line
                        AddWordToLine(currentLine, word, font, part);
                    }

                    // Check height after adding content
                    float totalHeight = CalculateTotalHeight(lines);
                    if (totalHeight > bounds.Height && AllowEllipsis)
                    {
                        // Truncate and stop
                        var lastLine = lines[^1];
                        TruncateLineWithEllipsis(lastLine, font, part, bounds.Width);
                        break;
                    }
                }
            }

            return lines;
        }

        private bool TryAddWord(LayoutLine line, string word, SKFont font, TextSpan part, float maxWidth)
        {
            float wordWidth = CalculateTextWidth(word, font, part.CharacterSpacing);
            if (line.Width + wordWidth <= maxWidth)
            {
                AddWordToLine(line, word, font, part);
                return true;
            }
            return false;
        }

        private void AddWordToLine(LayoutLine line, string word, SKFont font, TextSpan part)
        {
            foreach (char c in word)
            {
                if (c == '\n' || c == '\r') continue;
                
                float charWidth = font.MeasureText(c.ToString());
                var layoutChar = new LayoutCharacter
                {
                    Character = c,
                    Width = charWidth,
                    CharacterSpacing = part.CharacterSpacing,
                    Font = font,
                    Style = part.Style
                };
                line.Characters.Add(layoutChar);
                line.Width += charWidth + part.CharacterSpacing;
            }
        }

        private void AddWordCharacterByCharacter(List<LayoutLine> lines, string word, SKFont font, 
            TextSpan part, SKRect bounds, FontMetrics fontMetrics)
        {
            var currentLine = lines[^1];
            
            foreach (char c in word)
            {
                if (c == '\n' || c == '\r') continue;

                float charWidth = font.MeasureText(c.ToString());
                
                // Check if character fits on current line
                if (currentLine.Width + charWidth + part.CharacterSpacing > bounds.Width)
                {
                    // Check height before creating new line
                    float totalHeight = CalculateTotalHeight(lines) + fontMetrics.LineHeight;
                    if (totalHeight > bounds.Height && AllowEllipsis)
                    {
                        TruncateLineWithEllipsis(currentLine, font, part, bounds.Width);
                        return;
                    }

                    if (AllowLinebreakOnOverflow)
                    {
                        var newLine = new LayoutLine();
                        newLine.UpdateMetrics(fontMetrics);
                        lines.Add(newLine);
                        currentLine = lines[^1];
                    }
                }

                var layoutChar = new LayoutCharacter
                {
                    Character = c,
                    Width = charWidth,
                    CharacterSpacing = part.CharacterSpacing,
                    Font = font,
                    Style = part.Style
                };
                currentLine.Characters.Add(layoutChar);
                currentLine.Width += charWidth + part.CharacterSpacing;
            }
        }

        private void TruncateLineWithEllipsis(LayoutLine line, SKFont font, TextSpan part, float maxWidth)
        {
            float ellipsisWidth = font.MeasureText(EllipsisChar.ToString());
            
            // Remove characters until ellipsis fits
            while (line.Characters.Count > 0 && line.Width + ellipsisWidth > maxWidth)
            {
                var lastChar = line.Characters[^1];
                line.Width -= lastChar.Width + lastChar.CharacterSpacing;
                line.Characters.RemoveAt(line.Characters.Count - 1);
            }

            // Add ellipsis
            var ellipsisChar = new LayoutCharacter
            {
                Character = EllipsisChar,
                Width = ellipsisWidth,
                CharacterSpacing = part.CharacterSpacing,
                Font = font,
                Style = part.Style
            };
            line.Characters.Add(ellipsisChar);
            line.Width += ellipsisWidth + part.CharacterSpacing;
        }

        private float CalculateTextWidth(string text, SKFont font, float characterSpacing)
        {
            float width = 0;
            foreach (char c in text)
            {
                if (c != '\n' && c != '\r')
                    width += font.MeasureText(c.ToString()) + characterSpacing;
            }
            return width;
        }

        private float CalculateTotalHeight(List<LayoutLine> lines)
        {
            return lines.Sum(l => l.LineHeight);
        }

        private List<Glyph> PositionGlyphs(TextModel model, SKRect bounds, List<LayoutLine> layoutLines)
        {
            var glyphs = new List<Glyph>();
            
            // Calculate total content dimensions
            float totalHeight = CalculateTotalHeight(layoutLines);
            
            // Calculate vertical alignment offset
            float verticalOffset = CalculateVerticalOffset(model.Align.VerticalAlign, bounds.Height, totalHeight);
            
            float currentY = verticalOffset;
            
            foreach (var line in layoutLines)
            {
                // Calculate horizontal alignment offset for this line
                float horizontalOffset = CalculateHorizontalOffset(model.Align.HorizontalAlign, bounds.Width, line.Width);
                
                float currentX = horizontalOffset;
                float baselineY = currentY + line.Baseline;
                
                foreach (var layoutChar in line.Characters)
                {
                    float charHalfWidth = layoutChar.Width / 2;
                    float spacingHalf = layoutChar.CharacterSpacing / 2;
                    
                    var glyph = new Glyph(
                        layoutChar.Character,
                        new SKPoint(currentX + charHalfWidth + spacingHalf, baselineY),
                        new SKSize(1, 1),
                        new SKPoint(0.5f, 0.5f),
                        new(layoutChar.Style),
                        new SKSize(layoutChar.Width, line.LineHeight)
                    );
                    
                    glyphs.Add(glyph);
                    currentX += layoutChar.Width + layoutChar.CharacterSpacing;
                }
                
                currentY += line.LineHeight;
            }
            
            return glyphs;
        }

        private float CalculateVerticalOffset(TextAlign.AlignType verticalAlign, float boundsHeight, float contentHeight)
        {
            return verticalAlign switch
            {
                TextAlign.AlignType.Middle => (boundsHeight - contentHeight) / 2,
                TextAlign.AlignType.End => boundsHeight - contentHeight,
                _ => 0 // Start
            };
        }

        private float CalculateHorizontalOffset(TextAlign.AlignType horizontalAlign, float boundsWidth, float lineWidth)
        {
            return horizontalAlign switch
            {
                TextAlign.AlignType.Middle => (boundsWidth - lineWidth) / 2,
                TextAlign.AlignType.End => boundsWidth - lineWidth,
                _ => 0 // Start
            };
        }

        // Helper classes for layout calculation
        private class LayoutLine
        {
            public List<LayoutCharacter> Characters { get; } = new List<LayoutCharacter>();
            public float Width { get; set; } = 0;
            public float LineHeight { get; set; } = 0;
            public float Baseline { get; set; } = 0;
            public float Ascent { get; set; } = 0;
            public float Descent { get; set; } = 0;

            public void UpdateMetrics(FontMetrics metrics)
            {
                LineHeight = Math.Max(LineHeight, metrics.LineHeight);
                Baseline = Math.Max(Baseline, metrics.Baseline);
                Ascent = Math.Max(Ascent, metrics.Ascent);
                Descent = Math.Max(Descent, metrics.Descent);
            }
        }

        private class LayoutCharacter
        {
            public char Character { get; set; }
            public float Width { get; set; }
            public float CharacterSpacing { get; set; }
            public SKFont Font { get; set; }
            public TextStyle Style { get; set; }
        }

        private class FontMetrics
        {
            public float LineHeight { get; }
            public float Baseline { get; }
            public float Ascent { get; }
            public float Descent { get; }

            public FontMetrics(SKFont font)
            {
                Ascent = -font.Metrics.Ascent;
                Descent = font.Metrics.Descent;
                LineHeight = Ascent + Descent + font.Metrics.Leading;
                Baseline = Ascent;
            }
        }
    }
}