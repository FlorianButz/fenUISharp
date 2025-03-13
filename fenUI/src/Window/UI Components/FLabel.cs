using SkiaSharp;

namespace FenUISharp
{
    public class FLabel : UIComponent
    {
        private string _text = "";
        public string Text { get => _text; set { SetText(value); } }
        private SKFont Font { get; set; }

        public SKTypeface? Typeface { get; private set; }
        private float _textSize = 14;
        public float TextSize { get => _textSize; set { _textSize = value; UpdateFont(); } }
        private float _textWeight = 500;
        public float TextWeight { get => _textWeight; set { _textWeight = value; UpdateFont(); } }

        private SKTextAlign _textAlign = SKTextAlign.Center;
        public SKTextAlign TextAlign { get => _textAlign; set { _textAlign = value; Invalidate(); } }

        void UpdateFont()
        {
            if (Font != null) Font.Dispose();

            Font = new SKFont(Typeface, TextSize);
            Font.Hinting = SKFontHinting.Full;
            Font.Subpixel = true;

            Invalidate();
        }

        void SetText(string text)
        {
            _text = text;
            Invalidate();
        }

        public void SetTypeface(SKTypeface sKTypeface)
        {
            Typeface = sKTypeface;
            UpdateFont();
        }

        public FLabel(string text, Vector2 position, Vector2 size, float fontSize = 14, string? typefaceName = null) : base(position.x, position.y, size.x, size.y)
        {
            Text = text;
            _textSize = fontSize;

            if(typefaceName == null)
                Typeface = FResources.GetTypeface("inter-regular");
            else
                Typeface = FResources.GetTypeface(typefaceName);

            UpdateFont();

            Invalidate();
        }

        void DrawTextInRect(SKCanvas canvas, string text, SKRect bounds, SKTextAlign align)
        {
            Font.GetFontMetrics(out SKFontMetrics fm);
            float lineHeight = (fm.Descent - fm.Ascent) + fm.Leading;
            float spaceWidth = Font.MeasureText(" ");
            List<string> lines = new List<string>();

            // Helper function to break a long word into parts that fit within maxWidth.
            List<string> BreakWord(string word, float maxWidth, SKPaint paint)
            {
                List<string> parts = new List<string>();
                string currentPart = "";
                foreach (char c in word)
                {
                    string test = currentPart + c;
                    if (Font.MeasureText(test) > maxWidth && currentPart.Length > 0)
                    {
                        parts.Add(currentPart);
                        currentPart = c.ToString();
                    }
                    else
                    {
                        currentPart = test;
                    }
                }
                if (!string.IsNullOrEmpty(currentPart))
                {
                    parts.Add(currentPart);
                }
                return parts;
            }

            string[] words = text.Split(' ');
            string currentLine = "";
            float currentWidth = 0;

            foreach (var word in words)
            {
                float wordWidth = Font.MeasureText(word);
                if (wordWidth > bounds.Width)
                {
                    // If the word itself is too long, flush the current line first.
                    if (!string.IsNullOrWhiteSpace(currentLine))
                    {
                        lines.Add(currentLine.TrimEnd());
                        currentLine = "";
                        currentWidth = 0;
                    }
                    // Break the long word into manageable parts.
                    var brokenParts = BreakWord(word, bounds.Width, skPaint);
                    // Add each broken part as a separate line.
                    foreach (var part in brokenParts)
                    {
                        lines.Add(part);
                    }
                }
                else
                {
                    // Check if the word fits in the current line.
                    if (currentWidth + wordWidth > bounds.Width)
                    {
                        lines.Add(currentLine.TrimEnd());
                        currentLine = word + " ";
                        currentWidth = wordWidth + spaceWidth;
                    }
                    else
                    {
                        currentLine += word + " ";
                        currentWidth += wordWidth + spaceWidth;
                    }
                }
            }
            // Add any remaining text.
            if (!string.IsNullOrWhiteSpace(currentLine))
            {
                lines.Add(currentLine.TrimEnd());
            }

            // Calculate the vertical starting position to center the block.
            float totalTextHeight = lines.Count * lineHeight;
            float y = bounds.Top + (bounds.Height - totalTextHeight) / 2 - fm.Ascent;

            // Render each line with the chosen alignment.
            foreach (var line in lines)
            {
                float textWidth = Font.MeasureText(line);
                float x = bounds.Left;
                if (align == SKTextAlign.Center)
                {
                    x = bounds.MidX - textWidth / 2;
                }
                else if (align == SKTextAlign.Right)
                {
                    x = bounds.Right - textWidth;
                }

                canvas.DrawText(line, x, y, Font, skPaint);
                y += lineHeight;
                if (y - fm.Descent > bounds.Bottom)
                    break;
            }
        }

        protected override void DrawToSurface(SKCanvas canvas)
        {
            DrawTextInRect(canvas, Text, transform.localBounds, TextAlign);
        }
    }
}