using SkiaSharp;

namespace FenUISharp.Themes
{
    public class Theme
    {
        public SKColor Primary { get; init; }
        public SKColor PrimaryVariant { get; init; }
        public SKColor PrimaryBorder { get; init; }
        public SKColor Secondary { get; init; }
        public SKColor SecondaryVariant { get; init; }
        public SKColor SecondaryBorder { get; init; }

        public SKColor Background { get; init; }
        public SKColor Surface { get; init; }
        public SKColor SurfaceVariant { get; init; }

        public SKColor OnPrimary { get; init; }
        public SKColor OnSecondary { get; init; }
        public SKColor OnBackground { get; init; }
        public SKColor OnSurface { get; init; }

        public SKColor Shadow { get; init; }

        public SKColor DisabledMix { get; init; }
        public SKColor HoveredMix { get; init; }
        public SKColor PressedMix { get; init; }
        public SKColor SelectedMix { get; init; }

        public SKColor Error { get; init; }
        public SKColor Success { get; init; }
        public SKColor Warning { get; init; }
    }

}