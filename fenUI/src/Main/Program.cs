using System.Reflection;
using FenUISharp;

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

            FWindow.uiComponents.Add(new TestComponent(0, 0, 250, 500));

            // for (int i = 0; i < 1500; i++) {
            //     FWindow.uiComponents.Add(new TestComponent());
            // }

            window.CreateSurface();
            window.Begin();
            
            window.Cleanup();
        }
    }
}