using FenUISharp.Materials;
using FenUISharp.Mathematics;
using FenUISharp.States;
using SkiaSharp;

namespace FenUISharp.Objects
{
    public class FBlurPane : FPanel, IStateListener
    {
        public bool HighQualityBlur { get; set; } = true;
        public State<Material> BlurMaterial { get; init; }

        public FBlurPane(Func<Vector2>? position = null, Func<Vector2>? size = null) : base(position, size)
        {
            BlurMaterial = new(() => new BlurMaterial(() => Shape.SurfaceDrawRect, () => Composition.GrabBehindPlusBuffer(Shape.GlobalBounds, HighQualityBlur ? 0.3f : 0.02f)) { BlurRadius = () => HighQualityBlur ? 15 : 5 }, this);

            RenderMaterial.Value = () => new MaterialCompose(
                () => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.PanelMaterial().WithOverride(new() { ["BorderColor"] = () => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.OnSurface.WithAlpha(50) }),
                () => BlurMaterial.CachedValue
            );
        }

        public override void OnInternalStateChanged<T>(T value)
        {
            base.OnInternalStateChanged(value);
            Invalidate(Invalidation.SurfaceDirty);
        }

        protected override void Update()
        {
            base.Update();

            if (!FContext.GetCurrentWindow().IsNextFrameRendering()) return;
            Invalidate(Invalidation.SurfaceDirty);
        }

        public override void Render(SKCanvas canvas)
        {
            base.Render(canvas);

            using (var paint = GetRenderPaint())
            using (var panelPath = GetPanelPath())
            {
                RenderMaterial.CachedValue.DrawWithMaterial(canvas, panelPath, paint);
            }
        }
    }
}