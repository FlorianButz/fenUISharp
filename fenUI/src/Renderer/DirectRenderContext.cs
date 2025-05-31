using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SkiaSharp;
using SharpDX.DirectComposition;
using FenUISharp.Mathematics;

namespace FenUISharp
{
    public class DirectRenderContext : FRenderContext
    {
        private SharpDX.Direct3D11.Device? _device;
        private DeviceContext? _context;
        private SwapChain? _swapChain;
        private Texture2D? _backBuffer;
        private RenderTargetView? _renderTargetView;
        private Texture2D? _stagingTexture;
        private SKBitmap? _bitmap;

        public DirectRenderContext(Window windowRoot) : base(windowRoot)
        {
            InitializeDirectX();
            Surface = CreateSurface();
        }

        private void InitializeDirectX()
        {
            Vector2 size = GetSize();

            var swapChainDesc = new SwapChainDescription()
            {
                BufferCount = 2,
                ModeDescription = new ModeDescription((int)size.x, (int)size.y,
                                                      new Rational(60, 1), Format.B8G8R8A8_UNorm),
                IsWindowed = true,
                OutputHandle = WindowRoot.hWnd,
                SampleDescription = new SampleDescription(1, 0),
                SwapEffect = SwapEffect.FlipDiscard,
                Usage = Usage.RenderTargetOutput
            };

            SharpDX.Direct3D11.Device.CreateWithSwapChain(
                SharpDX.Direct3D.DriverType.Hardware,
                DeviceCreationFlags.BgraSupport,
                swapChainDesc,
                out _device,
                out _swapChain
            );
            _context = _device.ImmediateContext;
            CreateRenderTarget();
        }

        private void CreateRenderTarget()
        {
            Utilities.Dispose(ref _renderTargetView);
            Utilities.Dispose(ref _backBuffer);
            Utilities.Dispose(ref _stagingTexture);

            _backBuffer = _swapChain?.GetBackBuffer<Texture2D>(0);
            _renderTargetView = new RenderTargetView(_device, _backBuffer);
            _context?.OutputMerger.SetRenderTargets(_renderTargetView);

            Vector2 size = GetSize();
            _context?.Rasterizer.SetViewport(0, 0, (int)size.x, (int)size.y);

            _stagingTexture = new Texture2D(_device, new Texture2DDescription
            {
                Width = (int)size.x,
                Height = (int)size.y,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.B8G8R8A8_UNorm,
                Usage = ResourceUsage.Dynamic,
                BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.Write,
                SampleDescription = new SampleDescription(1, 0),
                OptionFlags = ResourceOptionFlags.None
            });
        }

        protected override SKSurface CreateSurface()
        {
            Vector2 size = GetSize();

            _bitmap?.Dispose();
            _bitmap = new SKBitmap(new SKImageInfo(
                (int)size.x,
                (int)size.y,
                SKColorType.Bgra8888,
                SKAlphaType.Premul
            ));

            return SKSurface.Create(_bitmap.Info, _bitmap.GetPixels(), _bitmap.Info.RowBytes);
        }

        public override SKSurface BeginDraw()
        {
            if (_onEndResizeFlag) OnEndResizeAfterDraw();
            if (Surface == null || _bitmap == null || _surfaceDirty || _recreateSurfaceFlag)
            {
                Surface = CreateSurface();
                _recreateSurfaceFlag = false;
            }

            // Surface.Canvas.Clear(new SKColor(0, 0, 0, 0));
            return Surface;
        }

        public override SKSurface CreateAdditional(SKImageInfo info)
        {
            return SKSurface.Create(info);
        }

        public override void EndDraw()
        {
            if (Surface == null || _bitmap == null)
            {
                return; // Nothing to draw
            }

            Surface.Canvas.Flush();

            // Get the pixels from the SKBitmap
            IntPtr pixelsPtr = _bitmap.GetPixels();

            try
            {
                // Map the staging texture for writing
                if (_context == null) throw new("_context is null.");

                var dataBox = _context.MapSubresource(_stagingTexture, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None);
                if (dataBox.DataPointer == IntPtr.Zero)
                {
                    Console.WriteLine("Error: DataPointer is null!");
                    return;
                }

                // Copy the bitmap data to the staging texture
                unsafe
                {
                    byte* source = (byte*)pixelsPtr;
                    byte* destination = (byte*)dataBox.DataPointer;

                    int width = _bitmap.Width;
                    int height = _bitmap.Height;
                    int bitmapRowBytes = _bitmap.RowBytes;
                    int stagingRowPitch = dataBox.RowPitch;

                    // Make sure we don't copy beyond the staging texture dimensions
                    int copyWidth = Math.Min(width * 4, stagingRowPitch); // 4 bytes per pixel (BGRA)

                    // Copy row by row
                    for (int y = 0; y < height; y++)
                    {
                        System.Buffer.MemoryCopy(source, destination, copyWidth, copyWidth);
                        source += bitmapRowBytes;
                        destination += stagingRowPitch;
                    }
                }

                _context.UnmapSubresource(_stagingTexture, 0);
                _context.ClearRenderTargetView(_renderTargetView, new SharpDX.Mathematics.Interop.RawColor4(1, 1, 1, 0));  // Clear with transparency

                // Copy from staging texture to the back buffer
                _context.CopyResource(_stagingTexture, _backBuffer);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in EndDraw: {ex.Message}");
            }

            // Present the frame
            try
            {
                _swapChain?.Present(1, PresentFlags.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error presenting swap chain: {ex.Message}");
            }
        }

        public override void UpdateWindow()
        {
            base.UpdateWindow();

            // Ensure DirectX viewport is synchronized with window size
            if (_context != null)
            {
                UpdateViewport();
            }
        }

        private void UpdateViewport()
        {
            // Make sure viewport matches client area exactly
            _context?.Rasterizer.SetViewport(0, 0, (int)WindowRoot.WindowSize.x, (int)WindowRoot.WindowSize.y);
        }

        public override void Dispose()
        {
            base.Dispose();
            _renderTargetView?.Dispose();
            _backBuffer?.Dispose();
            _stagingTexture?.Dispose();
            _swapChain?.Dispose();
            _context?.Dispose();
            _device?.Dispose();
        }

        public override void OnEndResize()
        {
            _onEndResizeFlag = true;
        }

        bool _onEndResizeFlag = false;
        bool _surfaceDirty = false;

        void OnEndResizeAfterDraw()
        {
            _onEndResizeFlag = false;
            var size = GetSize();

            // Dispose old resources first
            Surface?.Dispose();
            _bitmap?.Dispose();
            Utilities.Dispose(ref _renderTargetView);
            Utilities.Dispose(ref _backBuffer);
            Utilities.Dispose(ref _stagingTexture);

            // Resize swap chain
            _swapChain?.ResizeBuffers(
                2,
                (int)size.x,
                (int)size.y,
                Format.B8G8R8A8_UNorm,
                SwapChainFlags.None
            );

            // Recreate render target and viewport
            CreateRenderTarget();

            // Recreate Skia surface with the new size
            Surface = CreateSurface();
        }

        public override void OnWindowPropertyChanged()
        {
            _surfaceDirty = true;
        }

        private Vector2 GetSize()
        {
            if (WindowRoot.Bounds.Width <= 0 || WindowRoot.Bounds.Height <= 0) return new Vector2(1, 1);
            return new Vector2(WindowRoot.Bounds.Width, WindowRoot.Bounds.Height);
        }

        // Add DPI-related Win32 functions
        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(IntPtr hwnd);

        public override void OnResize(Vector2 newSize)
        {
        }
    }
}
