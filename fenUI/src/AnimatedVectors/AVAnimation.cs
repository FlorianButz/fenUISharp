namespace FenUISharp.AnimatedVectors
{
    public class AVAnimation
    {
        public int[] AffectedPathIDs { get; init; } = new int[0];
        public bool UseObjectAnchor { get; init; } = false;
        public bool UseObjectSizeTranslation { get; init; } = false;
        public bool DontResetValues { get; init; } = false;
        public bool PerKeyframeEase { get; init; } = false;
        public float Duration { get; init; } = 1f;
        public float ExtendDuration { get; init; } = 0f;

        public Func<Func<float, float>> CreateEasing { get; init; } = () => (x) => x;

        public List<AVKeyframe> Keyframes { get; init; } = new();
    }

    public class AVKeyframe
    {
        public float time;
        public List<(string id, object value)> attributes;
    }
}