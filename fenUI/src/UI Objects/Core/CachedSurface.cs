using FenUISharp.Logging;
using FenUISharp.Mathematics;
using FenUISharp.Objects;
using FenUISharp.RuntimeEffects;
using SkiaSharp;

namespace FenUISharp.Objects
{
    public class CachedSurface(Action<SKCanvas?> DrawAction) : IDisposable
    {
        private FAdditionalSurface? _cachedSurface;
        private SKImageInfo? _cachedImageInfo;
        private SKImage? _cachedSnapshot;

        private int padding = 0;
        private float quality = 1;

        public bool LockInvalidation { get; set; } = false;
        public bool UseSnapshotBlit { get; set; } = false;

        private readonly object _drawLock = new();

        public bool TryGetSurface(out SKSurface surface)
        {
            if (_cachedSurface == null) { surface = null; return false; }
            surface = _cachedSurface.SkiaSurface;
            return true;
        }

        private SKSurface? CreateSurface()
        {
            if (_cachedImageInfo == null)
            {
                FLogger.Error("InvalidateSurface has to be called before surface is created");
                return null;
            }
            
            if (!FContext.IsValidContext())
            {
                FLogger.Error("Invalid FenUISharp window context.");
                return null;
            }

            if (_cachedImageInfo.Value.Width == 0 || _cachedImageInfo.Value.Height == 0)
            {
                FLogger.Error("Invalid surface size.");
                return null;
            }

            _cachedSurface = FContext.GetCurrentWindow().RenderResources?.CreateAdditional(_cachedImageInfo.Value);

            if (_cachedSurface != null && _cachedSurface.SkiaSurface != null)
                FinalizeSurface(_cachedSurface.SkiaSurface);

            return _cachedSurface?.SkiaSurface;
        }

        private void FinalizeSurface(SKSurface surface)
        {
            surface.Canvas.Scale(quality, quality);
            surface.Canvas.Translate(padding, padding);
        }

        /// <summary>
        /// Draw the Draw Action to the surface / or returns cached surface. Applies post processing only if cached surface is invalidated
        /// </summary>
        /// <param name="effectChain"></param>
        /// <returns></returns>
        public SKSurface? Draw(PostProcessChain? effectChain = null)
        {
            lock (_drawLock)
            {
                if (_cachedSurface != null && _cachedSurface.IsValid() && !_cachedSurface.WasRebuilt)
                    return _cachedSurface.SkiaSurface;
                else
                {
                    if (_cachedImageInfo?.Width == 0 || _cachedImageInfo?.Height == 0)
                        return null;

                    SKSurface? surface = _cachedSurface?.SkiaSurface;
                    if (_cachedSurface == null || !_cachedSurface.IsValid())
                    {
                        _cachedSurface?.Dispose();

                        surface = CreateSurface();
                        if (surface == null)
                            return null;
                    }
                    else
                    {
                        // The surface has been rebuilt and is blank
                        FinalizeSurface(_cachedSurface.SkiaSurface);
                        _cachedSurface.RemoveRebuiltFlag();
                    }

                    var ppInfo = new PPInfo()
                    {
                        source = surface,
                        target = surface,
                        sourceInfo = _cachedImageInfo ?? new()
                    };

                    if (effectChain != null && surface != null) effectChain.OnBeforeRender(ppInfo);

                    DrawAction?.Invoke(surface?.Canvas);

                    if (effectChain != null && surface != null) effectChain.OnAfterRender(ppInfo);
                    if (effectChain != null && surface != null) effectChain.OnLateAfterRender(ppInfo);

                    _cachedSnapshot?.Dispose();
                    _cachedSnapshot = null;

                    if (UseSnapshotBlit || FenUI.Flags.Contains("force_snapshotblit"))
                        _cachedSnapshot = surface?.Snapshot();

                    return surface;
                }
            }
        }

        public void DrawFullChainToTarget(SKCanvas target, SKRect targetRect, SKPaint paint, PostProcessChain? effectChain = null)
        {
            // Rendering offscreen
            Draw(effectChain);

            // Draw to screen
            if (_cachedSnapshot != null) 
            {
                target.DrawImage(_cachedSnapshot, targetRect, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear), paint);
            }
            else if (_cachedSurface != null && _cachedSurface.IsValid())
            {
                target.DrawSurface(_cachedSurface.SkiaSurface, new SKPoint(targetRect.Left, targetRect.Top), paint);
            }
        }

        public static (SKPoint scale, SKPoint offset) GetTransformationsForTargetRect(
            SKRect sourceRect,
            SKRect targetRect,
            bool preserveAspect = true,
            bool center = true)
        {
            float scaleX = targetRect.Width / sourceRect.Width;
            float scaleY = targetRect.Height / sourceRect.Height;

            if (preserveAspect)
            {
                float uniformScale = Math.Min(scaleX, scaleY);
                scaleX = scaleY = uniformScale;
            }

            float offsetX = targetRect.Left;
            float offsetY = targetRect.Top;

            if (center)
            {
                offsetX += (targetRect.Width - sourceRect.Width * scaleX) / 2f;
                offsetY += (targetRect.Height - sourceRect.Height * scaleY) / 2f;
            }

            return (new SKPoint(scaleX, scaleY), new SKPoint(offsetX, offsetY));
        }

        public SKImage? CaptureSurfaceRegion(SKRect region, float quality = 0.5f)
        {
            if (_cachedSurface == null)
                return null;

            Compositor.Dump(_cachedSurface.SkiaSurface.Snapshot(), "buffer_surf_whole");

            var snapshot = _cachedSurface.SkiaSurface.Snapshot(new SKRectI((int)region.Left, (int)region.Top, (int)region.Right, (int)region.Bottom));
            var scaled = RMath.CreateLowResImage(snapshot, RMath.Clamp(quality, 0.01f, 1f), FenUISharp.SkiaDirectCompositionContext.SamplingOptions);
            snapshot?.Dispose();

            Compositor.Dump(scaled, "buffer_cropped_scaled");

            return scaled;
        }

        public void InvalidateSurface(SKRect dimensions, float quality, int padding)
        {
            lock (_drawLock)
            {
                if (LockInvalidation) return;

                if (_cachedSurface != null)
                {
                    _cachedSurface?.SkiaSurface?.Canvas.Dispose();
                    _cachedSurface?.Dispose();
                    _cachedSurface = null;
                }

                quality = RMath.Clamp(quality, 0.01f, 4f);

                float w = Math.Max(0, dimensions.Width);
                float h = Math.Max(0, dimensions.Height);
                int width = (int)((w + padding * 2) * quality);
                int height = (int)((h + padding * 2) * quality);
                if (width <= 0) width = 1;
                if (height <= 0) height = 1;
                this.padding = padding;
                this.quality = quality;

                _cachedImageInfo = new SKImageInfo(width, height);
            }
        }

        public SKImage? GetImage()
        {
            return _cachedSnapshot;
        }

        public void Dispose()
        {
            LockInvalidation = false;
            
            _cachedSnapshot?.Dispose();
            _cachedSurface?.Dispose();

            _cachedSnapshot = null;
            _cachedSurface = null;
        }

        internal void DisposeSurface()
        {
            lock (_drawLock)
            {
                if (LockInvalidation) return;

                // GPU will handle resource cleanup asynchronously - no need to block
                // The old content will be discarded when the GPU gets around to it
                _cachedSnapshot?.Dispose();
                _cachedSurface?.Dispose();

                _cachedSnapshot = null;
                _cachedSurface = null;
            }
        }
    }
}