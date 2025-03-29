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
            NativeWindow window = new NativeWindow("Test 1", "testClass", Window.RenderContextType.DirectX, windowSize: new Vector2(800, 400));
            // OverlayWindow window = new OverlayWindow("Test 1", "testClass", Window.RenderContextType.Software);
            
            window.RefreshRate = 60;
            window.SystemDarkMode = true;
            window.AllowResizing = true;
            
            // window.SetTrayIcon("icons/TrayIcon.ico", "Test");
            window.SetWindowIcon("icons/TrayIcon.ico");
            window.SetWindowVisibility(true);
            // window.SetAlwaysOnTop(true);

            var panel = new FPanel(window, new Vector2(0, 0), new Vector2(500, 200), 5, new ThemeColor(SKColors.Transparent));
            panel.BorderColor = window.WindowThemeManager.GetColor(t => t.Surface);
            panel.BorderSize = 1.5f;
            panel.CornerRadius = 10;
            panel.Invalidate();
            var layout = new StackContentComponent(panel, StackContentComponent.ContentStackType.Vertical, StackContentComponent.ContentStackBehavior.Scroll);
            layout.Pad = 15;
            panel.components.Add(layout);
            window.AddUIComponent(panel);

            for (int i = 0; i < 2; i++)
            {
                var label = new FLabel(window, "Lorem ipsum dolor sit amet, consetetur sadipscing elitr,\n sed diam nonumy\n\n\n\n eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet.",
                    new Vector2(0, 0), new Vector2(400, 0), truncation: TextTruncation.Linebreak);
                label.FitVerticalToContent = true;
                // label.transform.stretchHorizontal = true;

                label.transform.SetParent(panel.transform);
                window.AddUIComponent(label);

                var btn2 = new FSimpleButton(window, new Vector2(0, 25), "Test Text, click!", () => label.SetText("Test awdpojawpdjawdpojawdpojawpdojawpd awpdjawpodpa apod apwkodpaowdkpoawdk " + Random.Shared.Next()),
                    color: window.WindowThemeManager.GetColor(t => t.Primary), textColor: window.WindowThemeManager.GetColor(t => t.OnPrimary));
                btn2.transform.alignment = new Vector2(0.5f, 0f);
                btn2.transform.SetParent(panel.transform);
                window.AddUIComponent(btn2);
            }

            panel.transform.UpdateLayout();
            // layout.FullUpdateLayout();

            // label.SetText("taowd");

            var btn = new FSimpleButton(window, new Vector2(0, 25), "Test Text, click!", () => Console.WriteLine("test3"),
                color: window.WindowThemeManager.GetColor(t => t.Primary), textColor: window.WindowThemeManager.GetColor(t => t.OnPrimary));
            btn.transform.alignment = new Vector2(0.5f, 0f);
            window.AddUIComponent(btn);

            window.BeginWindowLoop();
        }
    }
}