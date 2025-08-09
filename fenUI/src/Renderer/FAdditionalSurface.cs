using SkiaSharp;
using Vortice.Direct3D12;

namespace FenUISharp
{
    public class FAdditionalSurface : IDisposable
    {
        public SKSurface SkiaSurface { get; private set; }
        public ID3D12Resource Texture { get; private set; }
        public GRD3DTextureResourceInfo ResourceInfo { get; private set; }
        public GRBackendTexture BackendTexture { get; private set; }

        public SKImageInfo Info { get; private set; }

        private SkiaDirectCompositionContext SkiaDirectCompositionContext { get; set; }

        public FAdditionalSurface(
            SKSurface s,
            ID3D12Resource d,
            GRD3DTextureResourceInfo r,
            GRBackendTexture t,
            SKImageInfo info,
            SkiaDirectCompositionContext skiaDirectCompositionContext)
        {
            this.SkiaSurface = s;
            this.Texture = d;
            this.ResourceInfo = r;
            this.BackendTexture = t;

            this.Info = info;

            this.SkiaDirectCompositionContext = skiaDirectCompositionContext;
            this.SkiaDirectCompositionContext.OnRebuildAdditionals += RebuildSurface;
            this.SkiaDirectCompositionContext.OnDisposeAdditionals += Dispose;
        }

        private void RebuildSurface()
        {
            Dispose();
            this.SkiaDirectCompositionContext.OnRebuildAdditionals += RebuildSurface;
            this.SkiaDirectCompositionContext.OnDisposeAdditionals += Dispose;

            // Get new surface with new resources
            var newResources = SkiaDirectCompositionContext.CreateAdditional(Info);
            
            // Set resources
            SkiaSurface = newResources.SkiaSurface;
            Texture = newResources.Texture;
            ResourceInfo = newResources.ResourceInfo;
            BackendTexture = newResources.BackendTexture;
        }

        public void Dispose()
        {
            SkiaSurface?.Dispose();
            SkiaSurface = null!;

            Texture?.Dispose();
            Texture = null!;

            ResourceInfo?.Dispose();
            ResourceInfo = null!;

            BackendTexture?.Dispose();
            BackendTexture = null!;

            this.SkiaDirectCompositionContext.OnRebuildAdditionals -= RebuildSurface;
            this.SkiaDirectCompositionContext.OnDisposeAdditionals -= Dispose;
        }
    }
}