using FenUISharp.Mathematics;
using SkiaSharp;

namespace FenUISharp.Mathematics
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

        public static float Lerp(float from, float to, float t)
        {
            return from * (1 - t) + to * t;
        }

        public static byte Lerp(byte from, byte to, float t)
        {
            return (byte)(from * (1 - t) + to * t);
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

        public static SKImage? CreateLowResImage(SKImage sourceImage, float scaleFactor, SKSamplingOptions samplingOptions)
        {
            if (sourceImage == null) return null;

            int newWidth = (int)(sourceImage.Width * scaleFactor);
            int newHeight = (int)(sourceImage.Height * scaleFactor);

            var info = new SKImageInfo(newWidth, newHeight);
            using (var surface = SKSurface.Create(info))
            {
                if (surface == null) return sourceImage;
                var canvas = surface.Canvas;
                if (canvas == null) return sourceImage;

                // Draw the original image scaled down
                canvas.DrawImage(sourceImage,
                    SKRect.Create(0, 0, sourceImage.Width, sourceImage.Height),
                    SKRect.Create(0, 0, newWidth, newHeight),
                    samplingOptions);

                return surface.Snapshot();
            }
        }

        public static SKImage? Combine(SKImage sourceImage1, SKImage sourceImage2, SKSamplingOptions samplingOptions)
        {
            if (sourceImage1 == null && sourceImage2 == null)
                return null;
            if (sourceImage1 == null) return sourceImage2;
            if (sourceImage2 == null) return sourceImage1;

            // If one is null or has zero size, return the other
            bool img1Valid = sourceImage1 != null && sourceImage1.Width > 0 && sourceImage1.Height > 0;
            bool img2Valid = sourceImage2 != null && sourceImage2.Width > 0 && sourceImage2.Height > 0;

            if (!img1Valid && !img2Valid)
                return null;

            if (!img1Valid) return sourceImage2;
            if (!img2Valid) return sourceImage1;

            var info = new SKImageInfo(RMath.Clamp(sourceImage1.Width, 1, 99999), RMath.Clamp(sourceImage1.Height, 1, 99999));
            using var surface = SKSurface.Create(info);

            var canvas = surface.Canvas;

            canvas.DrawImage(sourceImage1, SKRect.Create(0, 0, sourceImage1.Width, sourceImage1.Height), samplingOptions);
            canvas.DrawImage(sourceImage2, SKRect.Create(0, 0, sourceImage1.Width, sourceImage1.Height), samplingOptions);

            return surface.Snapshot();
        }

        public static bool IsImageValid(SKImage image)
        {
            return image != null &&
                image.Handle != IntPtr.Zero &&
                image.Width > 0 &&
                image.Height > 0;
        }

        public static bool IsRectFullyInside(SKRect outer, SKRect inner)
        {
            return inner.Left >= outer.Left &&
                   inner.Top >= outer.Top &&
                   inner.Right <= outer.Right &&
                   inner.Bottom <= outer.Bottom;
        }

        public static bool IsRectPartiallyInside(SKRect outer, SKRect inner)
        {
            float intersectLeft = Math.Max(outer.Left, inner.Left);
            float intersectTop = Math.Max(outer.Top, inner.Top);
            float intersectRight = Math.Min(outer.Right, inner.Right);
            float intersectBottom = Math.Min(outer.Bottom, inner.Bottom);

            return intersectLeft < intersectRight && intersectTop < intersectBottom;
        }

        public static bool IsRectPartiallyInside(SKRect rect, SKPath path)
        {
            // Check if any corner or midpoint of the rect is inside the path
            var pointsToCheck = new[]
            {
            new SKPoint(rect.Left,     rect.Top),
            new SKPoint(rect.Right,    rect.Top),
            new SKPoint(rect.Right,    rect.Bottom),
            new SKPoint(rect.Left,     rect.Bottom),
            new SKPoint(rect.MidX,     rect.Top),
            new SKPoint(rect.Right,    rect.MidY),
            new SKPoint(rect.MidX,     rect.Bottom),
            new SKPoint(rect.Left,     rect.MidY),
            new SKPoint(rect.MidX,     rect.MidY),
        };

            foreach (var point in pointsToCheck)
            {
                if (path.Contains(point.X, point.Y))
                    return true;
            }

            using var rectPath = new SKPath();
            rectPath.AddRect(rect);

            using var intersection = new SKPath();
            bool intersects = path.Op(rectPath, SKPathOp.Intersect, intersection);

            if (intersects && !intersection.IsEmpty)
                return true;

            return false;
        }


        public static float Remap(float t, float oldMin, float oldMax, float newMin, float newMax)
        {
            if (Math.Abs(oldMax - oldMin) < float.Epsilon)
                return newMin; // Avoid divide-by-zero, default to newMin

            float normalized = (t - oldMin) / (oldMax - oldMin);
            return newMin + normalized * (newMax - newMin);
        }

        public static bool Approximately(float value, float other)
        {
            return Approximately(value, other, 2);
        }

        public static bool Approximately(float value, float other, float exponent)
        {
            return Math.Round(value * MathF.Pow(10, exponent)) == Math.Round(other * MathF.Pow(10, exponent));
        }

        public static float InverseLerp(float a, float b, float value)
        {
            if (a == b) return 0;
            return (value - a) / (b - a);
        }
    }
}