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
            if (!FContext.IsValidContext()) throw new Exception("Invalid FenUISharp window context.");

            _cachedSurface = FContext.GetCurrentWindow()?.SkiaDirectCompositionContext?.CreateAdditional(_cachedImageInfo.Value);

            _cachedSurface?.SkiaSurface.Canvas.Scale(quality, quality);
            _cachedSurface?.SkiaSurface.Canvas.Translate(padding, padding);

            return _cachedSurface?.SkiaSurface;
        }

        /// <summary>
        /// Draw the Draw Action to the surface / or returns cached surface. Applies post processing only if cached surface is invalidated
        /// </summary>
        /// <param name="effectChain"></param>
        /// <returns></returns>
        public SKSurface? Draw(PostProcessChain? effectChain = null)
        {
            if (_cachedSurface != null)
                return _cachedSurface.SkiaSurface;
            else
            {
                var surface = CreateSurface();

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

                _cachedSnapshot = surface?.Snapshot();

                return surface;
            }
        }

        public void DrawFullChainToTarget(SKCanvas target, SKRect targetRect, SKPaint paint, PostProcessChain? effectChain = null)
        {
            // Rendering offscreen
            Draw(effectChain);

            // Draw to screen
            if (_cachedSnapshot != null) target.DrawImage(_cachedSnapshot, targetRect, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear), paint);

            // Direct drawing, introduces heavy aliasing. Might be good as an optional choice
            // var sourceRect = SKRect.Create(0, 0, _cachedImageInfo?.Width ?? 1, _cachedImageInfo?.Height ?? 1);
            // var transformations = GetTransformationsForTargetRect(sourceRect, targetRect, false);

            // int save = target.Save();
            // target.Scale(transformations.scale);

            // if (surface != null)
            //     target.DrawSurface(surface, transformations.offset, paint);

            // target.RestoreToCount(save);
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
            var scaled = RMath.CreateLowResImage(snapshot, RMath.Clamp(quality, 0.01f, 1f), SkiaDirectCompositionContext.SamplingOptions);
            snapshot?.Dispose();

            Compositor.Dump(scaled, "buffer_cropped_scaled");

            return scaled;
        }

        public void InvalidateSurface(SKRect dimensions, float quality, int padding)
        {
            if (LockInvalidation) return;

            if (_cachedSurface != null)
            {
                _cachedSurface?.SkiaSurface?.Canvas.Dispose();
                _cachedSurface?.Dispose();
                _cachedSurface = null;
            }

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
            LockInvalidation = false;
            DisposeSurface();
        }

        internal void DisposeSurface()
        {
            if (LockInvalidation) return;

            // _cachedImageInfo = null;
            _cachedSnapshot?.Dispose();
            _cachedSurface?.Dispose();

            _cachedSnapshot = null;
            _cachedSurface = null;
        }
    }
}