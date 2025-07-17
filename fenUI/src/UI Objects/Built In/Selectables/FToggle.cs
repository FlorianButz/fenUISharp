using FenUISharp.Behavior;
using FenUISharp.Mathematics;
using FenUISharp.Objects.Buttons;
using FenUISharp.States;
using SkiaSharp;

namespace FenUISharp.Objects
{
    public class FToggle : SelectableButton, IStateListener
    {
        public State<SKColor> CheckColor { get; set; }

        public FToggle(Func<Vector2>? position = null, Func<Vector2>? size = null) : base(position: position, size: size ?? (() => new(20, 20)))
        {
            CheckColor = new(() => SKColors.White, this);

            Padding.SetStaticState(2);

            Transform.Size.SetStaticState(new(20, 20));

            // Creating checkmark

            FImage image = new(() => Resources.GetImage("fenui-builtin-check"));
            image.Transform.Size.SetResponsiveState(() => new(Layout.ClampSize(Transform.Size.CachedValue).x - 4, Layout.ClampSize(Transform.Size.CachedValue).y - 4));
            image.Transform.LocalPosition.SetStaticState(new(0.5f, 1f));
            image.Enabled.SetResponsiveState(() => IsSelected);
            image.SetParent(this);
        }
        
        public override void Dispose()
        {
            base.Dispose();

            CheckColor.Dispose();
        }
    }
}