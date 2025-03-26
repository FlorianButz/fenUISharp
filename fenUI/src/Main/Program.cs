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
            NativeWindow window = new NativeWindow("Test 1", "testClass", Window.RenderContextType.Software);
            window.SystemDarkMode = true;
            window.AllowResizing = true;
            // window.UseMica = true;
            // window.SetTrayIcon("icons/TrayIcon.ico", "Test");
            window.SetWindowIcon("icons/TrayIcon.ico");
            window.SetWindowVisibility(true);
            window.SetAlwaysOnTop(true);

            var panel = new FPanel(window, new Vector2(0, 0), new Vector2(225, 75), 5, new ThemeColor(SKColors.Transparent));
            panel.BorderColor = window.WindowThemeManager.GetColor(t => t.Surface);
            panel.BorderSize = 1;
            panel.CornerRadius = 10;
            window.AddUIComponent(panel);

            var label = new FLabel(window, "AWODJAWPODJPAOWIDJWPOJDPOAJDPOWAIJDPOJAWPODJWOPAWJDP", new Vector2(0, 0), new Vector2(150, 20), truncation: TextTruncation.Scroll);
            label.transform.boundsPadding.SetValue(label, 15, 25);
            label.TextAlign = SKTextAlign.Center;
            window.AddUIComponent(label);

            var btn = new FSimpleButton(window, new Vector2(0, -100), "Test Text, click!", () => Console.WriteLine("test"),
                color: window.WindowThemeManager.GetColor(t => t.Primary), textColor: window.WindowThemeManager.GetColor(t => t.OnPrimary));
            window.AddUIComponent(btn);

            window.BeginWindowLoop();
        }
    }
}