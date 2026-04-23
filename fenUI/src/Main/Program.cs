using System.Reflection;
using FenUISharp;
using FenUISharp.AnimatedVectors;
using FenUISharp.Materials;
using FenUISharp.Mathematics;
using FenUISharp.Native;
using FenUISharp.Objects;
using FenUISharp.Objects.Buttons;
using FenUISharp.Objects.Text;
using FenUISharp.Objects.Text.Layout;
using FenUISharp.Objects.Text.Model;
using FenUISharp.RuntimeEffects;
using FenUISharp.Themes;
using SkiaSharp;

namespace FenUISharp
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            FenUI.Init();
            FenUI.SetupAppModel("FlorianButz.fenUI");

            FWindow testWindow = new FTransparentWindow("Test Window!", "testWindow", null, null);
            // testWindow.Properties.ExcludeFromPeek = true;
            // testWindow.Properties.UseMica = true;
            // testWindow.Properties.MicaBackdropType = Native.MicaBackdropType.MainWindow;
            testWindow.Properties.VisibleInTaskbar = false;
            testWindow.Properties.AlwaysOnTop = true;

            string iconPath = Resources.ExtractResourceToTempFile<FenUI>($"{FenUI.ResourceLibName}.icons.fenui.ico");
            testWindow.Properties.SetWindowIcon(iconPath);
            testWindow.Properties.CreateTrayIcon(iconPath, "Test Tooltip!");

            testWindow.WithView(new BasicView());

            testWindow.Properties.AllowResize = false;

            testWindow.Properties.ShowWindow(Native.ShowWindowCommand.SW_SHOW);
            testWindow.BeginWindowLoop();

            FenUI.EnableDebugFunctions();
            FenUI.Demo();
        }
    }

    class BasicView : View
    {
        public override List<UIObject> Create()
        {
            var btn = new FSimpleButton(new Objects.Text.FText(TextModelFactory.CreateBasic("Test! Click me!")));
            
            var fps = new FText(TextModelFactory.CreateBasic(""));
            fps.Transform.LocalPosition.SetStaticState(new Vector2(0, -250));
            fps.Transform.Size.SetStaticState(new Vector2(700, 200));

            var p = new FPopupPanel(() => new(125, 75), true, true, true);
            p.CornerRadius.SetStaticState(50);
            btn.OnClick += () =>
            {
                p.Show(() => btn.Transform.LocalToGlobal(Vector2.Zero));
            };

            var s = new TestObject();

            var panel = new FPanel();
            panel.RenderMaterial.SetStaticState(new GlassMaterial(() => panel.Composition.GrabBehindPlusBuffer(panel.Shape.GlobalBounds, 1)));

            return new() { btn, s, panel };
        }
    }
}