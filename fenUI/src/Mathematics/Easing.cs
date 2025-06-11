namespace FenUISharp.Mathematics
{
    public class Easing
    {
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

        public static float EaseOutExpo(float x)
        {
            return x == 1 ? 1 : 1 - (float)Math.Pow(2, -10 * x);
        }

        public static float EaseInExpo(float x)
        {
            return x == 0 ? 0 : (float)Math.Pow(2, 10 * x - 10);
        }
    }
}
