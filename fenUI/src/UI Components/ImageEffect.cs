using FenUISharp.Components;
using FenUISharp.Mathematics;
using SkiaSharp;

namespace FenUISharp
{
    public class ImageEffect
    {
        public UIComponent Parent { get; init; }

        public bool InheritValues { get; set; } = true;

        // Effects which are only applied to the cached version

        private float _blurRadius = 0f;
        public float BlurRadius
        {
            get => (Parent.Transform.Parent != null && InheritValues) ? Math.Max(_blurRadius, Parent.Transform.Parent.ParentComponent.ImageEffect.BlurRadius) : _blurRadius;
            set { _blurRadius = value; OnRequireInvalidation(); }
        }

        // Effects which are applied every frame

        private float _opacity = 1f;
        public float Opacity
        {
            get => (Parent.Transform.Parent != null && InheritValues) ? _opacity * Parent.Transform.Parent.ParentComponent.ImageEffect.Opacity : _opacity;
            set { _opacity = value; }
        }

        private float _brightness = 1f;
        public float Brightness
        {
            get => (Parent.Transform.Parent != null && InheritValues) ? _brightness * Parent.Transform.Parent.ParentComponent.ImageEffect.Brightness : _brightness;
            set { _brightness = value; }
        }

        private float _contrast = 1f;
        public float Contrast
        {
            get => (Parent.Transform.Parent != null && InheritValues) ? _contrast * Parent.Transform.Parent.ParentComponent.ImageEffect.Contrast : _contrast;
            set { _contrast = value; }
        }

        private float _saturation = 1f;
        public float Saturation
        {
            get => (Parent.Transform.Parent != null && InheritValues) ? _saturation * Parent.Transform.Parent.ParentComponent.ImageEffect.Saturation : _saturation;
            set { _saturation = value; }
        }

        private SKColor _tint = SKColors.White;
        public SKColor Tint
        {
            get => (Parent.Transform.Parent != null && InheritValues) ? _tint.MultiplyMix(Parent.Transform.Parent.ParentComponent.ImageEffect.Tint) : _tint;
            set { _tint = value; }
        }

        private SKColor _add = SKColors.Black;
        public SKColor Add
        {
            get => (Parent.Transform.Parent != null && InheritValues) ? _add.MultiplyMix(Parent.Transform.Parent.ParentComponent.ImageEffect.Add) : _add;
            set { _add = value; }
        }

        public ImageEffect(UIComponent parent)
        {
            Parent = parent;
        }

        public void OnRequireInvalidation()
        {
            Parent.Invalidate();
        }

        public SKPaint ApplyImageEffect(in SKPaint paint)
        {
            SKImageFilter? finalImageFilter = null;
            SKColorFilter? finalColorFilter = null;

            if (Opacity != 1)
            {
                var opacityColor = OpacityColor(Opacity);
                finalColorFilter = ComposeColorFilter(finalColorFilter, opacityColor);
            }

            if (Brightness != 1)
            {
                var lightnessColor = BrightnessColor(Brightness);
                finalColorFilter = ComposeColorFilter(finalColorFilter, lightnessColor);
            }

            if (Contrast != 1)
            {
                var contrastColor = ContrastFilter(Contrast);
                finalColorFilter = ComposeColorFilter(finalColorFilter, contrastColor);
            }

            if (Saturation != 1)
            {
                var saturationColor = SaturationFilter(Saturation);
                finalColorFilter = ComposeColorFilter(finalColorFilter, saturationColor);
            }

            if (Tint != SKColors.White || Add != SKColors.Black)
            {
                var tintAddColor = TintAddColor(Tint, Add);
                finalColorFilter = ComposeColorFilter(finalColorFilter, tintAddColor);
            }

            if (finalImageFilter != null)
                paint.ImageFilter = finalImageFilter;
            if (finalColorFilter != null)
                paint.ColorFilter = finalColorFilter;

            return paint;
        }

        public SKPaint ApplyInsideCacheImageEffect(in SKPaint paint)
        {
            SKImageFilter? finalImageFilter = null;
            SKColorFilter? finalColorFilter = null;

            if (BlurRadius > 0)
            {
                var blur = SKImageFilter.CreateBlur(BlurRadius, BlurRadius);
                finalImageFilter = ComposeImageFilter(finalImageFilter, blur);
            }

            if (finalImageFilter != null)
                paint.ImageFilter = finalImageFilter;
            if (finalColorFilter != null)
                paint.ColorFilter = finalColorFilter;

            return paint;
        }

        SKImageFilter ComposeImageFilter(SKImageFilter? inner, SKImageFilter outer)
        {
            if (inner == null) return outer;
            else return SKImageFilter.CreateCompose(inner, outer);
        }

        SKColorFilter? ComposeColorFilter(SKColorFilter? inner, SKColorFilter? outer)
        {
            if (inner == null && outer != null) return outer;
            else if (outer == null && inner != null) return inner;
            else if (outer == null && inner == null) return null;
            else return SKColorFilter.CreateCompose(inner, outer);
        }

        static SKColorFilter OpacityColor(float value)
        {
            return SKColorFilter.CreateColorMatrix(new float[]
            {
                1, 0, 0, 0, 0,
                0, 1, 0, 0, 0,
                0, 0, 1, 0, 0,
                0, 0, 0, value, 0
            });
        }

        static SKColorFilter ContrastFilter(float value)
        {
            float avgLuminance = (1 - value) * 0.5f;

            return SKColorFilter.CreateColorMatrix(new float[]
            {
                value, 0, 0, 0, avgLuminance,
                0, value, 0, 0, avgLuminance,
                0, 0, value, 0, avgLuminance,
                0, 0, 0, 1, 0
            });
        }

        static SKColorFilter BrightnessColor(float brightness)
        {
            brightness = Math.Clamp(brightness, 0f, 2f);

            return SKColorFilter.CreateColorMatrix(new float[]
            {
                brightness, 0, 0, 0, 0,
                0, brightness, 0, 0, 0,
                0, 0, brightness, 0, 0,
                0, 0, 0, 1, 0
            });
        }

        static SKColorFilter TintAddColor(SKColor tint, SKColor add)
        {
            return SKColorFilter.CreateColorMatrix(new float[]
            {
                (float)tint.Red / 255, 0, 0, 0, (float)add.Red,
                0, (float)tint.Blue / 255, 0, 0, (float)add.Green,
                0, 0, (float)tint.Green / 255, 0, (float)add.Blue,
                0, 0, 0, 1, 0
            });
        }

        static SKColorFilter SaturationFilter(float saturation)
        {
            float lumR = 0.2126f;
            float lumG = 0.7152f;
            float lumB = 0.0722f;

            return SKColorFilter.CreateColorMatrix(new float[]
            {
                lumR + (1 - lumR) * saturation, lumG - lumG * saturation,     lumB - lumB * saturation,     0, 0,
                lumR - lumR * saturation,     lumG + (1 - lumG) * saturation, lumB - lumB * saturation,     0, 0,
                lumR - lumR * saturation,     lumG - lumG * saturation,     lumB + (1 - lumB) * saturation, 0, 0,
                0, 0, 0, 1, 0
            });
        }
    }
}