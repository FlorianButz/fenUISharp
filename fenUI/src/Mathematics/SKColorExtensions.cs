using SkiaSharp;

namespace FenUISharp.Mathematics {
    public static class SKColorExtensions {
        public static SKColor MultiplyMix(this SKColor color1, SKColor color2){
            return new SKColor(
                (byte)((((float)color1.Red / 255)    * ((float)color2.Red / 255))    * 255),
                (byte)((((float)color1.Green / 255)  * ((float)color2.Green / 255))  * 255),
                (byte)((((float)color1.Blue / 255)   * ((float)color2.Blue / 255))   * 255),
                (byte)((((float)color1.Alpha / 255)  * ((float)color2.Alpha / 255))  * 255)
                );
        }
    }
}