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

        public float ApproximateLength(float precision = 1f)
        {
            var measure = new SKPathMeasure(SKPath, false);
            float length = 0;

            do
            {
                length += measure.Length;
            } while (measure.NextContour());

            return length;
        }

        public void Dispose()
        {
            SKPath.Dispose();
        }
    }
}