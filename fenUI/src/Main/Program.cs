using FenUISharp;

namespace FenUISharpTest1
{
    class Program
    {
        static void Main(){
            FWindow window = new FWindow("fenUISharp Test", "fenUISharpTest");
            window.Show();
            window.CreateSurface();
            window.Begin();
            window.Cleanup();
        }
    }
}