using SkiaSharp;

namespace FenUISharp
{
    public class FLabel : UIComponent
    {
        private string _text = "";
        public string Text { get => _text; set { SetText(value); } }
        private SKFont Font { get; set; }

        public TextTruncation Truncation { get; set; }

        private float _scrollOffset = 0;
        private float _speedMulti = -10;
        private float ScrollSpeed { get; set; } = 0.75f;
        private float FadeLength { get; set; } = 0.075f;

        public SKTypeface? Typeface { get; private set; }
        private float _textSize = 14;
        public float TextSize { get => _textSize; set { _textSize = value; UpdateFont(); } }
        private float _textWeight = 500;
        public float TextWeight { get => _textWeight; set { _textWeight = value; UpdateFont(); } }

        private SKTextAlign _textAlign = SKTextAlign.Center;
        public SKTextAlign TextAlign { get => _textAlign; set { _textAlign = value; Invalidate(); } }

        private SKImageFilter? dropShadow;

        private AnimatorComponent changeTextAnim;

        void UpdateFont()
        {
            if (Font != null) Font.Dispose();

            Font = new SKFont(Typeface, TextSize);
            Font.Hinting = SKFontHinting.Full;
            Font.Subpixel = true;
	        Font.Edging = SKFontEdging.SubpixelAntialias;

            Invalidate();
        }

        private bool isSettingText = false; // Cancle too fast SetText calls to avoid visual issues
        public void SetText(string text)
        {
            if(Text == text || isSettingText == true) return;
            isSettingText = true;

            // renderQuality.SetValue(this, 1f, 35);

            changeTextAnim.Start();
            changeTextAnim.onComplete = () => {
                _text = text;
                Invalidate();

                changeTextAnim.inverse = true;
                changeTextAnim.Start();

                changeTextAnim.onComplete = () => {
                    changeTextAnim.inverse = false;
                    changeTextAnim.onComplete = null;
                    isSettingText = false;

                    skPaint.ImageFilter = dropShadow;
                    
                    renderQuality.DissolveValue(this);
                    Invalidate();
                };
            };
        }

        public void SilentSetText(string text){
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
            changeTextAnim?.Dispose();
        }

        public FLabel(string text, Vector2 position, Vector2 size, float fontSize = 14, string? typefaceName = null, TextTruncation truncation = TextTruncation.Elipsis) : base(position, size)
        {
            _text = text;
            _textSize = fontSize;
            Truncation = truncation;

            if (typefaceName == null)
                Typeface = Resources.GetTypeface("inter-regular");
            else
                Typeface = Resources.GetTypeface(typefaceName);

            dropShadow = SKImageFilter.CreateDropShadow(0, 0, 3, 3, SKColors.Black.WithAlpha(100));
            skPaint.ImageFilter = dropShadow;

            changeTextAnim = new AnimatorComponent(this, Easing.EaseInCubic, Easing.EaseOutCubic);
            changeTextAnim.duration = 0.2f;
            changeTextAnim.onValueUpdate += (t) => {
                float scaleTime = 0.75f + (1f - t) * 0.25f;

                using (var blur = SKImageFilter.CreateBlur(t * 5, t * 5)){
                    if(blur == null || dropShadow == null) return;
                    using (var compose = SKImageFilter.CreateCompose(dropShadow, blur)){
                        skPaint.ImageFilter = compose;
                    }
                }

                transform.scale = new Vector2(1, 1) * scaleTime;
                Invalidate();
            };
            components.Add(changeTextAnim);

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

            canvas.SaveLayer(bounds, null);
            canvas.ClipRect(bounds);

            float gap = transform.localBounds.Width / 4;
            float startX = bounds.Left + _scrollOffset;

            canvas.DrawText(text, startX, y, Font, skPaint);
            canvas.DrawText(text, startX + textWidth /* Add Offset */ + gap, y, Font, skPaint);

            var leftAlpha = 1f - RMath.Clamp(Math.Abs(startX) / 2, 0, 1) + RMath.Clamp(1 - (startX + (textWidth + gap) - 30), 0, 1);

            using (var maskPaint = new SKPaint())
            {
                maskPaint.BlendMode = SKBlendMode.DstIn;
                maskPaint.Shader = SKShader.CreateLinearGradient(
                    new SKPoint(bounds.Left, 0),
                    new SKPoint(bounds.Right, 0),
                    new SKColor[] { SKColors.Black.WithAlpha((byte)(leftAlpha * 255)), SKColors.Black, SKColors.Black, SKColors.Transparent },
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

            if (Truncation == TextTruncation.Scroll)
            {
                float textWidth = Font.MeasureText(Text);
                if (textWidth > transform.localBounds.Width)
                {
                    float gap = transform.localBounds.Width / 4;

                    _speedMulti = RMath.Lerp(_speedMulti, 1f, Window.DeltaTime);
                    _scrollOffset -= ScrollSpeed * RMath.Clamp(_speedMulti, 0, 1) * (Window.DeltaTime * 35);

                    if (_scrollOffset < -textWidth /* Keep Offset when teleporting */ - gap)
                    {
                        _scrollOffset += textWidth /* Add the offset */ + gap;
                        _speedMulti = -10;
                    }

                    Invalidate();
                }
            }
        }

        protected override void DrawToSurface(SKCanvas canvas)
        {
            if (Truncation == TextTruncation.Linebreak)
                DrawTextInRect(canvas, Text, transform.localBounds, TextAlign);
            else if (Truncation == TextTruncation.Elipsis)
                DrawTextInRect(canvas, GetTruncatedText(), transform.localBounds, TextAlign);
            else
                DrawScrollingText(canvas, Text, transform.localBounds);
        }

        public string GetTruncatedText(){
            if(Font.MeasureText(_text, skPaint) <= transform.size.x) return _text;

            string truncatedText = "";

            for(int c = 0; c < _text.Length; c++){
                string t = _text.Substring(0, c) + "...";

                if(Font.MeasureText(t, skPaint) < transform.size.x)
                    truncatedText = t;
                else break;
            }

            return truncatedText;
        }

        public float GetSingleLineTextWidth()
        {
            return Font.MeasureText(Text, skPaint);
        }

        public float GetSingleLineTextHeight()
        {
            Font.GetFontMetrics(out SKFontMetrics fm);
            float lineHeight = (fm.Descent - fm.Ascent) + fm.Leading;
            return lineHeight;
        }
    }

    public enum TextTruncation {
        Elipsis,
        Scroll,
        Linebreak
    }
}