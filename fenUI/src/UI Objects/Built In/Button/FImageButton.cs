using FenUISharp.Behavior;
using FenUISharp.Mathematics;
using FenUISharp.Objects.Buttons;
using FenUISharp.States;
using SkiaSharp;

namespace FenUISharp.Objects
{
    public class FImageButton : Button, IStateListener
    {
        public FImage Image { get; protected set; }

        private AnimatorComponent animatorComponent;

        internal SKColor currenthighlight;
        internal SKColor currentcolor;

        public State<SKColor> BaseColor { get; init; }
        public State<SKColor> BorderColor { get; init; }
        public State<SKColor> ShadowColor { get; init; }

        private State<SKColor> highlight;

        float padding = 7.5f;
        float minWidth = 0;
        float maxWidth = 0;
        float cornerRadius = 10f;


        public FImageButton(FImage image, Action? onClick = null, Func<Vector2>? position = null, Func<Vector2>? size = null) : base(onClick, position, size)
        {
            this.OnClick = onClick;

            BaseColor = new(() => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.Secondary, this);
            BorderColor = new(() => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.SecondaryBorder, this);
            ShadowColor = new(() => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.Shadow, this);
            highlight = new(() => new SKColor(112, 102, 107, BaseColor.CachedValue.Alpha), this);

            this.maxWidth = RMath.Clamp(maxWidth, minWidth, float.MaxValue);
            this.minWidth = RMath.Clamp(minWidth, 0, this.maxWidth);

            Image = image;

            Image.SetParent(this);
            Image.Layout.StretchHorizontal.SetStaticState(true);
            Image.Layout.StretchVertical.SetStaticState(true);

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

                float pixelsAdd = PIXEL_ADD;
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

            Image.Dispose();
            BaseColor.Dispose();
            BorderColor.Dispose();
            ShadowColor.Dispose();
            highlight.Dispose();
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

                    using (var shadow = SKImageFilter.CreateDropShadow(0, 2, 2, 2, ShadowColor.CachedValue))
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