using FenUISharp.Mathematics;
using FenUISharp.Objects;
using FenUISharp.States;
using SkiaSharp;

namespace FenUISharp.Behavior.RuntimeEffects
{
    public class ImageEffects : BehaviorComponent, IStateListener
    {
        public State<float> Opacity { get; init; }
        public State<float> BlurRadius { get; init; }

        public State<float> Saturation { get; init; }
        public State<float> Brightness { get; init; }

        public State<bool> InheritFromParent { get; init; }

        public ImageEffects(UIObject owner) : base(owner)
        {
            Opacity = new(() => 1f, this);
            Opacity.SetResolver(StateResolverTemplates.SmallestFloatResolver);
            Opacity.SetProcessor((x) => RMath.Clamp(x, 0, 1));

            Saturation = new(() => 1f, this);
            Saturation.SetProcessor((x) => RMath.Clamp(x, 0, 4));

            Brightness = new(() => 1f, this);
            Brightness.SetProcessor((x) => RMath.Clamp(x, 0, 2));

            BlurRadius = new(() => 0f, this);
            BlurRadius.SetResolver(StateResolverTemplates.BiggestFloatResolver);
            BlurRadius.SetProcessor((x) => Math.Abs(x));

            InheritFromParent = new(() => true, this);
            Owner.Padding.SetResponsiveState(GetPadding, 10);
        }

        public override void ComponentDestroy()
        {
            base.ComponentDestroy();

            Opacity.Dispose();
            BlurRadius.Dispose();
            Saturation.Dispose();
            Brightness.Dispose();
            InheritFromParent.Dispose();
        }

        public override void HandleEvent(BehaviorEventType type, object? data = null)
        {
            base.HandleEvent(type, data);

            switch (type)
            {
                case BehaviorEventType.AfterRender:
                    if (Owner.ObjectSurface.TryGetSurface(out SKSurface surfaceO))
                        ApplyImageEffects(surfaceO, Owner.Shape.SurfaceDrawRect);
                    break;
            }
        }

        public void ApplyImageEffects(SKSurface surface, SKRect bounds)
        {
            if (!AreEffectsApplied()) return;
            GetValues(out var values);

            using var snapshot = surface.Snapshot();
            using var paint = new SKPaint();

            surface.Canvas.Clear(SKColors.Transparent);

            if (values.blurRadius != 0)
            {
                using var blur = SKImageFilter.CreateBlur(BlurRadius.CachedValue, BlurRadius.CachedValue);
                paint.ImageFilter = blur;
            }

            if (values.saturation != 1)
            {
                float invSat = 1 - values.saturation;
                float R = 0.2126f * invSat;
                float G = 0.7152f * invSat;
                float B = 0.0722f * invSat;

                float[] colorMatrix = new float[]
                {
                    R + values.saturation, G, B, 0, 0,
                    R, G + values.saturation, B, 0, 0,
                    R, G, B + values.saturation, 0, 0,
                    0, 0, 0, 1, 0
                };

                using var colorFilter = SKColorFilter.CreateColorMatrix(colorMatrix);
                using var colorImageFilter = SKImageFilter.CreateColorFilter(colorFilter);

                if (paint.ImageFilter == null) paint.ImageFilter = colorImageFilter;
                else
                {
                    using var compose = SKImageFilter.CreateCompose(paint.ImageFilter, colorImageFilter);
                    paint.ImageFilter = compose;
                }
            }
            
            if (values.brightness != 1)
            {
                float[] colorMatrix = new float[]
                {
                    1 * values.brightness, 0, 0, 0, 0,
                    0, 1 * values.brightness, 0, 0, 0,
                    0, 0, 1 * values.brightness, 0, 0,
                    0, 0, 0, 1, 0
                };

                using var colorFilter = SKColorFilter.CreateColorMatrix(colorMatrix);
                using var colorImageFilter = SKImageFilter.CreateColorFilter(colorFilter);

                if (paint.ImageFilter == null) paint.ImageFilter = colorImageFilter;
                else
                {
                    using var compose = SKImageFilter.CreateCompose(paint.ImageFilter, colorImageFilter);
                    paint.ImageFilter = compose;
                }
            }

            paint.Color = SKColors.White.WithAlpha((byte)(Opacity.CachedValue * 255));

            surface.Canvas.DrawImage(snapshot, bounds, paint);
        }

        public bool AreEffectsApplied()
        {
            GetValues(out var values);
            return
                values.opacity != 1f ||
                values.blurRadius != 0f ||
                values.saturation != 1f ||
                values.brightness != 1f;
        }

        public void GetValues(out (float opacity, float blurRadius, float saturation, float brightness) values)
        {
            values = (
                Opacity.CachedValue * (InheritFromParent.CachedValue ? Owner.Parent?.ImageEffects.Opacity.CachedValue : 1f) ?? 1,
                Math.Max(BlurRadius.CachedValue, (InheritFromParent.CachedValue ? Owner.Parent?.ImageEffects.BlurRadius.CachedValue : 0f) ?? 0),
                Saturation.CachedValue * (InheritFromParent.CachedValue ? Owner.Parent?.ImageEffects.Saturation.CachedValue : 1f) ?? 1,
                Brightness.CachedValue * (InheritFromParent.CachedValue ? Owner.Parent?.ImageEffects.Brightness.CachedValue : 1f) ?? 1
            );
        }

        public int GetPadding()
        {
            return (int)MathF.Max(BlurRadius.CachedValue, 0);
        }

        public void OnInternalStateChanged<T>(T value)
        {
            Owner.Invalidate(UIObject.Invalidation.SurfaceDirty);

            // Hacky solution for updating
            Owner.Children.ForEach(x =>
            {
                if (x.ImageEffects.InheritFromParent.CachedValue) x.Invalidate(UIObject.Invalidation.SurfaceDirty);
            });
        }
    }
}