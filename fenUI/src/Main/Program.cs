using System.Reflection;
using FenUISharp;
using FenUISharp.Mathematics;
using FenUISharp.Objects;
using FenUISharp.Themes;
using SkiaSharp;

namespace FenUISharpTest1
{
    class Programw
    {
        [STAThread]
        static void Main()
        {
            FenUI.Init();
            FenUI.SetupAppModel("FlorianButz.fenUI");
            FenUI.Demo();
        }
    }
}