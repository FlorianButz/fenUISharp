using FenUISharp.Components;
using FenUISharp.Components.Text;
using FenUISharp.Components.Text.Model;
using FenUISharp.Mathematics;
using SkiaSharp;

namespace FenUISharp
{
    public class FInternalMessageBox : UIComponent
    {
        public static void Create(Window window, string title, string message, Action? onClickOK = null, bool blurBackground = false)
        {
            FText text = new(window, Vector2.Zero, new(0, 45), TextModelFactory.CreateBasic(title, 20, true, textColor: window.WindowThemeManager.GetColor(t => t.OnSurface)));
            text.Transform.StretchHorizontal = true;
            text.Transform.Alignment = new(0.5f, 0f);
            text.Transform.Anchor= new(0.5f, 0f);

            FText desc = new(window, Vector2.Zero, new(0, 75), TextModelFactory.CreateBasic(message, 14, false, textColor: window.WindowThemeManager.GetColor(t => t.OnSurface.WithAlpha(100)),
                align: new() {HorizontalAlign = FenUISharp.Components.Text.Layout.TextAlign.AlignType.Start }));
            desc.Transform.StretchHorizontal = true;

            List<UIComponent> components = new();
            components.Add(text);
            components.Add(desc);

            var msg = new FInternalMessageBox(window, Vector2.Zero, Vector2.Zero, components, blurBackground);
        }

        private AnimatorComponent anim;
        private FPanel panel;

        public FInternalMessageBox(Window rootWindow, Vector2 position, Vector2 size, List<UIComponent> components, bool blurBackground) : base(rootWindow, position, size)
        {
            // TODO: Close when pressing ESC

            Transform.StretchHorizontal = true;
            Transform.StretchVertical = true;
            Transform.MarginHorizontal = 0;
            Transform.MarginVertical = 0;

            new UserScrollComponent(this);
            new UserDragComponent(this);

            if (blurBackground)
            {
                panel = new FBlurPane(rootWindow, Vector2.Zero, new(400, 175), backgroundColor: new(new SKColor(255, 255, 255, 10)), cornerRadius: 15);
                panel.Transform.SetParent(this.Transform);
            }
            else
            {
                panel = new(rootWindow, Vector2.Zero, new(400, 175), color: rootWindow.WindowThemeManager.GetColor(t => t.Background), cornerRadius: 15);
                panel.Transform.SetParent(this.Transform);
                panel.BorderColor = rootWindow.WindowThemeManager.GetColor(t => t.OnSurface.WithAlpha(50));
                panel.BorderSize = 1;
            }

            components.ForEach(x => { if (x.Transform.Parent == null) x.Transform.SetParent(panel.Transform); });

            anim = new(this, Easing.EaseOutQuint, Easing.EaseOutQuint);
            anim.Duration = 0.3f;
            anim.onValueUpdate += (x) =>
            {
                panel.Transform.Scale = Vector2.One * RMath.Remap(x, 0, 1, 0.85f, 1f);
                panel.ImageEffect.Opacity = RMath.Remap(x, 0, 1, 0f, 1f);
                opacity = RMath.Remap(x, 0, 1, 0f, 0.4f);

                Invalidate();
            };
            anim.Restart();
        }

        private float opacity = 0f;

        protected override void DrawToSurface(SKCanvas canvas)
        {
            var paint = SkPaint.Clone();
            paint.Color = SKColors.Black.WithAlpha((byte)Math.Clamp(opacity * 255, 0, 255));

            var bounds = Transform.LocalBounds;
            canvas.DrawRect(bounds, paint);
        }
    }
}