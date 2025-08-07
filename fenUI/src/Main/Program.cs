using System.Reflection;
using FenUISharp;
using FenUISharp.AnimatedVectors;
using FenUISharp.Mathematics;
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

            FWindow testWindow = new FTestWin("Test Window!", "testWindow");
            testWindow.BeginWindowLoop();

            // FenUI.EnableDebugFunctions();
            // FenUI.Demo();
        }
    }
}