using SkiaSharp;
using System;

namespace FenUISharp
{
    public static class SKSquircle
    {
        public static SKPath CreateSquircle(SKRect rect, float cornerRadius, float squircleness = 0.5f)
        {
            var path = new SKPath();

            // Clamp parameters
            squircleness = Math.Clamp(squircleness, 0.01f, 1f);
            cornerRadius = Math.Min(Math.Min(rect.Width, rect.Height) / 2f, cornerRadius) * 2;

            // Calculate dimensions
            float left = rect.Left;
            float top = rect.Top;
            float right = rect.Right;
            float bottom = rect.Bottom;
            float width = rect.Width;
            float height = rect.Height;

            // Define corner regions
            float cornerWidth = Math.Min(cornerRadius, width / 2);
            float cornerHeight = Math.Min(cornerRadius, height / 2);

            // Define inner rectangle (where corners meet the straight edges)
            float innerLeft = left + cornerWidth;
            float innerTop = top + cornerHeight;
            float innerRight = right - cornerWidth;
            float innerBottom = bottom - cornerHeight;

            // Exponent for the superellipse (affects the squircleness)
            // Map 0-1 to ~8-2 (square to circle)
            float exponent = 2f + (1f - squircleness) * 2f;

            // Number of segments per corner
            int segments = 30;

            // Top-left corner
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments * (float)Math.PI / 2;
                float cosT = (float)Math.Cos(t + (float)Math.PI);
                float sinT = (float)Math.Sin(t + (float)Math.PI);

                float nx = MathF.Sign(cosT) * MathF.Pow(Math.Abs(cosT), 2f / exponent);
                float ny = MathF.Sign(sinT) * MathF.Pow(Math.Abs(sinT), 2f / exponent);

                float x = innerLeft + nx * cornerWidth;
                float y = innerTop + ny * cornerHeight;

                if (i == 0)
                    path.MoveTo(x, y);
                else
                    path.LineTo(x, y);
            }

            // Top-right corner
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments * (float)Math.PI / 2;
                float cosT = (float)Math.Cos(t + (float)Math.PI * 3 / 2);
                float sinT = (float)Math.Sin(t + (float)Math.PI * 3 / 2);

                float nx = MathF.Sign(cosT) * MathF.Pow(Math.Abs(cosT), 2f / exponent);
                float ny = MathF.Sign(sinT) * MathF.Pow(Math.Abs(sinT), 2f / exponent);

                float x = innerRight + nx * cornerWidth;
                float y = innerTop + ny * cornerHeight;

                path.LineTo(x, y);
            }

            // Bottom-right corner
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments * (float)Math.PI / 2;
                float cosT = (float)Math.Cos(t);
                float sinT = (float)Math.Sin(t);

                float nx = MathF.Sign(cosT) * MathF.Pow(Math.Abs(cosT), 2f / exponent);
                float ny = MathF.Sign(sinT) * MathF.Pow(Math.Abs(sinT), 2f / exponent);

                float x = innerRight + nx * cornerWidth;
                float y = innerBottom + ny * cornerHeight;

                path.LineTo(x, y);
            }

            // Bottom-left corner
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments * (float)Math.PI / 2;
                float cosT = (float)Math.Cos(t + (float)Math.PI / 2);
                float sinT = (float)Math.Sin(t + (float)Math.PI / 2);

                float nx = MathF.Sign(cosT) * MathF.Pow(Math.Abs(cosT), 2f / exponent);
                float ny = MathF.Sign(sinT) * MathF.Pow(Math.Abs(sinT), 2f / exponent);

                float x = innerLeft + nx * cornerWidth;
                float y = innerBottom + ny * cornerHeight;

                path.LineTo(x, y);
            }

            path.Close();
            return path;
        }
    }
}