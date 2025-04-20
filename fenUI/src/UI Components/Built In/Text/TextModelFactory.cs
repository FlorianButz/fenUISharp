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
    }
}