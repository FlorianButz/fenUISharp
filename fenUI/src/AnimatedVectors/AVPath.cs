using SkiaSharp;

namespace FenUISharp.AnimatedVectors
{
    public struct AVPath : IDisposable
    {
        public SKPath Path { get; init; }
        public SKColor Stroke { get; init; }
        public SKColor Fill { get; init; }
        public float StrokeWidth { get; init; }

        public void Dispose()
        {
            Path.Dispose();
        }
    }
}