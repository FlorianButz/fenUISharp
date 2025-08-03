using FenUISharp.Mathematics;
using FenUISharp.Objects.Buttons;
using FenUISharp.Objects.Text;
using FenUISharp.Objects.Text.Model;
using FenUISharp.States;
using SkiaSharp;

namespace FenUISharp.Objects
{
    public class FLargeSelectButton : SelectableButton
    {
        public FDisplayableType Display { get; protected set; }
        public FText Label { get; protected set; }

        public State<float> SelectedBorderThickness { get; init; }
        public State<SKColor> BaseColor { get; init; }

        public FLargeSelectButton(FDisplayableType display, FText label, Action? onClick = null, Action<bool, SelectableButton>? onSelectionChanged = null, Func<Vector2>? position = null, Func<Vector2>? size = null) :
            base(onClick, onSelectionChanged, position: position, size: size ?? (() => new(80, 60)))
        {
            RenderMaterial.SetResponsiveState(FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.PanelMaterial);
            CornerRadius.SetStaticState(20);
            BaseColor = new(() => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.Primary.Multiply(0.9f).Saturate(0.35f), this, this);
            SelectedBorderThickness = new(() => 3, this, this);

            ImageEffects.SelfOpacity.SetStaticState(0.38f);

            Display = display;
            display.SetParent(this);
            display.Transform.Size.SetStaticState(new(25, 25));

            Label = label;
            label.SetParent(this);
            label.Transform.Size.SetResponsiveState(() =>
            {
                var measure = label.LayoutModel.GetBoundingRect(label.Model, Shape.LocalBounds);
                return new(measure.Width, measure.Height);
            });
            label.Layout.Alignment.SetStaticState(new(0.5f, 1f));
            label.Layout.AlignmentAnchor.SetStaticState(new(0.5f, 0f));
            label.Transform.LocalPosition.SetStaticState(new(0f, 5f));
            label.Model = TextModelFactory.CopyBasic(label.Model, bold: true, align: new() { HorizontalAlign = Text.Layout.TextAlign.AlignType.Middle, VerticalAlign = Text.Layout.TextAlign.AlignType.Start });

            label.ImageEffects.Opacity.SetResponsiveState(() => IsSelected ? 0.9f : 0.6f);
        }

        float borderThickness = 0f;

        protected override void Update()
        {
            base.Update();

            var lastBT = borderThickness;
            borderThickness = RMath.Lerp(borderThickness, IsSelected ? SelectedBorderThickness.CachedValue : 0, FContext.DeltaTime * 10f);

            if (!RMath.Approximately(borderThickness, lastBT)) Invalidate(Invalidation.SurfaceDirty);
        }

        public override void Render(SKCanvas canvas)
        {
            // base.Render(canvas);

            using (var path = SKSquircle.CreateSquircle(Shape.LocalBounds, CornerRadius.CachedValue))
                RenderMaterial.CachedValue.WithOverride(new()
                {
                    ["BaseColor"] = () => BaseColor.CachedValue
                }).DrawWithMaterial(canvas, path, this);

            if (borderThickness > 0.1f)
            {
                var bounds = Shape.LocalBounds;
                bounds.Inflate(5, 5);
                bounds.Offset(0.5f, 0.5f);

                using var renderPaint = GetRenderPaint();
                renderPaint.Color = EnabledFillColor.CachedValue;
                renderPaint.IsStroke = true;
                renderPaint.StrokeWidth = borderThickness;

                using (var path = SKSquircle.CreateSquircle(bounds, CornerRadius.CachedValue * 1.3f))
                    canvas.DrawPath(path, renderPaint);
            }
        }
    }
}