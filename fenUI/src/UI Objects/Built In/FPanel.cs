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

        public FPanel(Func<Vector2>? position = null, Func<Vector2>? size = null, float? cornerRadius = null, Func<SKColor>? color = null) : base(position, size)
        {
            CornerRadius = new(() => cornerRadius ?? 35, this, this);
            UseSquircle = new(() => true, this, this);

            Transform.SnapPositionToPixelGrid.SetStaticState(true);

            if(color == null)
                RenderMaterial.Value = () => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.PanelMaterial();
            else
                RenderMaterial.Value = () => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.PanelMaterial().WithOverride(new Dictionary<string, object>() { ["BaseColor"] = color });

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
    }
}