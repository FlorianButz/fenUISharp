using System.Reflection;
using FenUISharp;
using SkiaSharp;

namespace FenUISharpTest1
{
    class Program
    {
        [STAThread]
        static void Main()
        {
//             Resources.LoadDefault();
//             new WindowsMediaControls();

//             Window window = new Window("fenUISharp Test", "fenUISharpTest");
//             window.Show();
//             // FWindow.showBounds = true;

//             string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icons", "TrayIcon.ico");
//             window.SetWindowIcon(iconPath);
//             window.CreateTray(iconPath, "Test tray icon!");
            
//             Window.instance.onTrayIconRightClicked += () => {
//                 Console.WriteLine("Tray clicked!");
//             };

//             FPanel c = new FPanel(new Vector2(0, 0), new Vector2(300, 150), 15, SKColors.Black);
//             FPanel c2 = new FPanel(new Vector2(0, -25), new Vector2(100, 50), 15, SKColors.DarkGray);
//             c2.transform.stretchHorizontal = true;
// c2.transform.SetParent(c.transform);

//             FLabel label = new FLabel("Abcdefghijklmnopqrstuvwxyz 0123456789", new Vector2(0, 0), new Vector2(100, 20), truncation: TextTruncation.Linebreak);
//             label.transform.boundsPadding.SetValue(label, 15, 25);
//             label.TextAlign = SKTextAlign.Center;
//             label.transform.SetParent(c2.transform);
//             label.transform.stretchHorizontal = true;
//             label.transform.stretchVertical = true;
//             label.transform.marginHorizontal = 0;
//             label.transform.marginVertical = 0;

//             FSimpleButton simpleButton = new FSimpleButton(new Vector2(0, 50), "Test Button!", () => {Console.WriteLine("Test Button Clicked!"); label.Text = Random.Shared.NextInt64().ToString(); });
//             simpleButton.transform.SetParent(c.transform);
//             FSimpleButton simpleButton2 = new FSimpleButton(new Vector2(0, 25), "Longer Test Button Here! waddawdawdadwawd", () => Console.WriteLine("Test Button Clicked!"));
//             simpleButton2.transform.SetParent(c.transform);

//             Window.AddUIComponent(c);
//             Window.AddUIComponent(simpleButton);
//             Window.AddUIComponent(simpleButton2);
//             Window.AddUIComponent(label);
// Window.AddUIComponent(c2);

// Window.AddUIComponent(new FBlurPane(new Vector2(150, 0), new Vector2(100, 50), 15, new Vector2(10, 10), true));

//             window.CreateSurface();
//             window.Begin();
        
                Window window = new NativeWindow("Test 1", "testClass", new Vector2(100, 100), new Vector2(100, 100), true, false);
                window.SetWindowVisibility(true);
        }
    }
}