using System.Reflection;
using DynamicWin;
using FenUISharp;
using SkiaSharp;

using Windows.Media.Control;
using Windows.Storage.Streams;

namespace FenUISharpTest1
{
    class Program
    {
        [STAThread]
        static void Main(){
            FWindow window = new FWindow("fenUISharp Test", "fenUISharpTest");
            window.Show();

            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icons", "TrayIcon.ico");
            window.SetWindowIcon(iconPath);
            window.CreateTray(iconPath, "Test tray icon!");
            
            FWindow.onTrayIconRightClicked += () => {
                Console.WriteLine("Tray clicked!");
            };

            new WindowsMediaControls();

            var t = new TestComponent(0, 0, 100, 100);
            // t.skPaint.ImageFilter = SKImageFilter.CreateDropShadow(0, 0, 25, 25, SKColors.Black);

            // var t2 = new TestComponent(0, 0, 50, 50);
            // t2.skPaint.Color = SKColors.Red;
            // //t2.transform.alignment = new Vector2(0.5f, 0);
            // t2.transform.parent = t.transform;

            FWindow.uiComponents.Add(t);
            // FWindow.uiComponents.Add(t2);

            // for (int i = 0; i < 1500; i++) {
            //     FWindow.uiComponents.Add(new TestComponent());
            // }

            window.CreateSurface();
            window.Begin();
        }
    }
}