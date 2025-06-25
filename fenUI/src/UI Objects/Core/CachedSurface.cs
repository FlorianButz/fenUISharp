using FenUISharp.Mathematics;
using FenUISharp.Objects;
using SkiaSharp;

namespace FenUISharp.Objects
{
    public class CachedSurface(Action<SKCanvas?> DrawAction) : IDisposable
    {
        private SKSurface? _cachedSurface;
        private SKImageInfo? _cachedImageInfo;
        private SKImage? _cachedSnapshot;

        private int padding = 0;
        private float quality = 1;

        public bool LockInvalidation { get; set; } = false;

        public bool TryGetSurface(out SKSurface surface)
        {
            if (_cachedSurface == null) { surface = null; return false; }
            
            surface = _cachedSurface;
            return true;
        }

        private SKSurface? CreateSurface()
        {
            if (_cachedImageInfo == null) throw new Exception("InvalidateSurface has to be called before surface is created");
            if (!FContext.IsValidContext()) throw new Exception("Invalid FenUISharp window context.");

            _cachedSurface = FContext.GetCurrentWindow()?.RenderContext.CreateAdditional(_cachedImageInfo.Value);

            _cachedSurface?.Canvas.Scale(quality, quality);
            _cachedSurface?.Canvas.Translate(padding, padding);

            return _cachedSurface;
        }

        public SKSurface? Draw()
        {
            if (_cachedSurface != null)
                return _cachedSurface;
            else
            {
                var surface = CreateSurface();
                DrawAction?.Invoke(surface?.Canvas);

                _cachedSnapshot?.Dispose();
                _cachedSnapshot = null;

                _cachedSnapshot = surface?.Snapshot();

                return surface;
            }
        }

        public SKImage? CaptureSurfaceRegion(SKRect region, float quality = 0.5f)
        {
            if (_cachedSurface == null)
                return null;

            // TODO: remove
            Compositor.Dump(_cachedSurface.Snapshot(), "buffer_surf_whole");

            var snapshot = _cachedSurface.Snapshot(new SKRectI((int)region.Left, (int)region.Top, (int)region.Right, (int)region.Bottom));
            var scaled = RMath.CreateLowResImage(snapshot, RMath.Clamp(quality, 0.01f, 1f), FContext.GetCurrentWindow().RenderContext.SamplingOptions);
            snapshot?.Dispose();

            Compositor.Dump(scaled, "buffer_cropped_scaled");

            return scaled;
        }

        public void InvalidateSurface(SKRect dimensions, float quality, int padding)
        {
            if (LockInvalidation) return;

            _cachedSurface?.Canvas.Dispose();
            _cachedSurface?.Dispose();
            _cachedSurface = null;

            quality = RMath.Clamp(quality, 0, 1);

            int width = (int)((dimensions.Width + padding * 2) * quality);
            int height = (int)((dimensions.Height + padding * 2) * quality);
            this.padding = padding;
            this.quality = quality;

            _cachedImageInfo = new SKImageInfo(width, height);
        }

        public SKImage? GetImage()
        {
            return _cachedSnapshot;
        }

        public void Dispose()
        {
            _cachedSnapshot?.Dispose();
            _cachedSurface?.Dispose();
        }
    }
}