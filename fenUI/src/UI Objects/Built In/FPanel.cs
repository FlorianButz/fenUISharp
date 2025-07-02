using FenUISharp.Materials;
using FenUISharp.Mathematics;
using FenUISharp.Objects;
using FenUISharp.States;
using FenUISharp.Themes;
using SkiaSharp;

namespace FenUISharp.Objects
{
    public class FPanel : UIObject, IStateListener
    {
        public State<float> CornerRadius { get; private init; }

        public State<bool> UseSquircle { get; private init; }

        protected bool _drawBasePanel = true;

        public FPanel(Func<Vector2>? position = null, Func<Vector2>? size = null) : base(position, size)
        {
            CornerRadius = new(() => 20, this);
            UseSquircle = new(() => true, this);
            Transform.SnapPositionToPixelGrid.SetStaticState(true);
            RenderMaterial.Value = () => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.PanelMaterial();
            Padding.Value = () => 20;
        }

        public override void Render(SKCanvas canvas)
        {
            base.Render(canvas);
            if(_drawBasePanel) RenderBasePanel(canvas, Shape.LocalBounds);
        }

        protected virtual void RenderBasePanel(SKCanvas canvas, SKRect rect)
        {
            using var paint = GetRenderPaint();
            using var path = GetPanelPath(rect);
            RenderMaterial.CachedValue.DrawWithMaterial(canvas, path, this, paint);
        }

        public SKPath GetPanelPath(SKRect? rect = null)
        {
            SKPath path = new();

            if (UseSquircle.CachedValue)
                path = SKSquircle.CreateSquircle(rect ?? Shape.LocalBounds, CornerRadius.CachedValue);
            else
                path.AddRoundRect(rect ?? Shape.LocalBounds, CornerRadius.CachedValue, CornerRadius.CachedValue);

            return path;
        }

        public override void Dispose()
        {
            base.Dispose();

            CornerRadius.Dispose();
            UseSquircle.Dispose();
        }
    }
}