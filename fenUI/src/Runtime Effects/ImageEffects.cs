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

        public State<bool> ApplyEffectsToChildren { get; init; }

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

            ApplyEffectsToChildren = new(() => false, this);
            Owner.Padding.SetResponsiveState(GetPadding, 10);
        }

        public override void ComponentDestroy()
        {
            base.ComponentDestroy();

            Opacity.Dispose();
            BlurRadius.Dispose();
            ApplyEffectsToChildren.Dispose();
        }

        public override void HandleEvent(BehaviorEventType type, object? data = null)
        {
            base.HandleEvent(type, data);

            switch (type)
            {
                case BehaviorEventType.AfterRender:
                    if (Owner._objectSurface.TryGetSurface(out SKSurface surfaceO))
                        ApplyImageEffects(surfaceO, Owner.Shape.SurfaceDrawRect);
                    break;

                case BehaviorEventType.AfterDrawChildren:
                    if (ApplyEffectsToChildren.CachedValue)
                    {
                        if (Owner._childSurface.TryGetSurface(out SKSurface surfaceC))
                            ApplyImageEffects(surfaceC, Owner.Shape.SurfaceDrawRect);
                    }
                    break;
            }
        }

        public void ApplyImageEffects(SKSurface surface, SKRect bounds)
        {
            if (!AreEffectsApplied()) return;

            using var snapshot = surface.Snapshot();
            using var paint = new SKPaint();

            surface.Canvas.Clear(SKColors.Transparent);

            if (BlurRadius.CachedValue != 0)
            {
                using var blur = SKImageFilter.CreateBlur(BlurRadius.CachedValue, BlurRadius.CachedValue);
                paint.ImageFilter = blur;
            }

            if (Saturation.CachedValue != 1)
            {
                float invSat = 1 - Saturation.CachedValue;
                float R = 0.2126f * invSat;
                float G = 0.7152f * invSat;
                float B = 0.0722f * invSat;

                float[] colorMatrix = new float[]
                {
                    R + Saturation.CachedValue, G, B, 0, 0,
                    R, G + Saturation.CachedValue, B, 0, 0,
                    R, G, B + Saturation.CachedValue, 0, 0,
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
            
            if (Brightness.CachedValue != 1)
            {
                float[] colorMatrix = new float[]
                {
                    1 * Brightness.CachedValue, 0, 0, 0, 0,
                    0, 1 * Brightness.CachedValue, 0, 0, 0,
                    0, 0, 1 * Brightness.CachedValue, 0, 0,
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
            return
                Opacity.CachedValue != 1f ||
                BlurRadius.CachedValue != 0f ||
                Saturation.CachedValue != 1f ||
                Brightness.CachedValue != 1f;
        }

        public int GetPadding()
        {
            return (int)MathF.Max(BlurRadius.CachedValue, 0);
        }

        public void OnInternalStateChanged<T>(T value)
        {
            Owner.Invalidate(UIObject.Invalidation.SurfaceDirty);
        }
    }
}