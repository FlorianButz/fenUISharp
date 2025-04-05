using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FenUISharp
{
    public class Easing
    {
        public static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1;

            return (float)(1 + c3 * Math.Pow(t - 1, 3) + c1 * Math.Pow(t - 1, 2));
        }

        public static float EaseOutIn(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1;

            return (float)(c3 * t * t * t - c1 * t * t);
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
