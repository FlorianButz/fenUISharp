using SkiaSharp;

namespace FenUISharp.Themes
{
    public class Theme
    {
        public SKColor Primary { get; set; }
        public SKColor PrimaryVariant { get; set; }
        public SKColor PrimaryBorder { get; set; }
        public SKColor Secondary { get; set; }
        public SKColor SecondaryVariant { get; set; }
        public SKColor SecondaryBorder { get; set; }

        public SKColor Background { get; set; }
        public SKColor Surface { get; set; }
        public SKColor SurfaceVariant { get; set; }

        public SKColor OnPrimary { get; set; }
        public SKColor OnSecondary { get; set; }
        public SKColor OnBackground { get; set; }
        public SKColor OnSurface { get; set; }

        public SKColor Shadow { get; set; }

        public SKColor DisabledMix { get; set; }
        public SKColor HoveredMix { get; set; }
        public SKColor PressedMix { get; set; }
        public SKColor SelectedMix { get; set; }

        public SKColor Error { get; set; }
        public SKColor Success { get; set; }
        public SKColor Warning { get; set; }

        public Theme Clone()
        {
            return (Theme)this.MemberwiseClone();
        }
    }
}