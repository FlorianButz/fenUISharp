using System.Reflection;
using FenUISharp;
using FenUISharp.AnimatedVectors;
using FenUISharp.Mathematics;
using FenUISharp.Native;
using FenUISharp.Objects;
using FenUISharp.Themes;
using SkiaSharp;

namespace FenUISharp
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            FenUI.Init();
            FenUI.SetupAppModel("FlorianButz.fenUI");

            // FWindow testWindow = new FNativeWindow("Test Window!", "testWindow");
            // // testWindow.Properties.ExcludeFromPeek = true;
            // testWindow.Properties.UseMica = true;
            // testWindow.Properties.MicaBackdropType = Native.MicaBackdropType.MainWindow;
            // testWindow.Properties.VisibleInTaskbar = false;
            // testWindow.Properties.AlwaysOnTop = true;

            // string iconPath = Resources.ExtractResourceToTempFile<FenUI>($"{FenUI.ResourceLibName}.icons.fenui.ico");
            // testWindow.Properties.SetWindowIcon(iconPath);
            // testWindow.Properties.CreateTrayIcon(iconPath, "Test Tooltip!");

            // // testWindow.Properties.AllowResize = false;

            // testWindow.Properties.ShowWindow(Native.ShowWindowCommand.SW_SHOW);
            // testWindow.BeginWindowLoop();

            FenUI.EnableDebugFunctions();
            FenUI.Demo();
        }
    }
}