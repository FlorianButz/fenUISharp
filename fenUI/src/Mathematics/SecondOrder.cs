namespace FenUISharp.Mathematics
{
    public class Spring
    {
        private Vector2 xp;
        private Vector2 y, yd;
        private float k1, k2, k3;

        public Spring(Vector2 startValue, float speed = 2f, float springy = 0.4f, float r = 0.1f)
        {
            springy = 1f / springy; // Translate to actual springieness

            k1 = (float)(springy / (Math.PI * speed));
            k2 = (float)(1 / ((2 * Math.PI * speed) * (2 * Math.PI * speed)));
            k3 = (float)(r * springy / (2 * Math.PI * speed));

            xp = startValue;
            y = startValue;
            yd = new Vector2(0, 0);
        }

        public Spring(float speed = 2f, float springy = 0.4f)
        {
            springy = 1f / springy;

            var startValue = new Vector2(0, 0);
            var r = 0.1f;

            k1 = (float)(springy / (Math.PI * speed));
            k2 = (float)(1 / ((2 * Math.PI * speed) * (2 * Math.PI * speed)));
            k3 = (float)(r * springy / (2 * Math.PI * speed));

            xp = startValue;
            y = startValue;
            yd = new Vector2(0, 0);
        }


        public void SetValues(float f = 2f, float z = 0.4f, float r = 0.1f)
        {
            k1 = (float)(z / (Math.PI * f));
            k2 = (float)(1 / ((2 * Math.PI * f) * (2 * Math.PI * f)));
            k3 = (float)(r * z / (2 * Math.PI * f));
        }

        public Vector2 Update(float T, Vector2 x)
        {
            float k2_stable = (float)Math.Max(k2, Math.Max(T * T / 2 + T * k1 / 2, T * k1));
            y = y + new Vector2(T, T) * yd;
            yd = yd + T * (x + new Vector2(k3, k3) - y - (k1 * yd)) / k2_stable;

            y.x = RMath.LimitDecimalPoints(y.x, 1);
            y.y = RMath.LimitDecimalPoints(y.y, 1);

            return y;
        }
    }
}
