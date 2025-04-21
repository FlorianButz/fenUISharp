using FenUISharp.Components.Text.Layout;
using SkiaSharp;

namespace FenUISharp.Components.Text.Model
{
    public class TextModelFactory
    {
        public static TextModel CreateBasic(string text)
        {
            TextStyle style = new();
            TextAlign align = new() { HorizontalAlign = TextAlign.AlignType.Middle, VerticalAlign = TextAlign.AlignType.Middle };

            return new(new List<TextSpan>() { new TextSpan(text, style) }, align, FTypeface.Default);
        }

        public static TextModel CreateTest(string text)
        {
            TextAlign align = new() { HorizontalAlign = TextAlign.AlignType.Middle, VerticalAlign = TextAlign.AlignType.Middle };

            List<TextSpan> spans = new();
            var splitText = text.Split(' ');

            foreach (var part in splitText)
            {
                TextStyle style = new();
                style.Color = new SKColor(
                    (byte)(Random.Shared.NextSingle() * 255),
                    (byte)(Random.Shared.NextSingle() * 255),
                    (byte)(Random.Shared.NextSingle() * 255),
                    255);

                if(Random.Shared.NextSingle() > 0.5f)
                    style.Weight = SKFontStyleWeight.Bold;

                if(Random.Shared.NextSingle() > 0.5f)
                    style.Slant = SKFontStyleSlant.Italic;

                if(Random.Shared.NextSingle() > 0.5f)
                    style.Underlined = true;

                spans.Add(new TextSpan(part + ' ', style));
            }

            return new(spans, align, FTypeface.Default);
        }
    }
}