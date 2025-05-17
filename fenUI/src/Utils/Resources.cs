using System.Reflection;
using FenUISharp.Components.Text.Model;
using FenUISharp.Themes;
using SkiaSharp;

namespace FenUISharp
{
    public static class Resources
    {
        private static Dictionary<string, FTypeface> typefaces = new Dictionary<string, FTypeface>();
        private static Dictionary<string, Theme> themes = new Dictionary<string, Theme>();
        private static Dictionary<string, SKImage> images = new Dictionary<string, SKImage>();

        public static void LoadDefault()
        {
            // RegisterTypeface("fonts/Inter-VariableFont_opsz,wght.ttf", "inter-variable");
            RegisterTypeface("Segoe UI Variable", "segoe-ui");

            var asm = typeof(Resources).Assembly;

            RegisterImage(SKImage.FromEncodedData(asm.GetManifestResourceStream($"{FenUI.ResourceLibName}.images.test_img.png")), "test-img");

            var darkTheme = new Theme
            {
                Primary = new SKColor(54, 89, 174),
                PrimaryVariant = new SKColor(38, 62, 123),

                Secondary = new SKColor(82, 82, 82),
                SecondaryVariant = new SKColor(65, 65, 65),

                Background = new SKColor(32, 32, 32),
                Surface = new SKColor(50, 50, 50),
                SurfaceVariant = new SKColor(40, 40, 40),

                OnPrimary = new SKColor(255, 255, 255),
                OnSecondary = new SKColor(225, 225, 225),
                OnBackground = new SKColor(200, 200, 200),
                OnSurface = new SKColor(215, 215, 215),

                PrimaryBorder = new SKColor(0, 0, 0, 0),
                SecondaryBorder = new SKColor(0, 0, 0, 0),

                Shadow = new SKColor(0, 0, 0, 45),

                DisabledMix = new SKColor(255, 255, 255),
                HoveredMix = new SKColor(150, 150, 150),
                PressedMix = new SKColor(230, 230, 230),
                SelectedMix = new SKColor(230, 230, 230),

                Error = new SKColor(254, 64, 56),
                Success = new SKColor(56, 254, 116),
                Warning = new SKColor(254, 238, 56)
            };

            var lightTheme = new Theme
            {
                Primary = new SKColor(54, 89, 174),
                PrimaryVariant = new SKColor(38, 62, 123),

                Secondary = new SKColor(245, 245, 245),
                SecondaryVariant = new SKColor(200, 200, 200),

                Background = new SKColor(245, 245, 245),
                Surface = new SKColor(255, 255, 255),
                SurfaceVariant = new SKColor(240, 240, 240),

                OnPrimary = new SKColor(255, 255, 255),
                OnSecondary = new SKColor(0, 0, 0),
                OnBackground = new SKColor(30, 30, 30),
                OnSurface = new SKColor(45, 45, 45),

                PrimaryBorder = new SKColor(122, 161, 255),
                SecondaryBorder = new SKColor(204, 204, 204),

                Shadow = new SKColor(0, 0, 0, 20),

                DisabledMix = new SKColor(200, 200, 200),
                HoveredMix = new SKColor(220, 220, 220),
                PressedMix = new SKColor(200, 200, 200),
                SelectedMix = new SKColor(210, 210, 210),

                Error = new SKColor(220, 38, 38),
                Success = new SKColor(16, 185, 129),
                Warning = new SKColor(250, 204, 21)
            };

            RegisterTheme(darkTheme, "default-dark");
            RegisterTheme(lightTheme, "default-light");
        }

        public static string ExtractResourceToTempFile<T>(string resourceName)
        {
            var asm = typeof(T).Assembly;
            using var stream = asm.GetManifestResourceStream(resourceName);
            if (stream == null) throw new Exception($"Resource not found: {resourceName}");

            var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.ico");
            using var fileStream = File.Create(tempPath);
            stream.CopyTo(fileStream);
            return tempPath;
        }

        public static FTypeface RegisterTypeface(string familyName, string withId)
        {
            if (typefaces.Keys.Contains(withId)) return typefaces[withId];

            var typeface = new FTypeface(familyName);
            typefaces.Add(withId, typeface);
            return typeface;

            throw new IOException("Font with the name " + familyName + " could not be found. ");
        }

        public static FTypeface GetTypeface(string id)
        {
            return typefaces.ContainsKey(id) ? typefaces[id] : new("Segoe UI Variable");
        }

        public static Theme RegisterTheme(Theme theme, string withId)
        {
            themes.Add(withId, theme);
            return theme;
        }

        public static Theme GetTheme(string id)
        {
            return themes.ContainsKey(id) ? themes[id] : new();
        }

        public static SKImage LoadImage(string path)
        {
            if (!File.Exists(path))
            {
                throw new ArgumentException("Image not found at: " + path);
            }

            var image = SKImage.FromEncodedData(path);
            return image;
        }

        public static SKImage RegisterImage(SKImage image, string withId)
        {
            images.Add(withId, image);
            return image;
        }

        public static SKImage GetImage(string id)
        {
            return images.ContainsKey(id) ? images[id] : SKImage.Create(SKImageInfo.Empty);
        }

        public static Uri ConvertMsAppxToUri(string msAppX)
        {
            if (msAppX.StartsWith("ms-appx:///"))
            {
                string relativePath = msAppX.Substring("ms-appx:///".Length);
                string basePath = AppDomain.CurrentDomain.BaseDirectory; // Your EXE directory
                return new Uri(Path.Combine(basePath, relativePath));
            }
            return new Uri(msAppX);
        }

        public static Uri GetUriFromPath(string path)
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory; // Your EXE directory
            return new Uri(Path.Combine(basePath, path));
        }
    }
}