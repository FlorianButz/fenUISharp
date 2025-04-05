using System.Reflection;
using FenUISharp;
using FenUISharp.Themes;
using SkiaSharp;

namespace FenUISharpTest1
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            FenUI.Init();
            FenUI.SetupAppModel("FlorianButz.fenUI");

            // TransparentWindow popup = new TransparentWindow("Test 1", "testClass", Window.RenderContextType.Software, new Vector2(100, 150), null);
            // popup.SystemDarkMode = true;
            // popup.SetWindowVisibility(true);

            // var panel = new FPanel(popup, new Vector2(0, 0), new Vector2(0, 0), 5, new ThemeColor(SKColors.Transparent));
            // panel.Transform.StretchVertical = true;
            // panel.Transform.StretchHorizontal = true;
            // panel.Transform.MarginHorizontal = 5;
            // panel.Transform.MarginVertical = 0;
            
            // var layout = new StackContentComponent(panel, StackContentComponent.ContentStackType.Vertical, StackContentComponent.ContentStackBehavior.Scroll);
            // layout.Pad = 15;
            
            // for(int i = 0; i < 5; i++){
            //     var btn1 = new FSimpleButton(popup, new Vector2(0, 0), "Click this!");
            //     btn1.Transform.SetParent(layout.parent.Transform);
            //     btn1.Transform.StretchHorizontal = true;
            //     btn1.Transform.MarginHorizontal = 0;
            // }

            // popup.OnFocusLost += () => {
            //     popup.DisposeAndDestroyWindow();
            // };

            // popup.BeginWindowLoop();

            NativeWindow window = new NativeWindow("Test 1", "testClass", Window.RenderContextType.DirectX, windowSize: new Vector2(800, 400));
            // TransparentWindow window = new TransparentWindow("Test 1", "testClass", Window.RenderContextType.Software, new Vector2(250, 250), null);

            // new Thread(() => {
            //     var t = new TransparentWindow("Test 1", "testClass", Window.RenderContextType.Software, new Vector2(400, 200), null);
            //     new FSimpleButton(t, new Vector2(0, 25), "Test Text, click!", () => Console.WriteLine("test3"),
            //     color: window.WindowThemeManager.GetColor(t => t.Primary), textColor: window.WindowThemeManager.GetColor(t => t.OnPrimary));
            //     t.SetWindowVisibility(true);
            //     t.BeginWindowLoop();
            // }).Start();

            window.RefreshRate = 60;
            window.SystemDarkMode = true;
            window.AllowResizing = false;

            window.TrayMouseAction += (MouseInputCode c) => {
                if(c.state == (int)MouseInputState.Up &&
                c.button == (int)MouseInputButton.Left) {
                    window.SetWindowVisibility(true);
                }
            };

            window.MouseAction += (MouseInputCode c) => {
            };

            window.SetTrayIcon("icons/TrayIcon.ico", "Test");
            window.SetWindowIcon("icons/TrayIcon.ico");
            window.SetWindowVisibility(true);
            // window.SetAlwaysOnTop(true);

            var panel = new FPanel(window, new Vector2(0, 0), new Vector2(0, 0), 5, new ThemeColor(SKColors.Transparent));
            panel.Transform.StretchVertical = true;
            panel.Transform.StretchHorizontal = true;

            panel.BorderColor = window.WindowThemeManager.GetColor(t => t.Surface);
            panel.BorderSize = 1.5f;
            panel.CornerRadius = 10;
            panel.Invalidate();
            var layout = new StackContentComponent(panel, StackContentComponent.ContentStackType.Vertical, StackContentComponent.ContentStackBehavior.Scroll);
            layout.Pad = 15;

            var label = new FLabel(window, "Lorem ipsum dolor sit amet, consetetur sadipscing elitr,\n sed diam nonumy\n\n\n\n eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet.",
                new Vector2(0, 0), new Vector2(400, 0), truncation: TextTruncation.Linebreak);
            label.FitVerticalToContent = true;

            label.Transform.SetParent(panel.Transform);

            var btn2 = new FSimpleButton(window, new Vector2(0, 25), "Test Text, click!", () => label.SetText("Test awdpojawpdjawdpojawdpojawpdojawpd awpdjawpodpa apod apwkodpaowdkpoawdk " + Random.Shared.Next()),
                color: window.WindowThemeManager.GetColor(t => t.Primary), textColor: window.WindowThemeManager.GetColor(t => t.OnPrimary));
            btn2.Transform.Alignment = new Vector2(0.5f, 0f);
            btn2.Transform.SetParent(panel.Transform);

            panel.Transform.UpdateLayout();

            var btn = new FSimpleButton(window, new Vector2(0, 25), "Test Text, click!", () => Console.WriteLine("test3"),
                color: window.WindowThemeManager.GetColor(t => t.Primary), textColor: window.WindowThemeManager.GetColor(t => t.OnPrimary));
            btn.Transform.Alignment = new Vector2(0.5f, 0f);

            window.BeginWindowLoop();
        }
    }
}