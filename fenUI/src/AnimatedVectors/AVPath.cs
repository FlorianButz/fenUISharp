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

        public bool UseObjectAnchor { get; set; }
        public bool UseObjectSizeTranslation { get; set; }

        // Animatable values
        public Vector2 Anchor = new(0.5f, 0.5f);
        public Vector2 Translation = new(0f, 0f);
        public Vector2 Scale = new(1f, 1f);
        public float Rotation = 0f;
        public float Opacity = 1f;
        public float BlurRadius = 0f;
        public float StrokeTrace = 1f;

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