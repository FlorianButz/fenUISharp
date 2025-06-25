using FenUISharp.Components.Text.Layout;
using FenUISharp.Themes;
using SkiaSharp;

namespace FenUISharp.Objects.Text.Model
{
    public class TextModelFactory
    {
        public static TextModel CreateBasic(string text, float textSize = 14, bool bold = false, bool italic = false, Func<SKColor>? textColor = null, TextAlign? align = null)
        {
            TextStyle style = new()
            {
                FontSize = textSize,
                Color = textColor ?? (() => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.OnBackground),
                Weight = bold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
                Slant = italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright
            };
            TextAlign algn = align ?? new() { HorizontalAlign = TextAlign.AlignType.Middle, VerticalAlign = TextAlign.AlignType.Middle };

            return new(new List<TextSpan>() { new TextSpan(text, style) }, algn, FTypeface.Default);
        }

        public static TextModel CopyBasic(TextModel old, float? textSize = null, bool? bold = null, bool? italic = null, Func<SKColor>? textColor = null, TextAlign? align = null)
        {
            List<TextSpan> spans = new();

            foreach (var span in old.TextParts)
            {
                span.Style.Color = textColor ?? span.Style.Color;
                span.Style.Weight = (bold != null) ? (bold.Value ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal) : span.Style.Weight;
                span.Style.Slant = (italic != null) ? (italic.Value ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright) : span.Style.Slant;
                span.Style.FontSize = textSize ?? span.Style.FontSize;
                spans.Add(span);
            }

            return new(spans, align ?? old.Align, FTypeface.Default);
        }

        public static TextModel CreateTest(string text)
        {
            TextAlign align = new() { HorizontalAlign = TextAlign.AlignType.Middle, VerticalAlign = TextAlign.AlignType.Middle };

            List<TextSpan> spans = new();
            var splitText = text.Split(' ');

            foreach (var part in splitText)
            {
                TextStyle style = new();
                style.Color = (() => new SKColor(
                    (byte)(Random.Shared.NextSingle() * 255),
                    (byte)(Random.Shared.NextSingle() * 255),
                    (byte)(Random.Shared.NextSingle() * 255),
                    255));

                if (Random.Shared.NextSingle() > 0.5f)
                    style.Weight = SKFontStyleWeight.Bold;

                if (Random.Shared.NextSingle() > 0.5f)
                    style.Slant = SKFontStyleSlant.Italic;

                if (Random.Shared.NextSingle() > 0.5f)
                    style.Underlined = true;

                spans.Add(new TextSpan(part + ' ', style));
            }

            return new(spans, align, FTypeface.Default);
        }
    }
}