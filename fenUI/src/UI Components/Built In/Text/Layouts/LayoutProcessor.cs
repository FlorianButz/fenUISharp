
using FenUISharp.Components.Text.Model;

namespace FenUISharp.Components.Text.Layout
{
    public class LayoutProcessor : TextLayout
    {
        public TextLayout InnerLayout { get; set; }

        public LayoutProcessor(FText parent, TextLayout innerLayout) : base(parent)
        {
            InnerLayout = innerLayout;
        }

        public override List<Glyph> ProcessModel(TextModel model)
        {
            return InnerLayout.ProcessModel(model);
        }
    }
}