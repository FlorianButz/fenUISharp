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

            NativeWindow window = new NativeWindow("Test 1", "testClass", Window.RenderContextType.DirectX, windowSize: new Vector2(1200, 800));
            window.SystemDarkMode = true;
            window.SetTrayIcon("icons/TrayIcon.ico", "Test");
            window.SetWindowIcon("icons/TrayIcon.ico");

            window.TrayMouseAction += (MouseInputCode c) =>
            {
                if (c.state == (int)MouseInputState.Up &&
                c.button == (int)MouseInputButton.Left)
                {
                    window.SetWindowVisibility(true);
                }
            };

            var panel = new FPanel(window, new Vector2(0, 0), new Vector2(0, 0), 5, new ThemeColor(SKColors.Transparent));
            panel.Transform.StretchVertical = true;
            panel.Transform.StretchHorizontal = true;
            panel.Transform.MarginHorizontal = 100;
            panel.Transform.MarginVertical = 150;

            panel.BorderColor = window.WindowThemeManager.GetColor(t => t.Surface);
            panel.BorderSize = 1f;
            panel.CornerRadius = 10;

            var layout = new StackContentComponent(panel, StackContentComponent.ContentStackType.Vertical, StackContentComponent.ContentStackBehavior.Scroll);
            layout.Pad = 15;
            // layout.ContentClipBehaviorProvider = new ScaleContentClipBehavior() { ClipScale = new Vector2(1, 1) * 0.85f, ClipStart = 15, ClipLength = 100 };
            layout.ContentClipBehaviorProvider = new RandomContentClipBehavior(layout);

            var panel2 = new FPanel(window, new Vector2(0, 0), new Vector2(200, 100), 15, window.WindowThemeManager.GetColor(t => t.OnBackground));
            panel2.UseSquircle = true;
            panel2.Transform.SetParent(panel.Transform);

            for(int i = 0; i < 25; i++){
                var label = new FLabel(window, "Lorem ipsum dolor sit amet.",
                    new Vector2(0, 0), new Vector2(400, 0), truncation: TextTruncation.Linebreak);
                label.FitVerticalToContent = true;
                label.Transform.SetParent(panel.Transform);
            }

            var btn2 = new FSimpleButton(window, new Vector2(0, 25), "Test Text, click!",
                color: window.WindowThemeManager.GetColor(t => t.Primary), textColor: window.WindowThemeManager.GetColor(t => t.OnPrimary));
            btn2.Transform.Alignment = new Vector2(0.5f, 0f);
            btn2.Transform.SetParent(panel.Transform);

            // var btn = new FSimpleButton(window, new Vector2(0, 25), "Test Text, click!", () => Console.WriteLine("test3"),
            //     color: window.WindowThemeManager.GetColor(t => t.Primary), textColor: window.WindowThemeManager.GetColor(t => t.OnPrimary));
            // btn.Transform.Alignment = new Vector2(0.5f, 0f);
            
            var img = new FImage(window, new Vector2(0, 0), new Vector2(50, 50), Resources.GetImage("test-img"), 15);
            img.Transform.SetParent(panel.Transform);

            panel.Transform.UpdateLayout();

            var panel3 = new FPanel(window, new Vector2(0, 0), new Vector2(100, 100), 15, window.WindowThemeManager.GetColor(t => t.Primary));
            int time = 0;
            window.OnUpdate += () => {
                time++;
                var locPos = panel3.Transform.LocalPositionExcludeBounds;
                locPos = new Vector2(1, 1) * (float)(Math.Sin((float)time / 5) * 500);
                panel3.Transform.LocalPosition = locPos;
                panel3.GloballyInvalidated = true;
            };

            window.SetWindowVisibility(true);
            window.BeginWindowLoop();
        }
    }
}