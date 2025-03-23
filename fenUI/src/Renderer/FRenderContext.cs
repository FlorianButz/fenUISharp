using SkiaSharp;

namespace FenUISharp {

    public abstract class FRenderContext : IDisposable {
        
        public Window WindowRoot { get; set; }
        public SKSurface Surface { get; protected set; }

        public bool HasAlphaChannel { get; set; } = false;

        public SKSamplingOptions SamplingOptions { get; private set; } = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);

        public FRenderContext(Window windowRoot){
            WindowRoot = windowRoot;
        }

        public abstract SKSurface BeginDraw();
        public abstract void EndDraw();
        public abstract SKSurface CreateAdditional();
        protected abstract SKSurface CreateSurface();

        public abstract void OnResize(Vector2 newSize);
        public abstract void OnWindowPropertyChanged(); // Used for refreshrate change, etc..

        public virtual void UpdateWindow() {
            WindowRoot.UpdateWindowFrame();
        }

        public SKImage? CaptureWindowRegion(SKRect region, float quality = 0.5f)
        {
            if (Surface == null)
                return null;

            var snapshot = Surface.Snapshot(new SKRectI((int)region.Left, (int)region.Top, (int)region.Right, (int)region.Bottom));
            var scaled = RMath.CreateLowResImage(snapshot, RMath.Clamp(quality, 0.05f, 1f), WindowRoot.RenderContext.SamplingOptions);
            snapshot.Dispose();

            return scaled;
        }

        public virtual void Dispose()
        {
            Surface?.Dispose();
        }
    }

}