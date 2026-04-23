using SkiaSharp;

namespace FenUISharp.Objects.Text.Model
{
    public class FTypeface
    {
        public string FamilyName { get; init; }

        public Dictionary<UnicodeScript, string> ScriptFallbacks { get; } = new()
        {
            // CJK
            [UnicodeScript.Han] = "Microsoft YaHei UI",
            [UnicodeScript.Hiragana] = "Yu Gothic UI",
            [UnicodeScript.Katakana] = "Yu Gothic UI",
            [UnicodeScript.Hangul] = "Malgun Gothic",

            // RTL
            [UnicodeScript.Hebrew] = "Segoe UI",
            [UnicodeScript.Arabic] = "Segoe UI",

            // Indic / SEA
            [UnicodeScript.Devanagari] = "Nirmala UI",
            [UnicodeScript.Thai] = "Leelawadee UI",

            // Others
            [UnicodeScript.Cyrillic] = "Segoe UI"
        };

        public static FTypeface Default => new("Segoe UI Variable");

        public FTypeface(string familyName)
        {
            FamilyName = familyName;
        }

        public enum UnicodeScript { Latin, Han, Hiragana, Katakana, Hangul, Hebrew, Arabic, Devanagari, Thai, Cyrillic, Unknown }

        public static UnicodeScript DetectScript(char c)
        {
            int code = c;
            // Han (Ch)
            if (
                (code >= 0x4E00 && code <= 0x9FFF) ||
                (code >= 0x3400 && code <= 0x4DBF) ||
                (code >= 0x20000 && code <= 0x2A6DF)
            )
                return UnicodeScript.Han;
            // Jap
            if (code >= 0x3040 && code <= 0x309F)
                return UnicodeScript.Hiragana;
            if (code >= 0x30A0 && code <= 0x30FF)
                return UnicodeScript.Katakana;
            // Korean
            if (code >= 0xAC00 && code <= 0xD7AF)
                return UnicodeScript.Hangul;
            // Hebrew
            if (code >= 0x0590 && code <= 0x05FF)
                return UnicodeScript.Hebrew;
            // Arabic
            if (
                (code >= 0x0600 && code <= 0x06FF) ||
                (code >= 0x0750 && code <= 0x077F)
            )
                return UnicodeScript.Arabic;
            // Devanagari
            if (code >= 0x0900 && code <= 0x097F)
                return UnicodeScript.Devanagari;
            // Thai
            if (code >= 0x0E00 && code <= 0x0E7F)
                return UnicodeScript.Thai;
            // Cyrillic
            if (code >= 0x0400 && code <= 0x04FF)
                return UnicodeScript.Cyrillic;
            // Default
            return UnicodeScript.Latin;
        }

        private readonly Dictionary<(string, SKFontStyleWeight, SKFontStyleWidth, SKFontStyleSlant), SKTypeface> _cache = new();

        private SKTypeface GetCachedTypeface(string family, SKFontStyleWeight w, SKFontStyleWidth wd, SKFontStyleSlant s)
        {
            var key = (family, w, wd, s);
            if (!_cache.TryGetValue(key, out var tf))
            {
                tf = SKTypeface.FromFamilyName(family, w, wd, s);
                _cache[key] = tf;
            }
            return tf;
        }

        public SKTypeface CreateSKTypeface(char c,
            SKFontStyleWeight weight = SKFontStyleWeight.Normal,
            SKFontStyleSlant slant = SKFontStyleSlant.Upright,
            SKFontStyleWidth width = SKFontStyleWidth.Normal)
        {
            var script = DetectScript(c);

            string family = FamilyName;
            if (ScriptFallbacks.TryGetValue(script, out var fallback))
                family = fallback;

            return GetCachedTypeface(family, weight, width, slant);
        }
    }
}