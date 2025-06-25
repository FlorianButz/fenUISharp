using SkiaSharp;

namespace FenUISharp.Components.Text.Model
{
    public class FTypeface
    {
        public string FamilyName { get; init; }

        public static FTypeface Default => new("Segoe UI Variable");
        
        public FTypeface(string familyName)
        {
            FamilyName = familyName;
        }

        public SKTypeface CreateSKTypeface(SKFontStyleWeight weight = SKFontStyleWeight.Normal, SKFontStyleSlant slant = SKFontStyleSlant.Upright, SKFontStyleWidth width = SKFontStyleWidth.Normal)
        {
            return SKTypeface.FromFamilyName(FamilyName, weight, width, slant);
        }
    }
}