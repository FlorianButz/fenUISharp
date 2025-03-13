using System.Reflection;
using SkiaSharp;

namespace FenUISharp
{
    public static class FResources
    {
        private static Dictionary<string, SKTypeface> typefaces = new Dictionary<string, SKTypeface>();

        public static void LoadDefault(){
            LoadTypeface("Inter_18pt-Black.ttf", "inter-black");
            LoadTypeface("Inter_18pt-Bold.ttf", "inter-bold");
            LoadTypeface("Inter_18pt-Regular.ttf", "inter-regular");
            LoadTypeface("Inter_18pt-Medium.ttf", "inter-medium");
            LoadTypeface("Inter_18pt-Light.ttf", "inter-light");
        }

        public static SKTypeface LoadTypeface(string fontName, string withId)
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

        public static SKTypeface? GetTypeface(string typefaceId)
        {
            return typefaces.ContainsKey(typefaceId) ? typefaces[typefaceId] : null;
        }
    }
}