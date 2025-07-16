using FenUISharp.Behavior;
using FenUISharp.Materials;
using FenUISharp.Mathematics;
using FenUISharp.Objects.Text;
using FenUISharp.States;
using SkiaSharp;

namespace FenUISharp.Objects.Buttons
{
    public class FSimpleButton : Button, IStateListener
    {
        public FText Label { get; protected set; }

        float padding = 7.5f;
        float minWidth = 0;
        float maxWidth = 0;

        public FSimpleButton(FText label, Action? onClick = null, Func<Vector2>? position = null, float minWidth = 25, float maxWidth = 175) : base(onClick, position, () => new Vector2(0, 0))
        {
            this.OnClick = onClick;

            this.maxWidth = RMath.Clamp(maxWidth, minWidth, float.MaxValue);
            this.minWidth = RMath.Clamp(minWidth, 0, this.maxWidth);

            Label = label;
            Label.SetParent(this);
            Label.OnAnyChange += RefreshLabel;
            RefreshLabel();
        }

        void RefreshLabel()
        {
            var measuredText = Label.LayoutModel.GetBoundingRect(Label.Model, SKRect.Create(0, 0, maxWidth, 1000));

            float width = RMath.Clamp(measuredText.Width, minWidth, maxWidth);
            float height = RMath.Clamp(measuredText.Height, 20, 100);

            Label.Invalidate(Invalidation.SurfaceDirty | Invalidation.LayoutDirty);

            Label.Layout.StretchHorizontal.SetStaticState(true);
            Label.Layout.StretchVertical.SetStaticState(true);
            Label.Padding.SetStaticState(0);

            Transform.Size.SetStaticState(new Vector2(width + padding * 2.5f, height + padding * 0.5f));
        }

        public override void Dispose()
        {
            base.Dispose();
            Label.Dispose();
        }
    }
}