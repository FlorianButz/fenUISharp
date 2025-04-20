using FenUISharp.Components.Text.Model;

namespace FenUISharp.Components.Text.Layout
{
    public abstract class TextLayout
    {
        protected FText Parent { get; init; }

        public TextLayout(FText Parent)
        {
            this.Parent = Parent;
        }

        public abstract List<Glyph> ProcessModel(TextModel model);
    }
}