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
    }
}
