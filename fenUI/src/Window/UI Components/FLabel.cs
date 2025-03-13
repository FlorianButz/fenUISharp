using SkiaSharp;

namespace FenUISharp
{
    public class FLabel : FUIComponent
    {
        private string _text = "";
        public string Text { get => _text; set { SetText(value); } }
        private SKFont Font { get; set; }

        public bool UseLinebreaks { get; set; }
        private float _scrollOffset = 0;
        private float ScrollSpeed { get; set; } = 0.5f;
        private float FadeLength { get; set; } = 0.1f;

        public SKTypeface? Typeface { get; private set; }
        private float _textSize = 14;
        public float TextSize { get => _textSize; set { _textSize = value; UpdateFont(); } }
        private float _textWeight = 500;
        public float TextWeight { get => _textWeight; set { _textWeight = value; UpdateFont(); } }

        private SKTextAlign _textAlign = SKTextAlign.Center;
        public SKTextAlign TextAlign { get => _textAlign; set { _textAlign = value; Invalidate(); } }

        private SKImageFilter? dropShadow;

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

        protected override void OnComponentDestroy()
        {
            base.OnComponentDestroy();
            dropShadow?.Dispose();
        }

        public FLabel(string text, Vector2 position, Vector2 size, float fontSize = 14, string? typefaceName = null, bool useLinebreaks = false) : base(position, size)
        {
            Text = text;
            _textSize = fontSize;
            UseLinebreaks = useLinebreaks;

            if (typefaceName == null)
                Typeface = FResources.GetTypeface("inter-regular");
            else
                Typeface = FResources.GetTypeface(typefaceName);

            dropShadow = SKImageFilter.CreateDropShadow(0, 0, 3, 3, SKColors.Black.WithAlpha(100));
            skPaint.ImageFilter = dropShadow;

            UpdateFont();
            Invalidate();
        }

        void DrawTextInRect(SKCanvas canvas, string text, SKRect bounds, SKTextAlign align)
        {
            Font.GetFontMetrics(out SKFontMetrics fm);
            float lineHeight = (fm.Descent - fm.Ascent) + fm.Leading;
            float spaceWidth = Font.MeasureText(" ");
            List<string> lines = new List<string>();

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
                    if (!string.IsNullOrWhiteSpace(currentLine))
                    {
                        lines.Add(currentLine.TrimEnd());
                        currentLine = "";
                        currentWidth = 0;
                    }
                    var brokenParts = BreakWord(word, bounds.Width, skPaint);
                    foreach (var part in brokenParts)
                    {
                        lines.Add(part);
                    }
                }
                else
                {
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
            if (!string.IsNullOrWhiteSpace(currentLine))
            {
                lines.Add(currentLine.TrimEnd());
            }

            float totalTextHeight = lines.Count * lineHeight;
            float y = bounds.Top + (bounds.Height - totalTextHeight) / 2 - fm.Ascent;

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

        protected override void OnMouseEnter()
        {
            base.OnMouseEnter();

            SetText("Test Text On Hover! Hello there!");
        }

        void DrawScrollingText(SKCanvas canvas, string text, SKRect bounds)
        {
            Font.GetFontMetrics(out SKFontMetrics fm);
            float lineHeight = (fm.Descent - fm.Ascent) + fm.Leading;
            float y = bounds.Top + (bounds.Height - lineHeight) / 2 - fm.Ascent;
            float textWidth = Font.MeasureText(text);

            if (textWidth <= bounds.Width)
            {
                float x = bounds.Left;
                if (_textAlign == SKTextAlign.Center)
                {
                    x = bounds.MidX - textWidth / 2;
                }
                else if (_textAlign == SKTextAlign.Right)
                {
                    x = bounds.Right - textWidth;
                }
                
                canvas.DrawText(text, x, y, Font, skPaint);
                return;
            }

            float gap = bounds.Width / 4;

            _scrollOffset -= ScrollSpeed;
            if (_scrollOffset < -textWidth /* Keep Offset when teleporting */ - gap)
            {
                _scrollOffset += textWidth /* Add the offset */ + gap;
            }

            canvas.SaveLayer(bounds, null);
            canvas.ClipRect(bounds);

            float startX = bounds.Left + _scrollOffset;
            canvas.DrawText(text, startX, y, Font, skPaint);
            canvas.DrawText(text, startX + textWidth /* Add Offset */ + gap, y, Font, skPaint);

            using (var maskPaint = new SKPaint())
            {
                maskPaint.BlendMode = SKBlendMode.DstIn;
                maskPaint.Shader = SKShader.CreateLinearGradient(
                    new SKPoint(bounds.Left, 0),
                    new SKPoint(bounds.Right, 0),
                    new SKColor[] { SKColors.Transparent, SKColors.Black, SKColors.Black, SKColors.Transparent },
                    new float[] { 0f, FadeLength, 1 - FadeLength, 1f },
                    SKShaderTileMode.Clamp
                );
                canvas.DrawRect(bounds, maskPaint);
            }

            canvas.Restore();
        }



        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (!UseLinebreaks)
            {
                float textWidth = Font.MeasureText(Text);
                if (textWidth > transform.localBounds.Width)
                    Invalidate();
            }
        }

        protected override void DrawToSurface(SKCanvas canvas)
        {
            if (UseLinebreaks)
                DrawTextInRect(canvas, Text, transform.localBounds, TextAlign);
            else
                DrawScrollingText(canvas, Text, transform.localBounds);
        }

        public float GetSignleLineTextWidth(){
            return Font.MeasureText(Text, skPaint);
        }
     
        public float GetSignleLineTextHeight(){
            Font.GetFontMetrics(out SKFontMetrics fm);
            float lineHeight = (fm.Descent - fm.Ascent) + fm.Leading;
            return lineHeight;
        }
    }
}