using FenUISharp.Objects.Text.Layout;
using FenUISharp.Objects.Text.Model;
using SkiaSharp;

namespace FenUISharp.Objects.Text.Layout
{
    public class LayoutProcessor : TextLayout
    {
        public TextLayout InnerLayout { get; set; }

        public LayoutProcessor(FText parent, TextLayout innerLayout) : base(parent)
        {
            InnerLayout = innerLayout;
        }

        public override List<Glyph> ProcessModel(TextModel model, SKRect bounds)
        {
            return InnerLayout.ProcessModel(model, bounds);
        }
    }
}