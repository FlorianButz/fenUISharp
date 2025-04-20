using FenUISharp.Components.Text.Layout;
using SkiaSharp;

namespace FenUISharp.Components.Text.Model
{
    public class TextModel
    {
        public List<TextSpan> TextParts { get; init; }
        public FTypeface Typeface { get; init; }
        public TextAlign Align { get; init; }

        public TextModel(List<TextSpan> textParts, TextAlign align, FTypeface? typeface = null)
        {
            Typeface = typeface ?? FTypeface.Default;
            Align = align;
            TextParts = textParts;
        }

        public TextModel(TextModel copy)
        {
            TextParts = copy.TextParts;
            Typeface = copy.Typeface;
            Align = copy.Align;
        }
    }
}