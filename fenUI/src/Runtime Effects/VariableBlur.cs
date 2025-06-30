using FenUISharp.Mathematics;
using SkiaSharp;

namespace FenUISharp.RuntimeEffects
{
    public static class VariableBlur
    {
        // TODO: Probably should fix the seams between the first few segments some time
        // TODO: Make a more primitive version of this
        public static void ApplyBlur(SKSurface surface, SKRect bounds, int pad, Vector2 direction, float maxBlur = 5, Func<float, float>? progressiveFunc = null)
        {
            using var snap = surface.Snapshot();

            using var cpaint = new SKPaint { BlendMode = SKBlendMode.Clear };
            var b = bounds;
            b.Inflate(5, 5);
            surface.Canvas.DrawRect(bounds, cpaint);

            int sliceHeight = 1;

            Func<float, float> bFunc = progressiveFunc ?? ((x) => MathF.Pow(x, 2));

            for (int y = 0; y < bounds.Height; y += sliceHeight)
            {
                surface.Canvas.Save();

                // Calculate blur strength using curve
                float t = (float)y / (float)bounds.Height;

                float sigma = 0;
                if (direction.y == 1)
                    sigma = bFunc(1 - t) * maxBlur;
                else
                    sigma = bFunc(t) * maxBlur;

                using var blur = SKImageFilter.CreateBlur(sigma, sigma / 2);
                using var paint = new SKPaint
                {
                    ImageFilter = blur
                };

                using var fadePaint = new SKPaint();
                fadePaint.BlendMode = SKBlendMode.DstIn;

                // Draw blurred content
                var rect = SKRect.Create(0, y + bounds.Top, bounds.Width, sliceHeight);

                var clipRect = rect;
                surface.Canvas.ClipRect(clipRect);

                surface.Canvas.DrawImage(snap, -pad, -pad, paint);
                surface.Canvas.DrawRect(rect, fadePaint);
                surface.Canvas.Restore();
            }
        }
    }
}