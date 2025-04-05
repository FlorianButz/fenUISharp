using FenUISharp.Themes;
using SkiaSharp;

namespace FenUISharp
{
    public class FLabel : UIComponent
    {
        private string _text = "";
        public string Text { get => _text; set { SetText(value); } }
        private SKFont Font { get; set; }

        private ThemeColor _textColor;
        public ThemeColor TextColor
        {
            get => _textColor;
            set
            {
                _textColor = value;
                Invalidate();
            }
        }

        public TextTruncation Truncation { get; set; }
        public bool RenderDoubleSize { get; set; } = false;

        private float _scrollOffset = 0;
        private float _speedMulti = 0;
        private float _speedMultiAdd = -10;
        private float ScrollSpeed { get; set; } = 1;
        private float FadeLength { get; set; } = 0.075f;

        private bool _fitHorizontalToContent = false;
        public bool FitHorizontalToContent { get => _fitHorizontalToContent; set { _fitHorizontalToContent = value; SilentSetText(Text); } }

        private bool _fitVerticalToContent = false;
        public bool FitVerticalToContent { get => _fitVerticalToContent; set { _fitVerticalToContent = value; SilentSetText(Text); } }

        public SKTypeface? Typeface { get; private set; }
        private float _textSize = 14;
        public float TextSize { get => _textSize; set { _textSize = value; UpdateFont(); } }
        private float _textWeight = 500;
        public float TextWeight { get => _textWeight; set { _textWeight = value; UpdateFont(); } }

        private SKTextAlign _textAlign = SKTextAlign.Center;
        public SKTextAlign TextAlign { get => _textAlign; set { _textAlign = value; Invalidate(); } }

        private SKImageFilter? dropShadow;

        private AnimatorComponent changeTextAnim;

        public FLabel(Window root, string text, Vector2 position, Vector2 size, float fontSize = 14, string? typefaceName = null, TextTruncation truncation = TextTruncation.Elipsis, ThemeColor? textColor = null) : base(root, position, size)
        {
            SilentSetText(text);
            _textSize = fontSize;
            Truncation = truncation;

            _textColor = textColor ?? WindowRoot.WindowThemeManager.GetColor(t => t.OnSurface);

            if (typefaceName == null)
                Typeface = Resources.GetTypeface("inter-regular");
            else
                Typeface = Resources.GetTypeface(typefaceName);

            dropShadow = SKImageFilter.CreateDropShadow(0, 0, 3, 3, WindowRoot.WindowThemeManager.GetColor(t => t.Shadow).Value);
            SkPaint.ImageFilter = dropShadow;

            changeTextAnim = new AnimatorComponent(this, Easing.EaseInCubic, Easing.EaseOutCubic);
            changeTextAnim.duration = 0.2f;
            changeTextAnim.onValueUpdate += (t) =>
            {
                float scaleTime = 0.75f + (1f - t) * 0.25f;

                using (var blur = SKImageFilter.CreateBlur(t * 10, t * 10))
                {
                    if (blur == null || dropShadow == null) return;
                    using (var compose = SKImageFilter.CreateCompose(dropShadow, blur))
                    {
                        SkPaint.ImageFilter = compose;
                    }
                }

                Transform.Scale = new Vector2(1, 1) * scaleTime;
                Invalidate();
            };
            Components.Add(changeTextAnim);
            Transform.BoundsPadding.SetValue(this, 10, 25);

            UpdateFont();
            Invalidate();
        }

        void SilentUpdateFont()
        {
            if (Font != null) Font.Dispose();

            Font = new SKFont(Typeface, TextSize);
            // Font = new SKFont(SKTypeface.FromFamilyName("Segoe UI", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright), _textSize);

            Font.Hinting = SKFontHinting.Full;
            Font.Subpixel = true;
            Font.Edging = SKFontEdging.SubpixelAntialias;

            // Invalidate();
        }

        void UpdateFont()
        {
            SilentUpdateFont();
            Invalidate();
        }

        private bool isSettingText = false; // Cancle too fast SetText calls to avoid visual issues
        public void SetText(string text)
        {
            if (Text == text || isSettingText == true) { SilentSetText(text); return; }
            isSettingText = true;

            // renderQuality.SetValue(this, 1f, 35);
            int overridePad = 1;
            Transform.BoundsPadding.SetValue(overridePad, 25, 50);

            changeTextAnim.Start();
            changeTextAnim.onComplete = () =>
            {
                SilentSetText(text);

                changeTextAnim.inverse = true;
                changeTextAnim.Start();

                changeTextAnim.onComplete = () =>
                {
                    changeTextAnim.inverse = false;
                    changeTextAnim.onComplete = null;
                    isSettingText = false;

                    SkPaint.ImageFilter = dropShadow;

                    renderQuality.DissolveValue(this);
                    Transform.BoundsPadding.DissolveValue(overridePad);
                    Invalidate();
                };
            };
        }

        public void SilentSetText(string text)
        {
            if (FitHorizontalToContent)
            {
                var rect = Transform.LocalBounds;
                rect.Inflate(10000, 0);

                var calculatedTextBlockSize = new Vector2(CalculateTextBlockSize(text, rect));
                Transform.Size = new Vector2(calculatedTextBlockSize.x, Transform.Size.y);
            }

            if (FitVerticalToContent)
            {
                var rect = Transform.LocalBounds;
                rect.Inflate(0, 10000);

                var calculatedTextBlockSize = new Vector2(CalculateTextBlockSize(text, rect));
                Transform.Size = new Vector2(Transform.Size.x, calculatedTextBlockSize.y);
            }

            Transform.UpdateLayout();

            _text = text;
            Invalidate();
        }

        public void SetTypeface(SKTypeface sKTypeface)
        {
            Typeface = sKTypeface;
            UpdateFont();
        }

        protected override void ComponentDestroy()
        {
            base.ComponentDestroy();
            dropShadow?.Dispose();
            changeTextAnim?.Dispose();
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

            string[] paragraphs = text.Split(new char[] { '\n' }, StringSplitOptions.None);
            bool firstParagraph = true;
            foreach (var paragraph in paragraphs)
            {
                if (!firstParagraph)
                {
                    if (paragraph.Length == 0)
                        lines.Add("");
                }
                firstParagraph = false;

                if (paragraph.Length == 0)
                    continue;

                string[] words = paragraph.Split(' ');
                string currentLine = "";
                float currentWidth = 0;

                foreach (var word in words)
                {
                    if (string.IsNullOrEmpty(word))
                        continue;

                    float wordWidth = Font.MeasureText(word);
                    if (wordWidth > bounds.Width)
                    {
                        if (!string.IsNullOrWhiteSpace(currentLine))
                        {
                            lines.Add(currentLine.TrimEnd());
                            currentLine = "";
                            currentWidth = 0;
                        }
                        var brokenParts = BreakWord(word, bounds.Width, SkPaint);
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

                if (RenderDoubleSize)
                {
                    const float scaleFactor = 2f;
                    using (var s = WindowRoot.RenderContext.CreateAdditional(
                        new SKImageInfo((int)(GetSingleLineTextWidth() * scaleFactor),
                                        (int)(GetSingleLineTextHeight() * scaleFactor))))
                    {
                        _textSize = _textSize * scaleFactor;
                        SilentUpdateFont();

                        s.Canvas.Clear(SKColors.Transparent);
                        SkPaint.Color = _textColor.Value;
                        s.Canvas.DrawText(line, (float)Math.Round(x * scaleFactor, 0),
                            (float)Math.Round(y * scaleFactor, 0), Font, SkPaint);

                        var snapshot = s.Snapshot();
                        canvas.Scale(1 / scaleFactor);
                        canvas.DrawImage(snapshot, new SKPoint(0, 0), new SKSamplingOptions(SKFilterMode.Nearest));
                        canvas.Scale(scaleFactor);

                        _textSize = _textSize / scaleFactor;
                        SilentUpdateFont();
                    }
                }
                else
                {
                    canvas.DrawText(line, (float)Math.Round(x, 0), (float)Math.Round(y, 0), Font, SkPaint);
                }
                y += lineHeight;
                if (y - fm.Descent > bounds.Bottom)
                    break;
            }
        }

        Vector2 CalculateTextBlockSize(string text, SKRect bounds)
        {
            Font.GetFontMetrics(out SKFontMetrics fm);
            float lineHeight = (fm.Descent - fm.Ascent) + fm.Leading;
            float spaceWidth = Font.MeasureText(" ");
            List<string> lines = new List<string>();

            List<string> BreakWord(string word, float maxWidth)
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

            string[] paragraphs = text.Split(new char[] { '\n' }, StringSplitOptions.None);
            bool firstParagraph = true;
            foreach (var paragraph in paragraphs)
            {
                if (!firstParagraph)
                {
                    if (paragraph.Length == 0)
                    {
                        lines.Add("");
                    }
                }
                firstParagraph = false;

                if (paragraph.Length == 0)
                {
                    continue;
                }

                string[] words = paragraph.Split(' ');
                string currentLine = "";
                float currentWidth = 0;

                foreach (var word in words)
                {
                    if (string.IsNullOrEmpty(word))
                        continue;

                    float wordWidth = Font.MeasureText(word);
                    if (wordWidth > bounds.Width)
                    {
                        if (!string.IsNullOrWhiteSpace(currentLine))
                        {
                            lines.Add(currentLine.TrimEnd());
                            currentLine = "";
                            currentWidth = 0;
                        }
                        var brokenParts = BreakWord(word, bounds.Width);
                        lines.AddRange(brokenParts);
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
            }

            float maxLineWidth = 0;
            foreach (var line in lines)
            {
                float lineWidth = Font.MeasureText(line);
                if (lineWidth > maxLineWidth)
                    maxLineWidth = lineWidth;
            }

            float contentHeight = lines.Count * lineHeight;
            return new Vector2(maxLineWidth, contentHeight);
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

                canvas.DrawText(text, x, y, Font, SkPaint);
                return;
            }

            canvas.SaveLayer(bounds, null);
            canvas.ClipRect(bounds);

            float gap = Transform.LocalBounds.Width / 4;
            float startX = bounds.Left + _scrollOffset;

            SkPaint.Color = _textColor.Value;
            canvas.DrawText(text, startX, y, Font, SkPaint);
            canvas.DrawText(text, startX + textWidth /* Add Offset */ + gap, y, Font, SkPaint);

            var leftAlpha = Math.Round(SmoothTransition(Math.Abs(startX - bounds.Left) / textWidth, 0, (textWidth + gap) / textWidth, 100f), 1);

            using (var maskPaint = new SKPaint())
            {
                maskPaint.BlendMode = SKBlendMode.DstIn;
                maskPaint.Shader = SKShader.CreateLinearGradient(
                    new SKPoint(bounds.Left, 0),
                    new SKPoint(bounds.Right, 0),
                    new SKColor[] { SKColors.Black.WithAlpha((byte)(255 * (1f - leftAlpha))), SKColors.Black, SKColors.Black, SKColors.Transparent },
                    new float[] { 0f, FadeLength, 1 - FadeLength, 1f },
                    SKShaderTileMode.Clamp
                );
                canvas.DrawRect(bounds, maskPaint);
            }

            canvas.Restore();
        }

        float SmoothTransition(float t, float a, float b, float power = 15f)
        {
            return (float)(1 - Math.Pow((t - a) / (b - a), 2 * power) - Math.Pow((t - b) / (b - a), 2 * power));
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (Truncation == TextTruncation.Scroll)
            {
                float textWidth = Font.MeasureText(Text);
                if (textWidth > Transform.LocalBounds.Width)
                {
                    float gap = Transform.LocalBounds.Width / 4;

                    float startX = Transform.LocalBounds.Left + _scrollOffset;
                    _speedMulti =
                        Math.Clamp(_speedMultiAdd, 0, 1) +
                        (float)Math.Round(SmoothTransition(Math.Abs(startX - Transform.LocalBounds.Left) / textWidth, 0, (textWidth + gap) / textWidth, 15f), 1);

                    // _speedMulti = RMath.Lerp(_speedMulti, 1f, (float)WindowRoot.DeltaTime);
                    _speedMultiAdd = RMath.Lerp(_speedMultiAdd, 0.1f, (float)WindowRoot.DeltaTime * 2);
                    _scrollOffset -= ScrollSpeed * RMath.Clamp(_speedMulti, 0, 1) * ((float)WindowRoot.DeltaTime * 35);

                    if (_scrollOffset < -textWidth /* Keep Offset when teleporting */ - gap)
                    {
                        _scrollOffset += textWidth /* Add the offset */ + gap;
                        _speedMultiAdd = -15;
                    }

                    if (_speedMulti >= 0.05f)
                        Invalidate();
                }
            }
        }

        protected override void DrawToSurface(SKCanvas canvas)
        {
            if (Truncation == TextTruncation.Linebreak)
                DrawTextInRect(canvas, Text, Transform.LocalBounds, TextAlign);
            else if (Truncation == TextTruncation.Elipsis)
                DrawTextInRect(canvas, GetTruncatedText(), Transform.LocalBounds, TextAlign);
            else
                DrawScrollingText(canvas, Text, Transform.LocalBounds);
        }

        public string GetTruncatedText()
        {
            if (Font.MeasureText(_text, SkPaint) <= Transform.Size.x) return _text;

            string truncatedText = "";

            for (int c = 0; c < _text.Length; c++)
            {
                string t = _text.Substring(0, c) + "...";

                if (Font.MeasureText(t, SkPaint) < Transform.Size.x)
                    truncatedText = t;
                else break;
            }

            return truncatedText;
        }

        public float GetSingleLineTextWidth()
        {
            return Font.MeasureText(Text, SkPaint);
        }

        public float GetSingleLineTextHeight()
        {
            Font.GetFontMetrics(out SKFontMetrics fm);
            float lineHeight = (fm.Descent - fm.Ascent) + fm.Leading;
            return lineHeight;
        }
    }

    public enum TextTruncation
    {
        Elipsis,
        Scroll,
        Linebreak
    }
}