using System.Reflection;
using FenUISharp;
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
            FenUI.Demo();
        }
    }
}