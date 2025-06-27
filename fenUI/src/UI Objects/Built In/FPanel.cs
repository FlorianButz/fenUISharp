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
        public State<float> DropShadowRadius { get; private init; }
        public State<float> BorderSize { get; private init; }

        public State<SKColor> PanelColor { get; private init; }
        public State<SKColor> ShadowColor { get; private init; }
        public State<SKColor> BorderColor { get; private init; }

        public State<bool> UseSquircle { get; private init; }

        protected bool _drawBasePanel = true;

        public FPanel(Func<Vector2>? position = null, Func<Vector2>? size = null) : base(position, size)
        {
            CornerRadius = new(() => 20, this);
            DropShadowRadius = new(() => 5, this);
            BorderSize = new(() => 2, this);

            UseSquircle = new(() => true, this);

            PanelColor = new(() => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.Surface, this);
            ShadowColor = new(() => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.Shadow, this);
            BorderColor = new(() => PanelColor.CachedValue.AddMix(new SKColor(50, 50, 50)), this);

            Transform.SnapPositionToPixelGrid.SetStaticState(true);

            Padding.Value = () => 20;
        }

        public override void Render(SKCanvas canvas)
        {
            base.Render(canvas);
            RenderBasePanel(canvas, Shape.LocalBounds);
        }

        protected virtual void RenderBasePanel(SKCanvas canvas, SKRect rect)
        {
            if(BorderSize.CachedValue % 2 == 1)
                canvas.Translate(0.5f, 0.5f);

            var paint = GetRenderPaint();
            paint.Color = PanelColor.CachedValue;

            using var panelPath = GetPanelPath(rect);
            using (var dropShadow = SKImageFilter.CreateDropShadow(0, 2, DropShadowRadius.CachedValue, DropShadowRadius.CachedValue, ShadowColor.CachedValue))
                paint.ImageFilter = dropShadow;

            using (var strokePaint = paint.Clone())
            {
                strokePaint.IsStroke = true;

                strokePaint.StrokeCap = SKStrokeCap.Round;
                strokePaint.StrokeJoin = SKStrokeJoin.Round;

                strokePaint.ImageFilter = null; // Remove shadow

                strokePaint.Color = GetDarkStrokeColor();
                strokePaint.StrokeWidth = BorderSize.CachedValue + 2f * (BorderSize.CachedValue / 2);
                canvas.DrawPath(panelPath, strokePaint);

                strokePaint.Color = BorderColor.CachedValue;
                strokePaint.StrokeWidth = BorderSize.CachedValue;

                var strokeRect = SKRect.Create((float)Math.Round(Shape.LocalBounds.Left), (float)Math.Round(Shape.LocalBounds.Top), Shape.LocalBounds.Width, Shape.LocalBounds.Height);
                canvas.DrawPath(panelPath, strokePaint);
            }

            if (_drawBasePanel)
                canvas.DrawPath(panelPath, paint);
        }

        protected SKColor GetDarkStrokeColor()
        {
            // return BorderColor.CachedValue.MultiplyMix(new SKColor(50, 50, 50, 150));
            return FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.Background.MultiplyMix(new SKColor(200, 200, 200, 150));
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
            DropShadowRadius.Dispose();
            BorderSize.Dispose();

            UseSquircle.Dispose();

            PanelColor.Dispose();
            ShadowColor.Dispose();
            BorderColor.Dispose();
        }
    }
}