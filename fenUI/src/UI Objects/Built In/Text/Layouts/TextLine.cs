using FenUISharp.Objects.Text;

namespace FenUISharp
{
    public class TextLine
    {
        public List<Glyph> Glyphs { get; set; }

        public float LineWidth { get; set; }
        public float LineHeight { get; set; }

        public TextLine()
        {
            Glyphs = new();
        }
    }
}