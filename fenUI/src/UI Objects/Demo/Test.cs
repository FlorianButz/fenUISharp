using fenUI.Utils;
using FenUISharp;
using FenUISharp.Objects;
using FenUISharp.WinFeatures;
using SkiaSharp;

public class TestObject : UIObject
{

    public override void Dispose()
    {
        base.Dispose();
    }

    public override void Render(SKCanvas canvas)
    {
        var buffer = FContext.GetCurrentWindow().GetScreenBuffer();
        buffer.CaptureScreen();

        var frame = buffer.CachedCapture;
        if (frame != null)
        {
            canvas.DrawImage(frame, Shape.SurfaceDrawRect);
        }
        this.Invalidate(Invalidation.SurfaceDirty);
    }
}