using System.Text.RegularExpressions;
using FenUISharp.Objects.Text;
using FenUISharp.Objects.Text.Model;
using FenUISharp.Objects.Text.Rendering;
using SkiaSharp;

namespace FenUISharp.Objects.Text.Layout
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
                            lines.Add(newLine);
                        }
                        continue;
                    }

                    var currentLine = lines[^1];

                    // Calculate word width by summing individual char widths with their specific fonts
                    float wordWidth = MeasureStringWidth(word, model.Typeface, part);

                    // Check if word fits on current line
                    if (currentLine.Width + wordWidth > bounds.Width)
                    {
                        if (!AllowLinebreakOnOverflow && AllowEllipsis)
                        {
                            // Truncate current line with ellipsis
                            TruncateLineWithEllipsis(currentLine, model.Typeface, part, bounds.Width);
                            return lines; // Stop processing
                        }
                        else if (AllowLinebreakOnOverflow)
                        {
                            // Move to next line if current line has content
                            if (currentLine.Characters.Count > 0)
                            {
                                var newLine = new LayoutLine();
                                lines.Add(newLine);
                                currentLine = lines[^1];
                            }

                            // Only check height bounds if we would have multiple lines
                            bool wouldExceedHeight = false;
                            if (lines.Count > 1)
                            {
                                float totalHeight = CalculateTotalHeight(lines);
                                wouldExceedHeight = totalHeight > bounds.Height;
                            }

                            if (wouldExceedHeight && AllowEllipsis)
                            {
                                // Remove the last line and add ellipsis to previous line
                                lines.RemoveAt(lines.Count - 1);
                                var lastLine = lines[^1];
                                TruncateLineWithEllipsis(lastLine, model.Typeface, part, bounds.Width);
                                return lines;
                            }

                            // Try to fit word, character by character if needed
                            if (!TryAddWord(currentLine, word, model.Typeface, part, bounds.Width))
                                AddWordCharacterByCharacter(lines, word, model.Typeface, part, bounds);
                        }
                        else
                        {
                            // Neither ellipsis nor line breaks allowed - force overflow
                            AddCharsToLine(currentLine, word, model.Typeface, part);
                        }
                    }
                    else
                    {
                        // Word fits on current line
                        AddCharsToLine(currentLine, word, model.Typeface, part);
                    }

                    // Check height after adding content
                    if (lines.Count > 1)
                    {
                        float totalHeight = CalculateTotalHeight(lines);
                        if (totalHeight > bounds.Height && AllowEllipsis)
                        {
                            var lastLine = lines[^1];
                            TruncateLineWithEllipsis(lastLine, model.Typeface, part, bounds.Width);
                            break;
                        }
                    }
                }
            }

            return lines;
        }

        /// <summary>
        /// Tries to add a word to the given LayoutLine
        /// </summary>
        /// <param name="line">The target line</param>
        /// <param name="word">The given word</param>
        /// <param name="typeface">The words' typeface</param>
        /// <param name="part">The part the word is contained in</param>
        /// <param name="maxWidth">The maximum width of the line</param>
        /// <returns></returns>
        private bool TryAddWord(LayoutLine line, string word, Model.FTypeface typeface, TextSpan part, float maxWidth)
        {
            float wordWidth = MeasureStringWidth(word, typeface, part);
            
            if (line.Width + wordWidth <= maxWidth)
            {
                AddCharsToLine(line, word, typeface, part);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Iterates over every character, creates the specific font for that script, 
        /// updates the line metrics, and adds the character.
        /// </summary>
        private void AddCharsToLine(LayoutLine line, string text, Model.FTypeface typeface, TextSpan part)
        {
            foreach (char c in text)
            {
                if (c == '\n' || c == '\r') continue;
                
                var font = TextRenderer.CreateFont(c, typeface, part.Style);            
                var fontMetrics = new FontMetrics(font);
                line.UpdateMetrics(fontMetrics);
                float charWidth = font.MeasureText(c.ToString());
                
                var layoutChar = new LayoutCharacter
                {
                    Character = c,
                    Width = charWidth,
                    CharacterSpacing = part.CharacterSpacing,
                    Font = font, // Store the resolved font
                    Style = part.Style
                };

                line.Characters.Add(layoutChar);
                line.Width += charWidth + part.CharacterSpacing;
            }
        }

        private void AddWordCharacterByCharacter(List<LayoutLine> lines, string word, Model.FTypeface typeface, 
            TextSpan part, SKRect bounds)
        {
            var currentLine = lines[^1];
            
            foreach (char c in word)
            {
                if (c == '\n' || c == '\r') continue;

                // Resolve Font
                var font = TextRenderer.CreateFont(c, typeface, part.Style);
                var fontMetrics = new FontMetrics(font);

                float charWidth = font.MeasureText(c.ToString());
                
                // Check if character fits
                if (currentLine.Width + charWidth + part.CharacterSpacing > bounds.Width)
                {
                    if (AllowLinebreakOnOverflow)
                    {
                        // Check height bounds
                        float currentTotalHeight = CalculateTotalHeight(lines);
                        float projectedHeight = currentTotalHeight + fontMetrics.LineHeight;

                        if (lines.Count > 0 && projectedHeight > bounds.Height && AllowEllipsis)
                        {
                            TruncateLineWithEllipsis(currentLine, typeface, part, bounds.Width);
                            return;
                        }

                        var newLine = new LayoutLine();
                        // New line with new metrics
                        newLine.UpdateMetrics(fontMetrics);
                        lines.Add(newLine);
                        currentLine = lines[^1];
                    }
                }

                currentLine.UpdateMetrics(fontMetrics);

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

        private void TruncateLineWithEllipsis(LayoutLine line, Model.FTypeface typeface, TextSpan part, float maxWidth)
        {
            // Resolve font for Ellipsis
            var font = TextRenderer.CreateFont(EllipsisChar, typeface, part.Style);
            float ellipsisWidth = font.MeasureText(EllipsisChar.ToString());
            
            // Update line metrics to ensure the ellipsis fits vertically
            line.UpdateMetrics(new FontMetrics(font));

            // Remove characters until ellipsis fits
            while (line.Characters.Count > 0 && line.Width + ellipsisWidth > maxWidth)
            {
                var lastChar = line.Characters[^1];
                line.Width -= lastChar.Width + lastChar.CharacterSpacing;
                line.Characters.RemoveAt(line.Characters.Count - 1);
            }

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

        private float MeasureStringWidth(string text, Model.FTypeface typeface, TextSpan part)
        {
            float width = 0;
            foreach (char c in text)
            {
                if (c != '\n' && c != '\r')
                {
                    // Resolve font per char to get accurate measurement
                    using var font = TextRenderer.CreateFont(c, typeface, part.Style);
                    width += font.MeasureText(c.ToString()) + part.CharacterSpacing;
                }
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
            
            float totalHeight = CalculateTotalHeight(layoutLines);
            float verticalOffset = CalculateVerticalOffset(model.Align.VerticalAlign, bounds.Height, totalHeight);
            
            float currentY = verticalOffset;
            
            foreach (var line in layoutLines)
            {
                float horizontalOffset = CalculateHorizontalOffset(model.Align.HorizontalAlign, bounds.Width, line.Width);
                
                float currentX = horizontalOffset;
                // Baseline is derived from the max metrics of all fonts in this line
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
                _ => 0 
            };
        }

        private float CalculateHorizontalOffset(TextAlign.AlignType horizontalAlign, float boundsWidth, float lineWidth)
        {
            return horizontalAlign switch
            {
                TextAlign.AlignType.Middle => (boundsWidth - lineWidth) / 2,
                TextAlign.AlignType.End => boundsWidth - lineWidth,
                _ => 0 
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
                // This ensures the line grows to fit the tallest font (e.g. mixed English and Chinese)
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