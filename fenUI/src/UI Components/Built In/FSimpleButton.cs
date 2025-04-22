using FenUISharp.Components.Text;
using FenUISharp.Components.Text.Layout;
using FenUISharp.Components.Text.Model;
using FenUISharp.Mathematics;
using FenUISharp.Themes;
using SkiaSharp;

namespace FenUISharp.Components
{
    public class FSimpleButton : UIComponent
    {
        public FText Label { get; protected set; }

        private string labelText = "";

        private AnimatorComponent animatorComponent;

        SKColor currenthighlight;
        SKColor currentcolor;

        private ThemeColor _color;
        public ThemeColor BaseColor
        {
            get => _color;
            set
            {
                _color = value;
                Invalidate();
            }
        }

        private ThemeColor _border;
        public ThemeColor Border
        {
            get => _border;
            set
            {
                _border = value;
                Invalidate();
            }
        }

        private ThemeColor _textColor;
        public ThemeColor TextColor
        {
            get => _textColor;
            set
            {
                _textColor = value;
                CreateModel();
            }
        }

        ThemeColor highlight;

        float padding = 7.5f;
        float minWidth = 0;
        float maxWidth = 0;
        float cornerRadius = 6f;

        public Action? OnClick { get; set; }

        public FSimpleButton(Window root, Vector2 position, string text, Action? onClick = null, float minWidth = 25, float maxWidth = 175,
            ThemeColor? color = null, ThemeColor? textColor = null) : base(root, position, new Vector2(0, 0))
        {
            this.OnClick = onClick;
            Label = new FText(root, new Vector2(0, 0), new Vector2(0, 0), TextModelFactory.CreateBasic(text));

            var wrap = new WrapLayout(Label) { AllowLinebreakOnOverflow = false };
            Label.Layout = wrap;

            highlight = new ThemeColor(new SKColor(112, 102, 107, 100));
            _color = color ?? WindowRoot.WindowThemeManager.GetColor(t => t.Secondary);
            _textColor = textColor ?? WindowRoot.WindowThemeManager.GetColor(t => t.OnSecondary);
            _border = WindowRoot.WindowThemeManager.GetColor(t => t.SecondaryBorder);

            this.maxWidth = RMath.Clamp(maxWidth, minWidth, float.MaxValue);
            this.minWidth = RMath.Clamp(minWidth, 0, this.maxWidth);

            SetText(text);

            WindowRoot.WindowThemeManager.ThemeChanged += UpdateColors;

            Label.Transform.SetParent(Transform);
            Label.CareAboutInteractions = false;
            WindowRoot.AddUIComponent(Label);

            currentcolor = BaseColor.Value;
            currenthighlight = highlight.Value;

            animatorComponent = new AnimatorComponent(this, Easing.EaseOutCubic);
            animatorComponent.Duration = 0.2f;

            animatorComponent.onValueUpdate += (t) =>
            {
                var hoveredMix = RMath.Lerp(BaseColor.Value, WindowRoot.WindowThemeManager.GetColor(t => t.HoveredMix).Value, 0.2f);
                var hoveredHigh = RMath.Lerp(highlight.Value, WindowRoot.WindowThemeManager.GetColor(t => t.HoveredMix).Value, 0.2f);

                currentcolor = RMath.Lerp(BaseColor.Value, hoveredMix, t);
                currenthighlight = RMath.Lerp(highlight.Value, hoveredHigh, t);

                float pixelsAdd = 0.75f;
                float sx = (Transform.Size.x + pixelsAdd) / Transform.Size.x;
                float sy = (Transform.Size.y + pixelsAdd / 2) / Transform.Size.y;

                Transform.Scale = Vector2.Lerp(new Vector2(1, 1), new Vector2(sx, sy), t);
                Invalidate();
            };

            Transform.BoundsPadding.SetValue(this, 10, 100);

            Components.Add(animatorComponent);
        }

        void UpdateColors()
        {
            var hoveredMix = RMath.Lerp(BaseColor.Value, WindowRoot.WindowThemeManager.GetColor(t => t.HoveredMix).Value, 0.2f);
            var hoveredHigh = RMath.Lerp(highlight.Value, WindowRoot.WindowThemeManager.GetColor(t => t.HoveredMix).Value, 0.2f);

            currentcolor = RMath.Lerp(BaseColor.Value, hoveredMix, _isMouseHovering ? 1 : 0);
            currenthighlight = RMath.Lerp(highlight.Value, hoveredHigh, _isMouseHovering ? 1 : 0);
        }

        void CreateModel()
        {
            Label.Model = TextModelFactory.CreateBasic(labelText, textColor: _textColor);
        }

        public void SetText(string text)
        {
            labelText = text;
            CreateModel();

            // float height = Label.Layout.GetSingleLineTextHeight() + 1;
            // float height = 25;

            var measuredText = Label.Layout.GetBoundingRect(Label.Model, SKRect.Create(0, 0, maxWidth, 1000));

            float width = RMath.Clamp(measuredText.Width, minWidth, maxWidth);
            float height = RMath.Clamp(measuredText.Height, 25, 100);

            Label.Transform.Size = new Vector2(width, height);
            Label.Invalidate();

            Transform.Size = new Vector2(width + padding * 2.5f, height + padding * 0.5f);
        }

        protected override void MouseEnter()
        {
            base.MouseEnter();
            animatorComponent.Inverse = false;
            animatorComponent.Start();
        }

        protected override void MouseExit()
        {
            base.MouseExit();
            animatorComponent.Inverse = true;
            animatorComponent.Start();
        }

        protected override void MouseAction(MouseInputCode inputCode)
        {
            base.MouseAction(inputCode);

            if (inputCode.button == 0 && inputCode.state == 0)
            {
                animatorComponent.Inverse = true;
                animatorComponent.Start();
            }
            else if (inputCode.button == 0 && inputCode.state == 1)
            {
                animatorComponent.Inverse = false;
                animatorComponent.Start();

                OnClick?.Invoke();
            }
        }

        protected override void ComponentDestroy()
        {
            base.ComponentDestroy();
            WindowRoot.DestroyUIComponent(Label);

            WindowRoot.WindowThemeManager.ThemeChanged -= UpdateColors;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
        }

        protected override void DrawToSurface(SKCanvas canvas)
        {
            // using (var roundRect = new SKRoundRect(Transform.LocalBounds, cornerRadius, cornerRadius))
            {
                // Draw base rectangle
                using (var paint = SkPaint.Clone())
                {
                    paint.IsAntialias = true;
                    paint.Style = SKPaintStyle.Fill;
                    paint.Color = currentcolor;

                    using (var shadow = SKImageFilter.CreateDropShadow(0, 2, 2, 2, WindowRoot.WindowThemeManager.GetColor(t => t.Shadow).Value))
                        paint.ImageFilter = shadow;
                    // canvas.DrawRoundRect(roundRect, paint);
                    canvas.DrawPath(SKSquircle.CreateSquircle(Transform.LocalBounds, cornerRadius), paint);
                }

                // Highlight on Top Edge
                using (var paint = SkPaint.Clone())
                {
                    paint.IsAntialias = true;
                    paint.Shader = SKShader.CreateLinearGradient(
                        new SKPoint(Transform.LocalBounds.Left, Transform.LocalBounds.Top),
                        new SKPoint(Transform.LocalBounds.Left, Transform.LocalBounds.Top + 4f),
                        new SKColor[] { currenthighlight, SKColors.Transparent },
                        new float[] { 0.0f, 0.4f },
                        SKShaderTileMode.Clamp
                    );
                    // canvas.DrawRoundRect(roundRect, paint);
                    canvas.DrawPath(SKSquircle.CreateSquircle(Transform.LocalBounds, cornerRadius), paint);
                }

                using (var paint = SkPaint.Clone())
                {
                    paint.IsStroke = true;
                    paint.Color = _border.Value;
                    paint.StrokeWidth = 1;

                    canvas.Translate(0.5f, 0.5f);
                    canvas.DrawPath(SKSquircle.CreateSquircle(Transform.LocalBounds, cornerRadius), paint);
                    canvas.Translate(-0.5f, -0.5f);
                }

                // Inner Shadow
                using (var paint = SkPaint.Clone())
                {
                    paint.IsAntialias = true;
                    paint.Shader = SKShader.CreateLinearGradient(
                        new SKPoint(Transform.LocalBounds.Left, Transform.LocalBounds.Bottom),
                        new SKPoint(Transform.LocalBounds.Left, Transform.LocalBounds.Top + 4f),
                        new SKColor[] { WindowRoot.WindowThemeManager.GetColor(t => t.Shadow).Value, SKColors.Transparent },
                        new float[] { 0f, 1f },
                        SKShaderTileMode.Clamp
                    );
                    // canvas.DrawRoundRect(roundRect, paint);
                    canvas.DrawPath(SKSquircle.CreateSquircle(Transform.LocalBounds, cornerRadius), paint);
                }
            }
        }
    }
}