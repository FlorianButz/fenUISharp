using System.Reflection;
using FenUISharp;
using SkiaSharp;

using Windows.Media.Control;
using Windows.Storage.Streams;

namespace FenUISharpTest1
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            FResources.LoadDefault();
            new FWindowsMediaControls();

            FWindow window = new FWindow("fenUISharp Test", "fenUISharpTest");
            window.Show();

            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icons", "TrayIcon.ico");
            window.SetWindowIcon(iconPath);
            window.CreateTray(iconPath, "Test tray icon!");
            
            FWindow.onTrayIconRightClicked += () => {
                Console.WriteLine("Tray clicked!");
            };

            FPanel c = new FPanel(new Vector2(0, 0), new Vector2(300, 150), SKColors.Black);
            FPanel c2 = new FPanel(new Vector2(0, -25), new Vector2(100, 50), SKColors.DarkGray);
            c2.transform.stretchHorizontal = true;
c2.transform.SetParent(c.transform);

            FLabel label = new FLabel("Abcdefghijklmnopqrstuvwxyz 0123456789", new Vector2(0, 0), new Vector2(100, 20), useLinebreaks: true);
            label.transform.boundsPadding.SetValue(label, 15, 25);
            label.TextAlign = SKTextAlign.Center;
            label.transform.SetParent(c2.transform);
            label.transform.stretchHorizontal = true;
            label.transform.stretchVertical = true;
            label.transform.marginHorizontal = 0;
            label.transform.marginVertical = 0;

            FSimpleButton simpleButton = new FSimpleButton(new Vector2(0, 50), "Test Button!", () => {Console.WriteLine("Test Button Clicked!"); /* label.Text = "Test 2!"; */ });
            simpleButton.transform.SetParent(c.transform);
            FSimpleButton simpleButton2 = new FSimpleButton(new Vector2(0, 25), "Abcdefghijklmnopqrstuvwxyz 0123456789", () => Console.WriteLine("Test Button Clicked!"));
            simpleButton2.transform.SetParent(c.transform);

            FWindow.uiComponents.Add(c);
            FWindow.uiComponents.Add(simpleButton);
            FWindow.uiComponents.Add(simpleButton2);
            FWindow.uiComponents.Add(label);
FWindow.uiComponents.Add(c2);

            window.CreateSurface();
            window.Begin();
        }
    }
}