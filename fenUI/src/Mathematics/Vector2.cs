namespace FenUISharp.Mathematics {

    public struct Vector2
    {
        public float x, y;

        public float Magnitude => (float)Math.Sqrt(Math.Abs(x * x + y * y));
        public Vector2 Swapped => new Vector2(y, x);

        public Vector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public Vector2(Vector2 v)
        {
            this.x = v.x;
            this.y = v.y;
        }

        public void Swap()
        {
            var cX = x;
            x = y;
            y = cX;
        }

        public static Vector2 Clamp(Vector2 value, Vector2 min, Vector2 max)
        {
            return new Vector2(RMath.Clamp(value.x, min.x, max.x), RMath.Clamp(value.y, min.y, max.y));
        }

        public static Vector2 Lerp(Vector2 from, Vector2 to, float t)
        {
            return new Vector2(RMath.Lerp(from.x, to.x, t), RMath.Lerp(from.y, to.y, t));
        }

        public static Vector2 operator *(Vector2 c1, Vector2 c2)
        {
            return new Vector2(c1.x * c2.x, c1.y * c2.y);
        }

        public static Vector2 operator +(Vector2 c1, Vector2 c2)
        {
            return new Vector2(c1.x + c2.x, c1.y + c2.y);
        }

        public static Vector2 operator -(Vector2 c1, Vector2 c2)
        {
            return new Vector2(c1.x - c2.x, c1.y - c2.y);
        }

        public static Vector2 operator /(Vector2 c1, Vector2 c2)
        {
            return new Vector2(c1.x / c2.x, c1.y / c2.y);
        }

        public static Vector2 operator +(Vector2 c1, float c2)
        {
            return new Vector2(c1.x + c2, c1.y + c2);
        }

        public static Vector2 operator *(Vector2 c1, float c2)
        {
            return new Vector2(c1.x * c2, c1.y * c2);
        }

        public static Vector2 operator *(float c2, Vector2 c1)
        {
            return new Vector2(c1.x * c2, c1.y * c2);
        }

        public static Vector2 operator /(Vector2 c1, float c2)
        {
            return new Vector2(c1.x / c2, c1.y / c2);
        }

        public static Vector2 operator -(Vector2 c1, float c2)
        {
            return new Vector2(c1.x - c2, c1.y - c2);
        }

        public static Vector2 operator /(float c1, Vector2 c2)
        {
            return new Vector2(c1 / c2.x, c1 / c2.y);
        }

        public static Vector2 operator -(float c1, Vector2 c2)
        {
            return new Vector2(c1 - c2.x, c1 - c2.y);
        }

        public override readonly bool Equals(object obj)
        {
            if (obj is Vector2)
                return x == ((Vector2)obj).x && y == ((Vector2)obj).y;

            return false;
        }

        public static bool operator ==(Vector2 c1, Vector2 c2)
        {
            return c1.x == c2.x && c1.y == c2.y;
        }

        public static bool operator !=(Vector2 c1, Vector2 c2)
        {
            return c1.x != c2.x || c1.y != c2.y;
        }

        public override string ToString()
        {
            return $"({x}, {y})";
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(x, y);
        }
    }
}