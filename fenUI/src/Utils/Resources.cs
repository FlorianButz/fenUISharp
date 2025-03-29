using System.Reflection;
using FenUISharp.Themes;
using SkiaSharp;

namespace FenUISharp
{
    public static class Resources
    {
        private static Dictionary<string, SKTypeface> typefaces = new Dictionary<string, SKTypeface>();
        private static Dictionary<string, Theme> themes = new Dictionary<string, Theme>();

        public static void LoadDefault()
        {
            RegisterTypeface("Inter_18pt-Black.ttf", "inter-black");
            RegisterTypeface("Inter_18pt-Bold.ttf", "inter-bold");
            RegisterTypeface("Inter_18pt-Regular.ttf", "inter-regular");
            RegisterTypeface("Inter_18pt-Medium.ttf", "inter-medium");
            RegisterTypeface("Inter_18pt-Light.ttf", "inter-light");

            var darkTheme = new Theme
            {
                Primary             = new SKColor(54, 89, 174),
                PrimaryVariant      = new SKColor(38, 62, 123),
                Secondary           = new SKColor(82, 82, 82),
                SecondaryVariant    = new SKColor(65, 65, 65),
                Background          = new SKColor(32, 32, 32),
                Surface             = new SKColor(50, 50, 50),
                SurfaceVariant      = new SKColor(40, 40, 40),
                
                OnPrimary           = new SKColor(255, 255, 255),
                OnSecondary         = new SKColor(225, 225, 225),
                OnBackground        = new SKColor(200, 200, 200),
                OnSurface           = new SKColor(215, 215, 215),

                Shadow              = new SKColor(0, 0, 0, 45),

                DisabledMix         = new SKColor(255, 255, 255),
                HoveredMix          = new SKColor(150, 150, 150),
                PressedMix          = new SKColor(230, 230, 230),
                SelectedMix         = new SKColor(230, 230, 230),

                Error               = new SKColor(254, 64, 56),
                Success             = new SKColor(56, 254, 116),
                Warning             = new SKColor(254, 238, 56)
            };

            RegisterTheme(darkTheme, "default-dark");
        }

        public static SKTypeface RegisterTypeface(string fontName, string withId)
        {
            if (typefaces.Keys.Contains(withId)) return typefaces[withId];
            var assembly = Assembly.GetExecutingAssembly();

            var stream = assembly.GetManifestResourceStream("fenUI.fonts." + fontName);
            if (stream != null)
            {
                var typeface = SKTypeface.FromStream(stream);
                typefaces.Add(withId, typeface);
                return typeface;
            }

            throw new IOException("Font with the name " + fontName + " could not be found. ");
        }

        public static SKTypeface? GetTypeface(string id)
        {
            return typefaces.ContainsKey(id) ? typefaces[id] : null;
        }

        public static Theme RegisterTheme(Theme theme, string withId)
        {
            themes.Add(withId, theme);
            return theme;
        }

        public static Theme? GetTheme(string id)
        {
            return themes.ContainsKey(id) ? themes[id] : null;
        }
    }
}