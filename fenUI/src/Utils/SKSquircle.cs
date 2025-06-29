using SkiaSharp;
using System;

namespace FenUISharp
{
    public static class SKSquircle
    {
        public static SKPath CreateSquircle(SKRect rect, float cornerRadius, float? squircleness = null)
        {
            var path = new SKPath();
            path.Offset(rect.Left, rect.Top); // Initial position set

            squircleness = Math.Clamp(squircleness ?? FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.DefaultSuperellipseRatio, 0.0f, 1.0f);
            cornerRadius = Math.Min(cornerRadius, Math.Min(rect.Width, rect.Height) * 0.5f);

            float exponent = 2f + (1f - squircleness ?? FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.DefaultSuperellipseRatio) * 2f;
            int segments = 30;

            float left = rect.Left;
            float top = rect.Top;
            float right = rect.Right;
            float bottom = rect.Bottom;

            float width = rect.Width;
            float height = rect.Height;

            float cornerW = cornerRadius;
            float cornerH = cornerRadius;

            float innerLeft = left + cornerW;
            float innerTop = top + cornerH;
            float innerRight = right - cornerW;
            float innerBottom = bottom - cornerH;

            void DrawCorner(float centerX, float centerY, float angleOffset, bool moveTo)
            {
                for (int i = 0; i <= segments; i++)
                {
                    float t = i / (float)segments * (float)Math.PI / 2f;
                    float cosT = (float)Math.Cos(t + angleOffset);
                    float sinT = (float)Math.Sin(t + angleOffset);

                    float nx = MathF.Sign(cosT) * MathF.Pow(Math.Abs(cosT), 2f / exponent);
                    float ny = MathF.Sign(sinT) * MathF.Pow(Math.Abs(sinT), 2f / exponent);

                    float x = centerX + nx * cornerW;
                    float y = centerY + ny * cornerH;

                    if (i == 0 && moveTo)
                        path.MoveTo(x, y);
                    else
                        path.LineTo(x, y);
                }
            }

            // Top-left corner to top-right
            DrawCorner(innerLeft, innerTop, (float)Math.PI, true);
            path.LineTo(innerRight, innerTop - cornerH);

            // Top-right corner to bottom-right
            DrawCorner(innerRight, innerTop, 3f * (float)Math.PI / 2f, false);
            path.LineTo(innerRight + cornerW, innerBottom);

            // Bottom-right corner to bottom-left
            DrawCorner(innerRight, innerBottom, 0f, false);
            path.LineTo(innerLeft, innerBottom + cornerH);

            // Bottom-left corner to top-left
            DrawCorner(innerLeft, innerBottom, (float)Math.PI / 2f, false);
            path.Close();

            return path;
        }
    }
}
