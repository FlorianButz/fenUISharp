
using FenUISharp.Behavior;
using FenUISharp.Components;
using FenUISharp.Components.Text.Layout;
using FenUISharp.Mathematics;
using FenUISharp.Objects.Buttons;
using FenUISharp.Objects.Text;
using FenUISharp.Objects.Text.Layout;
using FenUISharp.Objects.Text.Model;
using FenUISharp.States;
using FenUISharp.WinFeatures;
using SkiaSharp;

namespace FenUISharp.Objects
{
    public class TestViewPane : View
    {
        public override List<UIObject> Create()
        {
            // var f = new FPanel(size: () => new(750, 500));
            // // f.Layout.Alignment.SetStaticState(new(0, 0));

            // for (int i = 0; i < 1; i++)
            // {
            //     var panel = new FPanel(size: () => new(150, 75));
            //     panel.SetParent(f);
            //     panel.PanelColor.SetStaticState(new(
            //         (byte)(Random.Shared.NextSingle() * 255),
            //         (byte)(Random.Shared.NextSingle() * 255),
            //         (byte)(Random.Shared.NextSingle() * 255),
            //         255
            //     ));

            //     var pic = new FImage(() => Resources.GetImage("test-img"), size: () => new(200, 100));
            //     pic.SetParent(f);
            //     pic.CornerRadius.Value = () => 100;
            // }

            // var slider = new FSlider();
            // slider.SetParent(f);

            // var p = new FRadialProgressBar(() => slider.Value);
            // p.SetParent(f);
            // // p.Value.Subscribe((x) => Console.WriteLine(x));
            // // slider.OnValueChanged += (x) => p.Value.SetStaticState(slider.Value);

            // State<float> test = new(() => slider.Value, (x) => Console.WriteLine("D"+x));
            // // FContext.GetCurrentWindow().OnPostUpdate += () => Console.WriteLine(slider.Value);

            // // var fp = new FPopupPanel();
            // // fp.Transform.LocalPosition.SetStaticState(new(235, -100));
            // // fp.Transform.Size.SetStaticState(new(220, 150));
            // // fp.Show(() => FContext.GetCurrentWindow().ClientMousePosition);


            // var l = new StackContentComponent(f, StackContentComponent.ContentStackType.Vertical, StackContentComponent.ContentStackBehavior.Scroll);
            // l.ContentFade = true;
            // l.EnableEdgeBlur = true;
            // l.Pad.SetStaticState(25);
            // l.Gap.SetStaticState(20);
            // l.FullUpdateLayout();

            // return new();

            // var v = new FToggle();
            // new FRoundToggle().Transform.LocalPosition.SetStaticState(new(0, 100));

            // var fp = new FPopupPanel(() => new(150, 125));
            // fp.CornerRadius.SetStaticState(20);

            // // var toggle = new FRoundToggle();
            // // toggle.SetParent(fp);
            // // new FRoundToggle().SetParent(fp);

            // var col = new FColorPicker();
            // col.Transform.LocalPosition.SetStaticState(new(0, -150));
            // col.Layout.StretchHorizontal.SetStaticState(true);
            // col.Layout.StretchVertical.SetStaticState(true);
            // col.Layout.MarginHorizontal.SetStaticState(7.5f);
            // col.Layout.MarginVertical.SetStaticState(7.5f);
            // col.SetParent(fp);

            // var patch = new FColorPatch(size: () => new(200, 150));
            // patch.OnColorUpdated += (x) => Console.WriteLine("Color update: " + x);
            // patch.OnUserColorUpdated += (x) => Console.WriteLine("Color user update: " + x);

            // var n = new FNumericScroller(new FText(TextModelFactory.CreateBasic("", 14, bold: true)));
            // // n.Label.LayoutModel = new WiggleCharsLayoutProcessor(n.Label, new WrapLayout(n.Label));
            // n.MinValue.SetStaticState(0);
            // n.MaxValue.SetStaticState(2);
            // n.Step.SetStaticState(0.1f);
            // n.Value = 5;
            // n.FormatProvider.SetStaticState("C");

            // var slider = new FSlider(() => new(0, 70));
            // slider.MinValue.SetStaticState(0);
            // slider.MaxValue.SetStaticState(2);
            // slider.SnappingInterval = 0.1f;

            // slider.OnUserValueChanged += (x) => n.Value = x;
            // n.OnUserValueChanged += (x) => slider.Value = x;

            new FColorPatch(SKColors.Magenta, () => new(0, 150));

            // var v = new FToggle();
            // v.Transform.LocalPosition.SetStaticState(new(0, 100));
            // v.Transform.Size.SetStaticState(new(100, 100));

            // var s = new FSlider(position: () => new(0, 100));
            // s.SnappingInterval = 0.5f;
            // s.ExtraHotspots.Add(0.25f);
            // s.Transform.Size.SetStaticState(new(300, 5));

            // var sDisplay = new FText(TextModelFactory.CreateBasic("Slider Wert: 0"), position: () => new(0, 200), size: () => new(200, 200));
            // // sDisplay.LayoutModel = new ParticleExplosionLayoutProcessor(sDisplay, new WrapLayout(sDisplay)) { ExplosionRadius = 200, VelocityRandomness = 75, ParticleEndSize = 16, ParticleStartSize = 10, PreDelay = -1 };
            // // sDisplay.Padding.SetStaticState(200);
            // s.OnValueChanged += (x) => sDisplay.Model = TextModelFactory.CreateBasic("Slider Wert: " + x);

            // new FImage(() => Resources.GetImage("test-img"), size: () => new(150 * (s.Value + 1), 150), position: () => new(MathF.Sin(FContext.Time / 2) * 300, 0));

            // var btn = new FSimpleButton(new Text.FText(TextModelFactory.CreateBasic("Change Color")), position: () => new(new(-65, 50)));
            // btn.OnClick += () => fp.Show(() => btn.Transform.LocalToGlobal(btn.Transform.LocalPosition.CachedValue));
            // btn.Layout.Alignment.SetStaticState(new(1, 0));
            // // btn.OnClick += () => col.PickedColor = SKColors.Blue;

            // new FImage(() => Resources.GetImage("test-img"), position: () => new(0, -170)).TintColor.SetResponsiveState(() => col.PickedColor);

            return new List<UIObject>() { };
        }
    }
}