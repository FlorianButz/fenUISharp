using SkiaSharp;

namespace FenUISharp
{
    public class FSimpleButton : UIComponent
    {
        public FLabel label;

        private AnimatorComponent animatorComponent;

        SKColor currenthighlight;
        SKColor currentcolor;

        SKColor basecolor = new SKColor(100, 90, 100);
        SKColor hovercolor = new SKColor(115, 105, 115);

        SKColor highlight = new SKColor(112, 102, 107, 210);
        SKColor hoverhighlight = new SKColor(135, 125, 125, 255);

        float padding = 5f;
        float minWidth = 0;
        float maxWidth = 0;
        float cornerRadius = 5.5f;

        public Action? onClick { get; set; }

        public FSimpleButton(Window root, Vector2 position, string text, Action? onClick = null, float minWidth = 25, float maxWidth = 175) : base(root, position, new Vector2(0, 0))
        {
            this.onClick = onClick;
            label = new FLabel(root, text, new Vector2(0, 0), new Vector2(0, 0), 12);

            this.maxWidth = RMath.Clamp(maxWidth, minWidth, float.MaxValue);
            this.minWidth = RMath.Clamp(minWidth, 0, this.maxWidth);

            SetText(text);

            label.transform.SetParent(transform);
            label.careAboutInteractions = false;
            WindowRoot.AddUIComponent(label);

            currentcolor = basecolor;
            currenthighlight = highlight;

            animatorComponent = new AnimatorComponent(this, Easing.EaseOutCubic);
            animatorComponent.duration = 0.2f;

            animatorComponent.onValueUpdate += (t) =>
            {
                currentcolor = RMath.Lerp(basecolor, hovercolor, t);
                currenthighlight = RMath.Lerp(highlight, hoverhighlight, t);

                float pixelsAdd = 1f;
                float sx = (transform.size.x + pixelsAdd) / transform.size.x;
                float sy = (transform.size.y + pixelsAdd / 2) / transform.size.y;

                transform.scale = RMath.Lerp(new Vector2(1, 1), new Vector2(sx, sy), t);
                Invalidate();
            };

            transform.boundsPadding.SetValue(this, 5, 100);

            components.Add(animatorComponent);
        }

        public void SetText(string text)
        {
            label.Text = text;

            float height = label.GetSingleLineTextHeight() + 1;
            float width = RMath.Clamp(label.GetSingleLineTextWidth(), minWidth, maxWidth);

            label.transform.size = new Vector2(width, height);
            transform.size = new Vector2(width + padding * 2.5f, height + padding);
        }

        protected override void MouseEnter()
        {
            base.MouseEnter();
            animatorComponent.inverse = false;
            animatorComponent.Start();
        }

        protected override void MouseExit()
        {
            base.MouseExit();
            animatorComponent.inverse = true;
            animatorComponent.Start();
        }

        protected override void MouseAction(MouseInputCode inputCode)
        {
            base.MouseAction(inputCode);

            if (inputCode.button == 0 && inputCode.state == 0)
            {
                animatorComponent.inverse = true;
                animatorComponent.Start();
            }
            else if (inputCode.button == 0 && inputCode.state == 1)
            {
                animatorComponent.inverse = false;
                animatorComponent.Start();
            }
        }

        protected override void ComponentDestroy()
        {
            base.ComponentDestroy();
            WindowRoot.DestroyUIComponent(label);
        }

        protected override void DrawToSurface(SKCanvas canvas)
        {
            var roundRect = new SKRoundRect(transform.localBounds, cornerRadius, cornerRadius);

            // Draw base rectangle
            using (var paint = skPaint.Clone())
            {
                paint.IsAntialias = true;
                paint.Style = SKPaintStyle.Fill;
                paint.Color = currentcolor;
                canvas.DrawRoundRect(roundRect, paint);
            }

            // Highlight on Top Edge
            using (var paint = skPaint.Clone())
            {
                paint.IsAntialias = true;
                paint.Shader = SKShader.CreateLinearGradient(
                    new SKPoint(transform.localBounds.Left, transform.localBounds.Top),
                    new SKPoint(transform.localBounds.Left, transform.localBounds.Top + 4f),
                    new SKColor[] { currenthighlight, SKColors.Transparent },
                    new float[] { 0.0f, 0.4f },
                    SKShaderTileMode.Clamp
                );
                canvas.DrawRoundRect(roundRect, paint);
            }

            // Inner Shadow
            using (var paint = skPaint.Clone())
            {
                paint.IsAntialias = true;
                paint.Shader = SKShader.CreateLinearGradient(
                    new SKPoint(transform.localBounds.Left, transform.localBounds.Bottom),
                    new SKPoint(transform.localBounds.Left, transform.localBounds.Top + 4f),
                    new SKColor[] { SKColors.Black.WithAlpha(35), SKColors.Transparent },
                    new float[] { 0f, 0.5f },
                    SKShaderTileMode.Clamp
                );
                canvas.DrawRoundRect(roundRect, paint);
            }
        }
    }
}