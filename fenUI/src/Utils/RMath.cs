using SkiaSharp;

namespace FenUISharp
{
    public static class RMath
    {
        public static float Clamp(float value, float min, float max)
        {
            return value < min ? min : value > max ? max : value;
        }

        public static int Clamp(int value, int min, int max)
        {
            return value < min ? min : value > max ? max : value;
        }

        public static Vector2 Clamp(Vector2 value, Vector2 min, Vector2 max)
        {
            return new Vector2(Clamp(value.x, min.x, max.x), Clamp(value.y, min.y, max.y));
        }

        public static float Lerp(float from, float to, float t)
        {
            return from * (1 - t) + to * t;
        }

        public static byte Lerp(byte from, byte to, float t)
        {
            return (byte)(from * (1 - t) + to * t);
        }

        public static Vector2 Lerp(Vector2 from, Vector2 to, float t)
        {
            return new Vector2(Lerp(from.x, to.x, t), Lerp(from.y, to.y, t));
        }

        public static SKColor Lerp(SKColor from, SKColor to, float t)
        {
            return new SKColor(
                Lerp(from.Red, to.Red, t),
                Lerp(from.Green, to.Green, t),
                Lerp(from.Blue, to.Blue, t),
                Lerp(from.Alpha, to.Alpha, t)
            );
        }

        public static Vector2 RotateVector2(Vector2 point, Vector2 anchor, float d)
        {
            // Convert degrees to radians
            float radians = d * (float)Math.PI / 180f;

            float translatedX = point.x - anchor.x;
            float translatedY = point.y - anchor.y;

            float rotatedX = translatedX * (float)Math.Cos(radians) - translatedY * (float)Math.Sin(radians);
            float rotatedY = translatedX * (float)Math.Sin(radians) + translatedY * (float)Math.Cos(radians);

            rotatedX += anchor.x;
            rotatedY += anchor.y;

            return new Vector2(rotatedX, rotatedY);
        }

        public static Vector2 ScaleVector2(Vector2 point, Vector2 anchor, float s)
        {
            return new Vector2(((point.x - anchor.x) * s) + anchor.x, ((point.y - anchor.y) * s) + anchor.y);
        }

        public static Vector2 ScaleVector2(Vector2 point, Vector2 anchor, Vector2 s)
        {
            return new Vector2(((point.x - anchor.x) * s.x) + anchor.x, ((point.y - anchor.y) * s.y) + anchor.y);
        }

        public static bool ContainsPoint(SKRect rect, Vector2 point)
        {
            return rect.Left < point.x && rect.Top < point.y && (rect.Left + rect.Width) > point.x && (rect.Top + rect.Height) > point.y;
        }

        internal static float LimitDecimalPoints(float x, int v)
        {
            return (float)Math.Round(x, 2);
        }

        public static SKImage CreateLowResImage(SKImage sourceImage, float scaleFactor)
        {
            int newWidth = (int)(sourceImage.Width * scaleFactor);
            int newHeight = (int)(sourceImage.Height * scaleFactor);

            var info = new SKImageInfo(newWidth, newHeight);
            using (var surface = SKSurface.Create(info))
            {
                var canvas = surface.Canvas;

                // Draw the original image scaled down
                canvas.DrawImage(sourceImage,
                    SKRect.Create(0, 0, sourceImage.Width, sourceImage.Height),
                    SKRect.Create(0, 0, newWidth, newHeight),
                    Window.samplingOptions);

                return surface.Snapshot();
            }
        }
    }
}