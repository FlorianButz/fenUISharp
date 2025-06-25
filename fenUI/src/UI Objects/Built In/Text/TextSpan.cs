namespace FenUISharp.Objects.Text.Model
{
    public class TextSpan
    {
        public string Content { get; init; }
        public TextStyle Style { get; init; }
        public float CharacterSpacing { get; init; } = 0;

        public TextSpan(string content, TextStyle style)
        {
            this.Content = content;
            this.Style = style;
        }
    }
}