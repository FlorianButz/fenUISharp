
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


            var fp = new FPopupPanel(() => new(175, 100));

            var test = new FText(TextModelFactory.CreateBasic("Lorem ipsum dolor sit amet, consetetur sadipscing elitr"));
            // var test = new FImage(() => Resources.GetImage("test-img"));
            test.Layout.StretchHorizontal.SetStaticState(true);
            test.Layout.StretchVertical.SetStaticState(true);
            test.Layout.MarginHorizontal.SetStaticState(15);
            test.Layout.MarginVertical.SetStaticState(15);
            test.SetParent(fp);
            
            var toggle = new FRoundToggle();
            toggle.SetParent(fp);
            new FRoundToggle().SetParent(fp);
            new FRoundToggle().SetParent(fp);
            new FRoundToggle().SetParent(fp);
            new FRoundToggle().SetParent(fp);

            var s = new FSlider(position: () => new(0, 100));
            s.SnappingProvider = (x) => MathF.Round(x * 10) / 10;
            s.Transform.Size.SetStaticState(new(300, 5));
            
            var sDisplay = new FText(TextModelFactory.CreateBasic("Slider Wert: 0"), position: () => new(0, 200), size: () => new(200, 200));
            sDisplay.LayoutModel = new ParticleExplosionLayoutProcessor(sDisplay, new WrapLayout(sDisplay)) { ExplosionRadius = 200, VelocityRandomness = 75, ParticleEndSize = 16, ParticleStartSize = 10, PreDelay = -1 };
            sDisplay.Padding.SetStaticState(200);
            s.OnValueChanged += (x) => sDisplay.Model = TextModelFactory.CreateBasic("Slider Wert: " + x);
            
            new FImage(() => Resources.GetImage("test-img"), size: () => new(150 * (s.Value + 1), 150), position: () => new(MathF.Sin(FContext.Time / 2) * 300, 0));

            var btn = new FSimpleButton(new Text.FText(TextModelFactory.CreateBasic("Open Pop-up!")), position: () => new(new(0,0)));
            btn.OnClick += () => fp.Show(() => btn.Transform.LocalToGlobal(btn.Transform.LocalPosition.CachedValue));

            return new List<UIObject>() { btn };
        }
    }
}