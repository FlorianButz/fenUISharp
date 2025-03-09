using System.Reflection;
using FenUISharp;
using SkiaSharp;

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

            var t = new TestComponent(0, 0, 500, 350);
            t.skPaint.ImageFilter = SKImageFilter.CreateDropShadow(0, 0, 25, 25, SKColors.Black);

            // var t2 = new TestComponent(0, 0, 350, 50);
            // t2.transform.scale = new Vector2(0.25f, 0.25f);
            // t2.skPaint.Color = SKColors.Red;
            // //t2.transform.alignment = new Vector2(0.5f, 0);
            // t2.transform.parent = t.transform;

            // var t3 = new TestComponent(0, 0, 150, 15);
            // t3.transform.scale = new Vector2(0.35f, 0.15f);
            // t3.skPaint.Color = SKColors.Yellow;
            // //t2.transform.alignment = new Vector2(0.5f, 0);
            // t3.transform.parent = t2.transform;

            FWindow.uiComponents.Add(t);
            // FWindow.uiComponents.Add(t2);
            // FWindow.uiComponents.Add(t3);

            // for (int i = 0; i < 1500; i++) {
            //     FWindow.uiComponents.Add(new TestComponent());
            // }

            window.CreateSurface();
            window.Begin();
        }
    }
}