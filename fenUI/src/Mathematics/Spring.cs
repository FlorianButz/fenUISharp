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

        public void ResetVector(Vector2 resetValue)
        {
            xp = resetValue;
            yd = resetValue;
            y = resetValue;
        }

        public void SetValues(float speed = 2f, float springy = 0.4f)
        {
            springy = 1f / springy;
            var r = 0.1f;

            k1 = (float)(springy / (Math.PI * speed));
            k2 = (float)(1 / ((2 * Math.PI * speed) * (2 * Math.PI * speed)));
            k3 = (float)(r * springy / (2 * Math.PI * speed));
        }

        public Vector2 GetLastValue() => y;

        public Vector2 Update(float deltaTime, Vector2 x)
        {
            const float fixedDelta = 0.016f; // Stable timestep
            const int maxSteps = 5; // Avoid locking up on huge spikes
            float t = 0f;

            int steps = (int)MathF.Min(MathF.Ceiling(deltaTime / fixedDelta), maxSteps);
            float stepSize = deltaTime / steps;

            for (int i = 0; i < steps; i++)
            {
                float k2_stable = MathF.Max(k2, MathF.Max(stepSize * stepSize / 2 + stepSize * k1 / 2, stepSize * k1));
                y += yd * stepSize;
                yd += stepSize * (x + new Vector2(k3, k3) - y - (k1 * yd)) / k2_stable;
            }

            y.x = RMath.LimitDecimalPoints(y.x, 1);
            y.y = RMath.LimitDecimalPoints(y.y, 1);

            return y;
        }
    }
}
