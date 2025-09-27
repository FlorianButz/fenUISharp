using FenUISharp.Mathematics;
using FenUISharp.Objects.Text;
using SkiaSharp;

namespace FenUISharp.Objects.Buttons
{
    public class FContextMenuButton : Button
    {
        public FText Label { get; protected set; }

        float padding = 7.5f;

        public FContextMenuButton(FText label, Action? onClick = null) : base(onClick, () => new(), () => new(0, 20))
        {
            this.OnClick = onClick;

            Layout.StretchHorizontal.SetStaticState(true);

            Label = label;
            Label.SetParent(this);
            Label.OnAnyChange += RefreshLabel;

            RefreshLabel();
        }

        void RefreshLabel()
        {
            var measuredText = Label.LayoutModel.GetBoundingRect(Label.Model, SKRect.Create(0, 0, 10000, 1000));
            float height = RMath.Clamp(measuredText.Height, 20, 100);

            Label.Invalidate(Invalidation.SurfaceDirty | Invalidation.LayoutDirty);

            Label.Layout.StretchHorizontal.SetStaticState(true);
            Label.Layout.StretchVertical.SetStaticState(true);
            Label.Padding.SetStaticState(0);

            Transform.Size.SetStaticState(new Vector2(0, height + padding * 0.5f));
            Invalidate(Invalidation.LayoutDirty);
        }

        public override void Dispose()
        {
            base.Dispose();
            Label.Dispose();
        }
    }
}