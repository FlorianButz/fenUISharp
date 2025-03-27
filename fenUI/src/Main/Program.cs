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
            NativeWindow window = new NativeWindow("Test 1", "testClass", Window.RenderContextType.Software, windowSize: new Vector2(800, 400));
            window.SystemDarkMode = true;
            window.AllowResizing = true;
            window.UseMica = true;
            window.IsMicaMainWindow = false;

            // window.SetTrayIcon("icons/TrayIcon.ico", "Test");
            window.SetWindowIcon("icons/TrayIcon.ico");
            window.SetWindowVisibility(true);
            // window.SetAlwaysOnTop(true);

            var panel = new FPanel(window, new Vector2(0, 0), new Vector2(150, 100), 5, new ThemeColor(SKColors.Transparent));
            panel.BorderColor = window.WindowThemeManager.GetColor(t => t.Surface);
            panel.BorderSize = 1.5f;
            panel.CornerRadius = 10;
            panel.Invalidate();
            var layout = new StackContentComponent(panel, StackContentComponent.ContentStackType.Horizontal, StackContentComponent.ContentStackBehavior.Scroll);
            panel.components.Add(layout);
            window.AddUIComponent(panel);

            for (int i = 0; i < 8; i++)
            {
                var label = new FLabel(window, "abcdefghijklmnopqrstuvwxyz", new Vector2(0, 0), new Vector2(100, 10));
                label.transform.SetParent(panel.transform);
                window.AddUIComponent(label);
            }

            var btn2 = new FSimpleButton(window, new Vector2(0, 25), "Test Text, click!", () => Console.WriteLine("test"),
                color: window.WindowThemeManager.GetColor(t => t.Primary), textColor: window.WindowThemeManager.GetColor(t => t.OnPrimary));
            btn2.transform.alignment = new Vector2(0.5f, 0f);
            btn2.transform.SetParent(panel.transform);
            window.AddUIComponent(btn2);

            // panel.transform.UpdateLayout("");
            layout.FullUpdateLayout();

            var btn = new FSimpleButton(window, new Vector2(0, 25), "Test Text, click!", () => Console.WriteLine("test"),
                color: window.WindowThemeManager.GetColor(t => t.Primary), textColor: window.WindowThemeManager.GetColor(t => t.OnPrimary));
            btn.transform.alignment = new Vector2(0.5f, 0f);
            window.AddUIComponent(btn);

            window.BeginWindowLoop();
        }
    }
}