using FenUISharp.Mathematics;

namespace FenUISharp.AnimatedVectors
{
    public class AVPathAnimationOverride
    {
        public bool UseObjectAnchor { get; set; }
        public bool UseObjectSizeTranslation { get; set; }

        public Vector2 Anchor = new(0.5f, 0.5f);
        public Vector2 Translation = new(0f, 0f);
        public Vector2 Scale = new(1f, 1f);
        public float Rotation = 0f;
        public float Opacity = 1f;
        public float BlurRadius = 0f;
        public float StrokeTrace = 1f;
    }
}