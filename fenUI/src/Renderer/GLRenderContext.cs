using System.Runtime.InteropServices;
using OpenTK.Graphics.ES30;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SkiaSharp;

using OpenTK.Graphics;
using OpenTK.Platform.Windows;
using OpenTK;

namespace FenUISharp
{
    public class GLRenderContext : FRenderContext
    {
        private GRBackendRenderTarget renderTarget;
        private GRGlInterface gpuInterface;
        private GRContext grContext;

        private uint fbo;
        private uint rbo;

        private IntPtr glContext;
        private IntPtr deviceContext;

        public GLRenderContext(Window windowRoot) : base(windowRoot)
        {
            RecreateGl();
        }

        void RecreateGl(){
            if(glContext == IntPtr.Zero) CreateOpenGLContext(WindowRoot.hWnd);
            CreateOpenGl(glContext);
            Surface = CreateSurface();
        }

        public void CreateOpenGLContext(IntPtr hWnd)
        {
            IntPtr hdc = GetDC(hWnd);
            if (hdc == IntPtr.Zero)
                throw new Exception("Failed to get device context.");
                
            if(glContext != IntPtr.Zero)
                wglDeleteContext(glContext);

            PIXELFORMATDESCRIPTOR pfd = new PIXELFORMATDESCRIPTOR();
            pfd.nSize = (ushort)Marshal.SizeOf(typeof(PIXELFORMATDESCRIPTOR));
            pfd.nVersion = 1;
            pfd.dwFlags = PFD_DRAW_TO_WINDOW | PFD_SUPPORT_OPENGL | PFD_DOUBLEBUFFER;
            pfd.iPixelType = PFD_TYPE_RGBA;
            pfd.cColorBits = 32;
            pfd.cDepthBits = 24;
            pfd.cStencilBits = 8;
            pfd.iLayerType = 0; // PFD_MAIN_PLANE

            int pixelFormat = ChoosePixelFormat(hdc, ref pfd);
            if (pixelFormat == 0)
                throw new Exception("Failed to choose a pixel format.");

            if (!SetPixelFormat(hdc, pixelFormat, ref pfd))
                throw new Exception("Failed to set the pixel format.");

            IntPtr hglrc = wglCreateContext(hdc);
            if (hglrc == IntPtr.Zero)
                throw new Exception("Failed to create OpenGL context.");
            if (!wglMakeCurrent(hdc, hglrc))
                throw new Exception("Failed to make OpenGL context current.");

            deviceContext = hdc;
            glContext = hglrc;
        }

        void CreateOpenGl(IntPtr ctx)
        {
            grContext?.Dispose();
            gpuInterface?.Dispose();

            GL.LoadBindings(new BindingsContext());

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

        protected override SKSurface CreateSurface()
        {
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
            RecreateGl();
        }

        public override void Dispose()
        {
            base.Dispose();

            gpuInterface.Dispose();
            grContext.Dispose();
            renderTarget.Dispose();

            if(glContext != IntPtr.Zero)
                wglDeleteContext(glContext);
        }

        public override void OnWindowPropertyChanged()
        {
            RecreateGl();
        }
    }

    public class BindingsContext : IBindingsContext
    {
        public IntPtr GetProcAddress(string procName)
        {
            return wglGetProcAddress(procName);
        }

        [DllImport("opengl32.dll", EntryPoint = "wglGetProcAddress", CharSet = CharSet.Ansi)]
        public static extern IntPtr wglGetProcAddress(string procName);
    }
}