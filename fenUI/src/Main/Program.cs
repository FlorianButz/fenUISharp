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
            c.transform.boundsPadding.SetValue(c, 25, 25);

            FSimpleButton simpleButton = new FSimpleButton(new Vector2(0, 0), "Test Button!", () => Console.WriteLine("Test Button Clicked!"));
            simpleButton.transform.SetParent(c.transform);
            FSimpleButton simpleButton2 = new FSimpleButton(new Vector2(0, 25), "aoidjaoiwjoidjawoidjawoidowajd", () => Console.WriteLine("Test Button Clicked!"));
            simpleButton2.transform.SetParent(c.transform);

            FWindow.uiComponents.Add(c);
            FWindow.uiComponents.Add(simpleButton);
            FWindow.uiComponents.Add(simpleButton2);

            window.CreateSurface();
            window.Begin();
        }
    }
}