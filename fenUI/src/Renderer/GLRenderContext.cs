using OpenTK.Graphics.ES30;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SkiaSharp;

namespace FenUISharp
{
    public class GLRenderContext : FRenderContext
    {
        private GRBackendRenderTarget renderTarget;
        private GRGlInterface gpuInterface;
        private GRContext grContext;

        private uint fbo;
        private uint rbo;

        public GLRenderContext(Window windowRoot) : base(windowRoot)
        {
            CreateOpenGl();
        }

        void CreateOpenGl()
        {
            grContext?.Dispose();
            gpuInterface?.Dispose();

            gpuInterface = GRGlInterface.Create();
            grContext = GRContext.CreateGl(gpuInterface);

            fbo = (uint)GL.GenFramebuffer();
            rbo = (uint)GL.GenRenderbuffer();
            GL.BindRenderbuffer(OpenTK.Graphics.ES30.RenderbufferTarget.Renderbuffer, rbo);
            GL.RenderbufferStorage(OpenTK.Graphics.ES30.RenderbufferTarget.Renderbuffer, OpenTK.Graphics.ES30.RenderbufferInternalFormat.Rgba8, (int)WindowRoot.WindowSize.x, (int)WindowRoot.WindowSize.y);
            GL.BindFramebuffer(OpenTK.Graphics.ES30.FramebufferTarget.DrawFramebuffer, fbo);
            GL.FramebufferRenderbuffer(
                OpenTK.Graphics.ES30.FramebufferTarget.DrawFramebuffer,
                OpenTK.Graphics.ES30.FramebufferAttachment.ColorAttachment0,
                OpenTK.Graphics.ES30.RenderbufferTarget.Renderbuffer,
                rbo);

            var sampleCounts = GL.GetInteger(OpenTK.Graphics.ES30.GetPName.Samples);
            int stencilBits;
            GL.GetFramebufferAttachmentParameter(
                OpenTK.Graphics.ES30.FramebufferTarget.DrawFramebuffer,
                OpenTK.Graphics.ES30.FramebufferAttachment.ColorAttachment0,
                OpenTK.Graphics.ES30.FramebufferParameterName.FramebufferAttachmentStencilSize,
                out stencilBits);
        }

        protected override SKSurface CreateSurface(){
            renderTarget?.Dispose();
            Surface?.Dispose();

            renderTarget = new GRBackendRenderTarget((int)WindowRoot.WindowSize.x, (int)WindowRoot.WindowSize.y, 0, 0, new GRGlFramebufferInfo(0, SKColorType.Rgba8888.ToGlSizedFormat()));
            return SKSurface.Create(grContext, renderTarget, GRSurfaceOrigin.TopLeft, SKColorType.Rgba8888);
        }

        public override SKSurface BeginDraw()
        {
            Surface.Canvas.Clear(SKColors.Transparent);
            return Surface;
        }

        public override SKSurface CreateAdditional()
        {
            return SKSurface.Create(grContext, renderTarget, GRSurfaceOrigin.TopLeft, SKColorType.Rgba8888);
        }

        public override void EndDraw()
        {
            Surface.Canvas.Flush();
            Surface.Flush();
        }

        public override void OnResize(Vector2 newSize)
        {
            CreateOpenGl();
            Surface = CreateSurface();
        }

        public override void Dispose()
        {
            base.Dispose();

            gpuInterface.Dispose();
            grContext.Dispose();
            renderTarget.Dispose();
        }

        public override void OnWindowPropertyChanged()
        {
            CreateOpenGl();
            Surface = CreateSurface();
        }
    }
}