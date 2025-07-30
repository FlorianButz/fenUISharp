using System.Drawing.Drawing2D;
using SkiaSharp;

namespace FenUISharp.AnimatedVectors
{
    public class AnimatedVector : IDisposable
    {
        public LineCap LineCap { get; set; }
        public LineJoin LineJoin { get; set; }

        public List<AVPath> Paths { get; set; } = new();

        public void Dispose()
        {
            Paths.ForEach(x => x.Dispose());
            Paths = new();
        }
    }
}