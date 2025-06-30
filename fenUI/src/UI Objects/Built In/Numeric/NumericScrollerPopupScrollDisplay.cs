using FenUISharp.Materials;
using FenUISharp.Mathematics;
using FenUISharp.States;
using SkiaSharp;

namespace FenUISharp.Objects
{
    internal class NumericScrollerPopupScrollDisplay : UIObject, IStateListener
    {
        private float smoothedValue = 0f;
        public State<float> ReferenceValue { get; init; }
        public State<float> StepValue { get; init; }

        public Spring ValueSpring { get; set; }

        public int Lines { get; set; } = 7; // MUST be uneven
        public float LineThickness { get; set; } = 2f;

        private FImageButton leftArrow;
        private FImageButton rightArrow;

        private float? startValue = null;

        public NumericScrollerPopupScrollDisplay(Func<float> value, Func<float> step, Action? leftArrowClick = null, Action? rightArrowClick = null)
        {
            Layout.StretchHorizontal.SetStaticState(true);
            Layout.StretchVertical.SetStaticState(true);

            ValueSpring = new(2.25f, 2f);

            // Layout.MarginVertical.SetStaticState(-10);

            ReferenceValue = new(value, this);
            StepValue = new(step, this);

            const float btnSize = 20f;

            leftArrow = new FImageButton(new FImage(() => Resources.GetImage("fenui-builtin-arrow-left")), leftArrowClick, size: () => new(btnSize / 1.5f, btnSize));
            leftArrow.Transform.LocalPosition.SetStaticState(new(3.5f, 0f));
            leftArrow.Layout.Alignment.SetStaticState(new(0, 0.5f));
            leftArrow.Layout.AlignmentAnchor.SetStaticState(new(0, 0.5f));
            leftArrow.SetParent(this);

            rightArrow = new FImageButton(new FImage(() => Resources.GetImage("fenui-builtin-arrow-right")), rightArrowClick, size: () => new(btnSize / 1.5f, btnSize));
            rightArrow.Transform.LocalPosition.SetStaticState(new(-3.5f, 0));
            rightArrow.Layout.Alignment.SetStaticState(new(1, 0.5f));
            rightArrow.Layout.AlignmentAnchor.SetStaticState(new(1, 0.5f));
            rightArrow.SetParent(this);

            rightArrow.Image.TintColor.SetResponsiveState(() => rightArrow.InteractiveSurface.IsMouseHovering ? FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.OnSurface : SKColors.Transparent);
            leftArrow.Image.TintColor.SetResponsiveState(() => leftArrow.InteractiveSurface.IsMouseHovering ? FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.OnSurface : SKColors.Transparent);

            leftArrow.InteractiveSurface.OnMouseEnter += () => Invalidate(Invalidation.SurfaceDirty);
            leftArrow.InteractiveSurface.OnMouseExit += () => Invalidate(Invalidation.SurfaceDirty);
            rightArrow.InteractiveSurface.OnMouseEnter += () => Invalidate(Invalidation.SurfaceDirty);
            rightArrow.InteractiveSurface.OnMouseExit += () => Invalidate(Invalidation.SurfaceDirty);

            leftArrow.RenderMaterial.Value = () => new InteractableDefaultMaterial() { BaseColor = () => SKColors.Transparent, BorderColor = () => SKColors.Transparent, ShadowColor = () => SKColors.Transparent, HightlightColor = () => SKColors.Transparent };
            rightArrow.RenderMaterial.Value = () => new InteractableDefaultMaterial() { BaseColor = () => SKColors.Transparent, BorderColor = () => SKColors.Transparent, ShadowColor = () => SKColors.Transparent, HightlightColor = () => SKColors.Transparent };

            InteractiveSurface.EnableMouseActions.SetStaticState(true);
        }

        public override void Dispose()
        {
            base.Dispose();

            ReferenceValue.Dispose();
            StepValue.Dispose();
        }

        protected override void Update()
        {
            base.Update();

            if (startValue == null)
            {
                ValueSpring.ResetVector(new(ReferenceValue.CachedValue / StepValue.CachedValue, 0f));
            smoothedValue = ValueSpring.Update(FContext.DeltaTime, new(ReferenceValue.CachedValue / StepValue.CachedValue, 0f)).x;
                startValue = ReferenceValue.CachedValue / StepValue.CachedValue;
            }

            var lastVal = smoothedValue;
            smoothedValue = ValueSpring.Update(FContext.DeltaTime, new(ReferenceValue.CachedValue / StepValue.CachedValue, 0f)).x;
            if (lastVal != smoothedValue) Invalidate(Invalidation.SurfaceDirty);
        }

        public override void Render(SKCanvas canvas)
        {
            base.Render(canvas);

            int layer = canvas.SaveLayer();

            var lineBounds = Shape.LocalBounds;
            // lineBounds.Inflate(-20, 0);

            canvas.ClipRect(lineBounds);

            using var renderPaint = GetRenderPaint();

            for (int i = 0; i < Lines + 2; i++)
            {
                float x = lineBounds.Left + (lineBounds.Width / Lines) * ((i + (-smoothedValue % 1) - 1) + 0.5f) - LineThickness / 2;

                float dist = Math.Clamp(1f - (GetDistance(lineBounds.MidX, x) / 10), 0f, 1f);
                float height = lineBounds.Height / 2 + dist * 10;

                renderPaint.Color = FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.OnSurface.WithAlpha((byte)(dist * 128 + 127));

                float y = lineBounds.MidY - (height - dist * 10) / 2 + 1f;

                var rect = SKRect.Create(new SKPoint(x, y), new SKSize(LineThickness, height));
                using var rr = new SKRoundRect(rect, 5);

                canvas.DrawRoundRect(rr, renderPaint);
            }

            // renderPaint.Color = FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.Primary.AddMix(new SKColor(65, 65, 65));
            renderPaint.Color = FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.OnSurface;

            float circleX = RMath.Clamp(lineBounds.MidX + ((startValue??0) - smoothedValue) * lineBounds.Width / Lines, lineBounds.Left + 7.5f, lineBounds.Right - 7.5f);
            canvas.DrawCircle(new SKPoint(circleX, lineBounds.Top + 3f), 2, renderPaint);

            using var maskPaint = GetRenderPaint();
            using var shader = SKShader.CreateLinearGradient(
                    new(lineBounds.Left + (leftArrow.InteractiveSurface.IsMouseHovering ? 20 : 0), lineBounds.MidY),
                    new(lineBounds.Right - (rightArrow.InteractiveSurface.IsMouseHovering ? 20 : 0), lineBounds.MidY),
                    new SKColor[] {
                        SKColors.Transparent,
                        SKColors.White,
                        SKColors.Transparent
                    },
                    new float[] {
                        0f,
                        0.5f,
                        1f
                    },
                    SKShaderTileMode.Clamp
                );

            maskPaint.BlendMode = SKBlendMode.DstIn;
            maskPaint.Shader = shader;

            canvas?.DrawRect(Shape.LocalBounds, maskPaint);
            canvas?.RestoreToCount(layer);
        }

        float GetDistance(float one, float two)
        {
            return MathF.Abs(two - one);
        }
    }
}