using FenUISharp.Mathematics;
using FenUISharp.Objects.Buttons;
using FenUISharp.Objects.Text;
using SkiaSharp;

namespace FenUISharp.Objects
{
    internal class FSegmentedSelectionPaneSelectableButton : SelectableButton
    {
        public FText Label { get; protected set; }

        float padding = 5f;

        public FSegmentedSelectionPaneSelectableButton(FText label, Action? onClick = null) : base(onClick, null, () => new Vector2(), () => new(0, 0))
        {
            this.OnClick = onClick;

            Label = label;
            Label.SetParent(this);
            Label.OnAnyChange += RefreshLabel;
            RefreshLabel();
        }

        void RefreshLabel()
        {
            var measuredText = Label.LayoutModel.GetBoundingRect(Label.Model, SKRect.Create(0, 0, 1000, 1000));

            float width = RMath.Clamp(measuredText.Width, 15, 200);
            float height = RMath.Clamp(measuredText.Height, 20, 200);

            Label.Invalidate(Invalidation.SurfaceDirty | Invalidation.LayoutDirty);

            Label.Layout.StretchHorizontal.SetStaticState(true);
            Label.Layout.StretchVertical.SetStaticState(true);
            Label.Padding.SetStaticState(0);

            Transform.Size.SetStaticState(new Vector2(width + padding * 4f, height + padding));
        }

        public override void Render(SKCanvas canvas)
        {
            // Overriding base button render, don't need that here
            // base.Render(canvas);
        }

        public override void Dispose()
        {
            base.Dispose();
            Label.Dispose();
        }
    }
}