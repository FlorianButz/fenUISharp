using FenUISharp.Mathematics;
using SkiaSharp;

namespace FenUISharp.AnimatedVectors
{
    public class AVPath : IDisposable
    {
        public SKPath SKPath { get; init; }
        public SKColor Stroke { get; init; }
        public SKColor Fill { get; init; }
        public float StrokeWidth { get; init; }

        public void Dispose()
        {
            SKPath.Dispose();
        }
    }

    public static class SKPathExtensions
    {
        public static float ApproximateLength(this SKPath SKPath, float precision = 1f)
        {
            var measure = new SKPathMeasure(SKPath, false);
            float length = 0;

            do
            {
                length += measure.Length;
            } while (measure.NextContour());

            return length;
        }
    }
}