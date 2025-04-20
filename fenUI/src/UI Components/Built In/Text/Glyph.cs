using FenUISharp.Components.Text.Model;
using SkiaSharp;

namespace FenUISharp.Components.Text
{
    public class Glyph
    {
        public char Character { get; set; }
        public SKPoint Position { get; set; }
        public SKSize Size { get; set; }

        public SKPoint Anchor { get; set; }
        public SKSize Scale { get; set; }
        public TextStyle Style { get; set; }

        public SKRect Bounds => SKRect.Create(Position.X - Size.Width / 2, Position.Y - Size.Height, Size.Width, Size.Height);

        public Glyph(char character, SKPoint position, SKSize scale, SKPoint anchor, TextStyle style, SKSize size)
        {
            Character = character;
            Position = position;
            Anchor = anchor;
            Scale = scale;
            Style = style;
            Size = size;
        }
    }
}