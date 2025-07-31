using System.Drawing.Drawing2D;
using SkiaSharp;

namespace FenUISharp.AnimatedVectors
{
    public class AnimatedVector : IDisposable
    {
        public SKRect ViewBox { get; set; }

        public SKStrokeCap LineCap { get; set; }
        public SKStrokeJoin LineJoin { get; set; }

        public int ExtendBounds { get; set; }

        public List<AVPath> Paths { get; set; } = new();
        public List<(string id, AVAnimation animation)> Animations { get; set; } = new();

        public void Dispose()
        {
            Paths.ForEach(x => x.Dispose());
            Paths = new();
        }
    }
}