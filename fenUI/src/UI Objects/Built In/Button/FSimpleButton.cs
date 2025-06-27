using FenUISharp.Behavior;
using FenUISharp.Mathematics;
using FenUISharp.Objects.Text;
using FenUISharp.States;
using SkiaSharp;

namespace FenUISharp.Objects.Buttons
{
    public class FSimpleButton : Button, IStateListener
    {
        public FText Label { get; protected set; }

        private AnimatorComponent animatorComponent;

        SKColor currenthighlight;
        SKColor currentcolor;

        public State<SKColor> BaseColor;
        public State<SKColor> BorderColor;

        private State<SKColor> highlight;

        float padding = 7.5f;
        float minWidth = 0;
        float maxWidth = 0;
        float cornerRadius = 10f;


        public FSimpleButton(FText label, Action? onClick = null, Func<Vector2>? position = null, float minWidth = 25, float maxWidth = 175) : base(onClick, position, () => new Vector2(0, 0))
        {
            this.OnClick = onClick;

            highlight = new(() => new SKColor(112, 102, 107, 100), this);
            BaseColor = new(() => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.Secondary, this);
            BorderColor = new(() => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.SecondaryBorder, this);

            this.maxWidth = RMath.Clamp(maxWidth, minWidth, float.MaxValue);
            this.minWidth = RMath.Clamp(minWidth, 0, this.maxWidth);

            Label = label;

            Label.SetParent(this);
            Label.OnAnyChange += RefreshLabel;
            RefreshLabel();

            currentcolor = FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.Secondary;
            currenthighlight = FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.SecondaryBorder;

            animatorComponent = new AnimatorComponent(this, Easing.EaseOutCubic);
            animatorComponent.Duration = 0.15f;

            InteractiveSurface.OnMouseEnter += MouseEnter;
            InteractiveSurface.OnMouseExit += MouseExit;

            animatorComponent.OnValueUpdate += (t) =>
            {
                var hoveredMix = RMath.Lerp(BaseColor.CachedValue, FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.HoveredMix, 0.2f);
                var hoveredHigh = RMath.Lerp(highlight.CachedValue, FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.HoveredMix, 0.2f);

                currentcolor = RMath.Lerp(BaseColor.CachedValue, hoveredMix, t);
                currenthighlight = RMath.Lerp(highlight.CachedValue, hoveredHigh, t);

                float pixelsAdd = 0.75f;
                float sx = (Transform.Size.CachedValue.x + pixelsAdd) / Transform.Size.CachedValue.x;
                float sy = (Transform.Size.CachedValue.y + pixelsAdd / 2) / Transform.Size.CachedValue.y;

                Transform.Scale.SetStaticState(Vector2.Lerp(new Vector2(1, 1), new Vector2(sx, sy), t));
                Invalidate(Invalidation.SurfaceDirty);
            };

            Transform.SnapPositionToPixelGrid.SetStaticState(true);

            Padding.SetStaticState(10);
            InteractiveSurface.ExtendInteractionRadius.SetStaticState(-10);

            UpdateColors();
        }

        public override void OnInternalStateChanged<T>(T value)
        {
            base.OnInternalStateChanged(value);
            UpdateColors();
        }

        void UpdateColors()
        {
            if (animatorComponent.IsRunning) return;

            var hoveredMix = RMath.Lerp(BaseColor.CachedValue, FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.HoveredMix, 0.2f);
            var hoveredHigh = RMath.Lerp(highlight.CachedValue, FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.HoveredMix, 0.2f);

            currentcolor = RMath.Lerp(BaseColor.CachedValue, hoveredMix, InteractiveSurface.IsMouseHovering ? 1 : 0);
            currenthighlight = RMath.Lerp(highlight.CachedValue, hoveredHigh, InteractiveSurface.IsMouseHovering ? 1 : 0);
        }

        void RefreshLabel()
        {
            var measuredText = Label.LayoutModel.GetBoundingRect(Label.Model, SKRect.Create(0, 0, maxWidth, 1000));

            float width = RMath.Clamp(measuredText.Width, minWidth, maxWidth);
            float height = RMath.Clamp(measuredText.Height, 20, 100);

            Label.Invalidate(Invalidation.SurfaceDirty | Invalidation.LayoutDirty);

            Label.Layout.StretchHorizontal.SetStaticState(true);
            Label.Layout.StretchVertical.SetStaticState(true);
            Label.Padding.SetStaticState(0);

            Transform.Size.SetStaticState(new Vector2(width + padding * 2.5f, height + padding * 0.5f));
        }

        protected void MouseEnter()
        {
            animatorComponent.Inverse = false;
            animatorComponent.Start();
        }

        protected void MouseExit()
        {
            animatorComponent.Inverse = true;
            animatorComponent.Start();
        }

        protected override void MouseAction(MouseInputCode inputCode)
        {
            base.MouseAction(inputCode);

            if (inputCode.button == MouseInputButton.Left && inputCode.state == MouseInputState.Down)
            {
                animatorComponent.Inverse = true;
                animatorComponent.Start();
            }
            else if (inputCode.button == MouseInputButton.Left && inputCode.state == MouseInputState.Up)
            {
                animatorComponent.Inverse = false;
                animatorComponent.Start();
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            Label.Dispose();
        }

        public override void Render(SKCanvas canvas)
        {
            base.Render(canvas);

            canvas.Translate(0.5f, 0.5f);

            using (var path = SKSquircle.CreateSquircle(Shape.LocalBounds, cornerRadius))
            {
                // Draw base rectangle
                using (var paint = GetRenderPaint())
                {
                    paint.IsAntialias = true;
                    paint.Style = SKPaintStyle.Fill;
                    paint.Color = currentcolor;

                    using (var shadow = SKImageFilter.CreateDropShadow(0, 2, 2, 2, FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.Shadow))
                        paint.ImageFilter = shadow;
                    // canvas.DrawRoundRect(roundRect, paint);
                    canvas.DrawPath(path, paint);
                }

                // Highlight on Top Edge
                using (var paint = GetRenderPaint())
                {
                    paint.IsAntialias = true;
                    paint.Shader = SKShader.CreateLinearGradient(
                        new SKPoint(Shape.LocalBounds.Left, Shape.LocalBounds.Top),
                        new SKPoint(Shape.LocalBounds.Left, Shape.LocalBounds.Top + 4f),
                        new SKColor[] { currenthighlight, SKColors.Transparent },
                        new float[] { 0.0f, 0.4f },
                        SKShaderTileMode.Clamp
                    );
                    // canvas.DrawRoundRect(roundRect, paint);
                    canvas.DrawPath(path, paint);
                }

                using (var paint = GetRenderPaint())
                {
                    paint.IsStroke = true;
                    paint.Color = BorderColor.CachedValue;
                    paint.StrokeWidth = 1;

                    canvas.DrawPath(path, paint);
                }

                // Inner Shadow
                using (var paint = GetRenderPaint())
                {
                    paint.IsAntialias = true;
                    paint.Shader = SKShader.CreateLinearGradient(
                        new SKPoint(Shape.LocalBounds.Left, Shape.LocalBounds.Bottom),
                        new SKPoint(Shape.LocalBounds.Left, Shape.LocalBounds.Top + 4f),
                        new SKColor[] { FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.Shadow, SKColors.Transparent },
                        new float[] { 0f, 1f },
                        SKShaderTileMode.Clamp
                    );
                    // canvas.DrawRoundRect(roundRect, paint);
                    canvas.DrawPath(path, paint);
                }
            }
        }
    }
}