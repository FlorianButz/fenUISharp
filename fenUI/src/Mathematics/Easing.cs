namespace FenUISharp.Mathematics
{
    public class Easing
    {
        /// <summary>
        /// Combines two easing functions
        /// </summary>
        /// <param name="easeIn">The start easing function</param>
        /// <param name="easeOut">The ending easing function</param>
        /// <returns></returns>
        public static Func<float, float> CombineInOut(Func<float, float> easeIn, Func<float, float> easeOut)
        {
            return (x) =>
            {
                if (x < 0.5f)
                {
                    return 0.5f * easeIn(x * 2f);
                }
                else
                {
                    return 0.5f * easeOut((x - 0.5f) * 2f) + 0.5f;
                }
            };
        }

        public static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1;

            return (float)(1 + c3 * Math.Pow(x - 1, 3) + c1 * Math.Pow(x - 1, 2));
        }

        public static float EaseInBack(float x)
        {
            float c1 = 1.70158f;
            float c3 = c1 + 1;

            return c3 * x * x * x - c1 * x * x;
        }

        public static float EaseOutBackDramatic(float x)
        {
            const float c1 = 4.70158f;
            const float c3 = c1 + 1;

            return (float)(1 + c3 * Math.Pow(x - 1, 3) + c1 * Math.Pow(x - 1, 2));
        }

        public static float EaseInBackDramatic(float x)
        {
            float c1 = 4.70158f;
            float c3 = c1 + 1;

            return c3 * x * x * x - c1 * x * x;
        }

        public static float EaseOutIn(float x)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1;

            return (float)(c3 * x * x * x - c1 * x * x);
        }

        public static float EaseInQuint(float x)
        {
            return x * x * x * x * x;
        }

        public static float EaseOutQuint(float x)
        {
            //if (1 - (float)Math.Pow(1 - x, 5) >= 0.975f) return 1f;
            return 1 - (float)Math.Pow(1 - x, 5);
        }

        public static float EaseOutSin(float x)
        {
            return (float)Math.Sin((x * Math.PI) / 2);
        }

        public static float EaseInSin(float x)
        {
            return 1 - (float)Math.Cos((x * Math.PI) / 2);
        }

        public static float EaseOutCubic(float x)
        {
            return 1 - (float)Math.Pow(1 - x, 3);
        }

        public static float EaseInCubic(float x)
        {
            return x * x * x;
        }

        public static float EaseOutQuad(float x)
        {
            return 1 - (1 - x) * (1 - x);
        }

        public static float EaseInQuad(float x)
        {
            return x * x;
        }

        public static float EaseInQuart(float x)
        {
            return 1 - (float)Math.Pow(1 - x, 4);
        }

        public static float EaseOutQuart(float x)
        {
            return x * x * x * x;
        }

        public static float EaseOutBounce(float x)
        {
            float div = 2.75f;
            float mult = 7.5625f;

            if (x < 1 / div)
            {
                return mult * x * x;
            }
            else if (x < 2 / div)
            {
                x -= 1.5f / div;
                return mult * x * x + 0.75f;
            }
            else if (x < 2.5 / div)
            {
                x -= 2.25f / div;
                return mult * x * x + 0.9375f;
            }
            else
            {
                x -= 2.625f / div;
                return mult * x * x + 0.984375f;
            }
        }

        public static float EaseInBounce(float x)
        {
            return EaseOutBounce(1f - x);
        }

        public static float EaseOutElastic(float x)
        {
            float c4 = (float)(2 * Math.PI) / 3;

            return (float)(x == 0
            ? 0
            : x == 1
            ? 1
            : Math.Pow(2, -10 * x) * Math.Sin((x * 10 - 0.75) * c4) + 1);
        }

        public static float EaseInElastic(float x)
        {
            float c4 = (float)(2 * Math.PI) / 3;

            return (float)(x == 0
            ? 0
            : x == 1
            ? 1
            : -Math.Pow(2, 10 * x - 10) * Math.Sin((x * 10 - 10.75) * c4));
        }

        public static float EaseOutLessElastic(float x)
        {
            float c4 = 1;

            return (float)(x == 0
            ? 0
            : x == 1
            ? 1
            : Math.Pow(2, -10 * (x)) * Math.Sin(((x - 0.1) * 10 - 0.75) * c4) + 1);
        }

        public static float EaseOutExpo(float x)
        {
            return x == 1 ? 1 : 1 - (float)Math.Pow(2, -10 * x);
        }

        public static float EaseInExpo(float x)
        {
            return x == 0 ? 0 : (float)Math.Pow(2, 10 * x - 10);
        }
    }


    public static class BezierEasing
    {
        /// <summary>
        /// Creates a cubic Bezier easing function from four control point values.
        /// Equivalent to CSS cubic-bezier(x1, y1, x2, y2).
        /// </summary>
        /// <param name="x1">X coordinate of first control point (typically 0-1)</param>
        /// <param name="y1">Y coordinate of first control point</param>
        /// <param name="x2">X coordinate of second control point (typically 0-1)</param>
        /// <param name="y2">Y coordinate of second control point</param>
        /// <returns>A function that takes time (0-1) and returns eased value</returns>
        public static Func<float, float> CreateEasing(float x1, float y1, float x2, float y2)
        {
            return t => CubicBezier(t, x1, y1, x2, y2);
        }

        /// <summary>
        /// Evaluates a cubic Bezier curve at parameter t for easing.
        /// The curve starts at (0,0) and ends at (1,1) with control points (x1,y1) and (x2,y2).
        /// </summary>
        /// <param name="t">Time parameter (0-1)</param>
        /// <param name="x1">X coordinate of first control point</param>
        /// <param name="y1">Y coordinate of first control point</param>
        /// <param name="x2">X coordinate of second control point</param>
        /// <param name="y2">Y coordinate of second control point</param>
        /// <returns>Eased value</returns>
        public static float CubicBezier(float t, float x1, float y1, float x2, float y2)
        {
            t = Math.Max(0f, Math.Min(1f, t));

            // Use binary search to find the t value that gives us the desired x coordinate
            float tForX = SolveForT(t, x1, x2);
            return CalculateBezierY(tForX, y1, y2);
        }

        /// <summary>
        /// Solves for the t parameter that produces the given x coordinate
        /// </summary>
        private static float SolveForT(float x, float x1, float x2)
        {
            // Binary search to find t where Bezier X equals our target x
            float tMin = 0f;
            float tMax = 1f;
            float t = x; // Initial guess

            const float epsilon = 1e-6f;
            const int maxIterations = 10;

            for (int i = 0; i < maxIterations; i++)
            {
                float currentX = CalculateBezierX(t, x1, x2);
                float diff = currentX - x;

                if (Math.Abs(diff) < epsilon)
                    break;

                if (diff > 0)
                    tMax = t;
                else
                    tMin = t;

                t = (tMin + tMax) * 0.5f;
            }

            return t;
        }

        /// <summary>
        /// Calculates the X coordinate of the Bezier curve at parameter t
        /// </summary>
        private static float CalculateBezierX(float t, float x1, float x2)
        {
            // Cubic Bezier: B(t) = (1-t)³P₀ + 3(1-t)²tP₁ + 3(1-t)t²P₂ + t³P₃
            // For easing: P₀ = (0,0), P₁ = (x1,y1), P₂ = (x2,y2), P₃ = (1,1)
            float invT = 1f - t;
            return 3f * invT * invT * t * x1 + 3f * invT * t * t * x2 + t * t * t;
        }

        /// <summary>
        /// Calculates the Y coordinate of the Bezier curve at parameter t
        /// </summary>
        private static float CalculateBezierY(float t, float y1, float y2)
        {
            float invT = 1f - t;
            return 3f * invT * invT * t * y1 + 3f * invT * t * t * y2 + t * t * t;
        }

        // Common CSS easing presets
        public static readonly Func<float, float> Ease = CreateEasing(0.25f, 0.1f, 0.25f, 1f);
        public static readonly Func<float, float> EaseIn = CreateEasing(0.42f, 0f, 1f, 1f);
        public static readonly Func<float, float> EaseOut = CreateEasing(0f, 0f, 0.58f, 1f);
        public static readonly Func<float, float> EaseInOut = CreateEasing(0.42f, 0f, 0.58f, 1f);
    }
}
