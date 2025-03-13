namespace FenUISharp
{
    public class SecondOrder
    {
        private Vector2 xp;
        private Vector2 y, yd;
        private float k1, k2, k3;

        public SecondOrder(Vector2 x0, float f = 2f, float z = 0.4f, float r = 0.1f)
        {
            k1 = (float)(z / (Math.PI * f));
            k2 = (float)(1 / ((2 * Math.PI * f) * (2 * Math.PI * f)));
            k3 = (float)(r * z / (2 * Math.PI * f));

            xp = x0;
            y = x0;
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

            y.x = FMath.LimitDecimalPoints(y.x, 1);
            y.y = FMath.LimitDecimalPoints(y.y, 1);

            return y;
        }
    }
}
