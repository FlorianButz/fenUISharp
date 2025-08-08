using SkiaSharp;

namespace FenUISharp
{
    public class FWindowSurface : IDisposable
    {
        private WeakReference<FWindow> window { get; set; }
        public FWindow Window { get => window.TryGetTarget(out var target) ? target : throw new Exception("Window not set."); }

        public Func<SKColor> ClearColor { get; set; }

        internal FenUISharp.Objects.ModelViewPane? RootViewPane { get; private set; }

        public FWindowSurface(FWindow window)
        {
            this.window = new WeakReference<FWindow>(window);

            ClearColor = () => Window.Properties.UseSystemDarkMode ?
                (!Window.Properties._isFocused ? SKColor.Parse("#202020") : SKColor.Parse("#211f23")) :
                SKColor.Parse("#f3f3f3");
            // ClearColor = () => SKColors.Transparent;
        }

        public void Dispose()
        {
            RootViewPane?.Dispose();
            RootViewPane = null!;
        }

        public void Draw(SKCanvas canvas)
        {
            // RootViewPane?.(canvas);
        }
    }
}