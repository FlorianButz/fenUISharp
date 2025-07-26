using SkiaSharp;

namespace FenUISharp.Mathematics {
    public static class SKColorExtensions
    {
        public static SKColor MultiplyMix(this SKColor color1, SKColor color2)
        {
            return new SKColor(
                (byte)((((float)color1.Red / 255) * ((float)color2.Red / 255)) * 255),
                (byte)((((float)color1.Green / 255) * ((float)color2.Green / 255)) * 255),
                (byte)((((float)color1.Blue / 255) * ((float)color2.Blue / 255)) * 255),
                (byte)((((float)color1.Alpha / 255) * ((float)color2.Alpha / 255)) * 255)
                );
        }

        public static SKColor Multiply(this SKColor color1, (float, float, float, float) multiplier)
        {
            return new SKColor(
                (byte)((((float)color1.Red) * multiplier.Item1)),
                (byte)((((float)color1.Green) * multiplier.Item2)),
                (byte)((((float)color1.Blue) * multiplier.Item3)),
                (byte)((((float)color1.Alpha) * multiplier.Item4))
                );
        }

        public static SKColor Multiply(this SKColor color1, float multiplier)
        {
            return Multiply(color1, (multiplier, multiplier, multiplier, 1f));
        }

        public static SKColor AddMix(this SKColor color1, SKColor color2)
        {
            return new SKColor(
                (byte)RMath.Clamp(((((float)color1.Red / 255) + ((float)color2.Red / 255)) * 255), 0, 255),
                (byte)RMath.Clamp(((((float)color1.Green / 255) + ((float)color2.Green / 255)) * 255), 0, 255),
                (byte)RMath.Clamp(((((float)color1.Blue / 255) + ((float)color2.Blue / 255)) * 255), 0, 255),
                (byte)RMath.Clamp(((((float)color1.Alpha / 255) + ((float)color2.Alpha / 255)) * 255), 0, 255)
                );
        }

        public static SKColor Saturate(this SKColor color1, float saturation)
        {
            color1.ToHsl(out var h, out var s, out var l);
            return SKColor.FromHsl(h, RMath.Clamp(s * RMath.Clamp(saturation, 0, 999), 0, 100), l);
        }
    }
}