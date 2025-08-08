using FenUISharp.Mathematics;
using SkiaSharp;
using Vortice.Direct3D11;
using Vortice.Direct3D12;
using Vortice.DXGI;

namespace FenUISharp
{
    public class SkiaDirectCompositionContext : IDisposable
    {
        public Action<SKCanvas> DrawAction { get; private set; }

        public DirectCompositionContext DirectCompositionContext { get; protected set; }
        protected ID3D12Resource backBuffer;
        private GRD3DTextureResourceInfo resourceInfo;
        private GRBackendTexture backendTexture;

        public SKSurface Surface { get; protected set; }
        public GRContext grContext { get; protected set; }

        protected int width, height;

        public SkiaDirectCompositionContext(FWindow window, Action<SKCanvas> drawAction)
        {
            this.DrawAction = drawAction;

            width = (int)window.Shape.ClientSize.x;
            height = (int)window.Shape.ClientSize.y;

            // Creating DirectCompositionContext and getting adapter
            DirectCompositionContext = new DirectCompositionContext(
                window.hWnd,
                (int)window.Shape.ClientSize.x,
                (int)window.Shape.ClientSize.y,
                Vortice.DXGI.Format.B8G8R8A8_UNorm
            );
            using var adapter = DirectCompositionContext.GetHardwareAdapter();

            // Creating backend context for Skia D3D
            using var backendContext = new GRD3DBackendContext()
            {
                Device = DirectCompositionContext.Device.NativePointer,
                Adapter = adapter.NativePointer,
                ProtectedContext = false,
                Queue = DirectCompositionContext.CommandQueue.NativePointer
            };

            // Creating Skia D3D GRContext
            grContext = GRContext.CreateDirect3D(backendContext); // This line will throw an CLR/System.Runtime.InteropServices.SEHException: External component has thrown an exception
            if (grContext == null)
                throw new Exception("Failed to create Skia D3D GRContext");

            CreateTextures();
            CreateSkiaSurface(backBuffer, width, height);
        }

        private void CreateTextures()
        {
            backBuffer?.Dispose();

            var backBufferIndex = DirectCompositionContext.SwapChain.CurrentBackBufferIndex;
            backBuffer = DirectCompositionContext.SwapChain.GetBuffer<ID3D12Resource>(backBufferIndex);
        }

        private void CreateSkiaSurface(ID3D12Resource backBuffer, int width, int height)
        {
            Surface?.Dispose(); // Dispose previous surface if exists

            resourceInfo?.Dispose();
            backendTexture?.Dispose();

            resourceInfo = new GRD3DTextureResourceInfo
            {
                Resource = backBuffer.NativePointer,
                ResourceState = (int)ResourceStates.RenderTarget,
                Format = (uint)87,
                SampleCount = 1,
                LevelCount = 1,
                SampleQualityPattern = 0,
                Protected = false
            };

            backendTexture = new GRBackendTexture(
                width,
                height,
                resourceInfo
            );

            Surface = SKSurface.Create(grContext, backendTexture, GRSurfaceOrigin.TopLeft, SKColorType.Bgra8888);
        }

        internal void Draw()
        {
            if (Surface == null) return; // Skip rendering if surface is not created; for now

            DirectCompositionContext.Present((x) =>
            {
                // Get current back buffer
                CreateTextures();

                // Recreate Skia surface with current back buffer
                CreateSkiaSurface(backBuffer, DirectCompositionContext.SizeX, DirectCompositionContext.SizeY);

                Surface.Canvas.Clear(SKColors.Transparent);
                DrawAction?.Invoke(Surface.Canvas);

                Surface.Canvas.Flush(); // Push drawing commands
                Surface.Flush();

            }, PresentFlags.DoNotWait);
        }

        public void Dispose()
        {
            DrawAction = null!;

            this.backBuffer?.Dispose();
            this.backendTexture?.Dispose();
            this.DirectCompositionContext?.Dispose();
            this.grContext?.Dispose();
            this.resourceInfo?.Dispose();
            this.Surface?.Dispose();
        }

        internal void OnResize(Vector2 size)
        {
            width = (int)size.x;
            height = (int)size.y;

            DirectCompositionContext.Resize(width, height);
        }
    }
}