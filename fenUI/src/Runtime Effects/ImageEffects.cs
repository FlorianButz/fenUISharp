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

        public ImageEffects(UIObject owner) : base(owner)
        {
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

            Owner.Padding.SetResponsiveState(GetPadding, 10);
        }

        private bool _valuesChanged = true;
        private int? _savedLayer;
        private SKPaint? _filterPaint;

        public override void HandleEvent(BehaviorEventType type, out object outData, object? data = null)
        {
            base.HandleEvent(type, out outData, data);

            var capturedOwner = Owner;
            if (capturedOwner == null)
                return;

            capturedOwner.Visible.SetStaticState(Opacity.CachedValue != 0, 256);

            if (_valuesChanged)
            {
                _valuesChanged = false;
                _filterPaint = CreateLayerPaint();
            }

            _filterPaint ??= CreateLayerPaint();

            if (capturedOwner.HasVisibleChildren())
            {
                capturedOwner.ObjectSurface.TryGetSurface(out SKSurface surfaceO);
                switch (type)
                {
                    case BehaviorEventType.BeforeSurfaceDraw:
                        if (data == null || !AreEffectsApplied()) return;
                        SKCanvas beforeDraw = (SKCanvas)data;
                        _savedLayer = beforeDraw.SaveLayer(_filterPaint);
                        break;
                    case BehaviorEventType.AfterSurfaceDraw:
                        if (data == null || !AreEffectsApplied()) return;
                        SKCanvas afterDraw = (SKCanvas)data;
                        if (_savedLayer != null)
                            afterDraw.RestoreToCount(_savedLayer.Value);
                        _savedLayer = null;
                        break;
                }
            }
            else
            {
                switch (type)
                {
                    case BehaviorEventType.BeforeRender:
                        if (data == null || !AreEffectsApplied()) return;
                        SKCanvas beforeDraw = (SKCanvas)data;
                        _savedLayer = beforeDraw.SaveLayer(_filterPaint);
                        break;
                    case BehaviorEventType.AfterRender:
                        if (data == null || !AreEffectsApplied()) return;
                        SKCanvas afterDraw = (SKCanvas)data;
                        if (_savedLayer != null)
                            afterDraw.RestoreToCount(_savedLayer.Value);
                        _savedLayer = null;
                        break;
                }
            }
        }
        public SKPaint? CreateLayerPaint()
        {
            if (!AreEffectsApplied()) return null;
            var paint = new SKPaint();

            float blurRadius = BlurRadius.CachedValue;
            float saturation = Saturation.CachedValue;
            float brightness = Brightness.CachedValue;
            float opacity = Opacity.CachedValue;

            if (blurRadius != 0 && opacity != 0)
            {
                var blurQuality = RMath.Clamp(1f - ((blurRadius - 5f) / 12f), 0.25f, 1f);

                var scaleMatrix = SKMatrix.CreateScale(blurQuality, blurQuality);
                using var scaleFilter = SKImageFilter.CreateMatrix(scaleMatrix);
                var blurFilter = SKImageFilter.CreateBlur(blurRadius, blurRadius, scaleFilter);
                var inverseScaleMatrix = SKMatrix.CreateScale(1f / blurQuality, 1f / blurQuality);
                using var inverseScaleFilter = SKImageFilter.CreateMatrix(inverseScaleMatrix, SKSamplingOptions.Default, blurFilter);

                paint.ImageFilter = inverseScaleFilter;
            }

            if (saturation != 1 && opacity != 0)
            {
                float invSat = 1 - saturation;
                float R = 0.2126f * invSat;
                float G = 0.7152f * invSat;
                float B = 0.0722f * invSat;

                float[] colorMatrix = new float[]
                {
                    R + saturation, G, B, 0, 0,
                    R, G + saturation, B, 0, 0,
                    R, G, B + saturation, 0, 0,
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

            if (brightness != 1 && opacity != 0)
            {
                float[] colorMatrix = new float[]
                {
                    1 * brightness, 0, 0, 0, 0,
                    0, 1 * brightness, 0, 0, 0,
                    0, 0, 1 * brightness, 0, 0,
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

            paint.Color = SKColors.White.WithAlpha((byte)(opacity * 255));
            return paint;
        }

        public bool AreEffectsApplied()
        {
            float blurRadius = BlurRadius.CachedValue;
            float saturation = Saturation.CachedValue;
            float brightness = Brightness.CachedValue;
            float opacity = Opacity.CachedValue;

            return
                opacity != 1f ||
                blurRadius != 0f ||
                saturation != 1f ||
                brightness != 1f;
        }

        public int GetPadding()
        {
            return (int)MathF.Max(BlurRadius.CachedValue, 0);
        }

        public void OnInternalStateChanged<T>(T value)
        {
            _valuesChanged = true;

            if (!(Owner?.HasVisibleChildren() ?? true))
                Owner.Invalidate(UIObject.Invalidation.SurfaceDirty);
        }
    }
}