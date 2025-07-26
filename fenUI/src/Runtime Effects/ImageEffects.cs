using FenUISharp.Mathematics;
using FenUISharp.Objects;
using FenUISharp.States;
using SkiaSharp;

namespace FenUISharp.Behavior.RuntimeEffects
{
    public class ImageEffects : BehaviorComponent, IStateListener
    {
        public State<float> SelfOpacity { get; init; }
        public State<float> Opacity { get; init; }
        public State<float> BlurRadius { get; init; }

        public State<float> Saturation { get; init; }
        public State<float> Brightness { get; init; }

        public State<bool> InheritFromParent { get; init; }

        public ImageEffects(UIObject owner) : base(owner)
        {
            SelfOpacity = new(() => 1f, Owner, this);
            SelfOpacity.SetResolver(StateResolverTemplates.SmallestFloatResolver);
            SelfOpacity.SetProcessor((x) => RMath.Clamp(x, 0, 1));
            Opacity = new(() => 1f, Owner, this);
            Opacity.SetResolver(StateResolverTemplates.SmallestFloatResolver);
            Opacity.SetProcessor((x) => RMath.Clamp(x, 0, 1));

            Saturation = new(() => 1f, Owner, this);
            Saturation.SetProcessor((x) => RMath.Clamp(x, 0, 4));

            Brightness = new(() => 1f, Owner, this);
            Brightness.SetProcessor((x) => RMath.Clamp(x, 0, 2));

            BlurRadius = new(() => 0f, Owner, this);
            BlurRadius.SetResolver(StateResolverTemplates.BiggestFloatResolver);
            BlurRadius.SetProcessor((x) => Math.Abs(x));

            InheritFromParent = new(() => true, Owner, this);
            Owner.Padding.SetResponsiveState(GetPadding, 10);
        }

        public override void HandleEvent(BehaviorEventType type, object? data = null)
        {
            base.HandleEvent(type, data);

            var capturedOwner = Owner;
            if (capturedOwner == null) return;

            switch (type)
            {
                case BehaviorEventType.AfterRender:
                    if (capturedOwner.ObjectSurface.TryGetSurface(out SKSurface surfaceO))
                        ApplyImageEffects(surfaceO, capturedOwner.Shape.SurfaceDrawRect);
                    break;
            }
        }

        public void ApplyImageEffects(SKSurface surface, SKRect bounds)
        {
            if (!AreEffectsApplied()) return;
            GetValues(out var values, false);

            using var snapshot = surface.Snapshot();
            using var paint = new SKPaint();

            surface.Canvas.Clear(SKColors.Transparent);

            if (values.blurRadius != 0)
            {
                using var blur = SKImageFilter.CreateBlur(values.blurRadius, values.blurRadius);
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

            paint.Color = SKColors.White.WithAlpha((byte)(values.opacity * 255));

            surface.Canvas.DrawImage(snapshot, bounds, paint);
        }

        public bool AreEffectsApplied()
        {
            GetValues(out var values, false);
            return
                values.opacity != 1f ||
                values.blurRadius != 0f ||
                values.saturation != 1f ||
                values.brightness != 1f;
        }

        public void GetValues(out (float opacity, float blurRadius, float saturation, float brightness) values, bool isChild)
        {
            var capturedOwner = Owner;
            
            if (capturedOwner?.Parent == null || !InheritFromParent.CachedValue)
            {
                values = (
                    Opacity.CachedValue * (!isChild ? SelfOpacity.CachedValue : 1f),
                    BlurRadius.CachedValue,
                    Saturation.CachedValue,
                    Brightness.CachedValue
                );
                return;
            }
            
            capturedOwner.Parent.ImageEffects.GetValues(out var vals, true);

            values = (
                Opacity.CachedValue * (InheritFromParent.CachedValue ? vals.opacity : 1f) * (!isChild ? SelfOpacity.CachedValue : 1f),
                Math.Max(BlurRadius.CachedValue, InheritFromParent.CachedValue ? vals.blurRadius : 0f),
                Saturation.CachedValue * (InheritFromParent.CachedValue ? vals.saturation : 1f),
                Brightness.CachedValue * (InheritFromParent.CachedValue ? vals.brightness : 1f)
            );
        }

        public int GetPadding()
        {
            return (int)MathF.Max(BlurRadius.CachedValue, 0);
        }

        public void OnInternalStateChanged<T>(T value)
        {
            Owner?.Invalidate(UIObject.Invalidation.SurfaceDirty);

            Owner?.Children.ForEach(x =>
            {
                x.ImageEffects.OnInternalStateChanged<T>(value);
            });
        }
    }
}