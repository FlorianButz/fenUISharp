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

        public static FTypeface Default = Resources.GetTypeface("inter-variable");

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

        protected SKTypeface GetCachedTypeface(string family, SKFontStyleWeight w, SKFontStyleSlant s, SKFontStyleWidth wd)
        {
            var key = (family, w, wd, s);
            if (!_cache.TryGetValue(key, out var tf))
            {
                tf = SKTypeface.FromFamilyName(family, w, wd, s);
                _cache[key] = tf;
            }
            return tf;
        }

        public virtual SKTypeface CreateSKTypeface(char c,
            SKFontStyleWeight weight = SKFontStyleWeight.Normal,
            SKFontStyleSlant slant = SKFontStyleSlant.Upright,
            SKFontStyleWidth width = SKFontStyleWidth.Normal)
        {
            var script = DetectScript(c);

            string family = FamilyName;
            if (ScriptFallbacks.TryGetValue(script, out var fallback))
                family = fallback;
            
            return GetCachedTypeface(family, weight, slant, width);
        }
    }

    public class FStreamedTypeface : FTypeface
    {
        private bool _isStreamed;
        private Dictionary<(SKFontStyleWeight weight, SKFontStyleSlant slant, SKFontStyleWidth width), SKTypeface> _streamedTypeface;

        public bool UseSystemFallback { get; set; } = true;
        public string SystemFallbackTypefaceName => "Segoe UI Variable";

        public FStreamedTypeface(string familyName) : base(familyName)
            => throw new InvalidOperationException("A streamed typeface cannot be initialized with a family name");

        /// <summary>
        /// Creates a FTypeface with streaming typeface capabilities. 
        /// Typefaces must be added with AddVariant before usage.
        /// If no given typeface for a style is registered, it will fallback
        /// to the first registered typeface.
        /// 
        /// If the UseSystemFallback option is chosen (by default true), it will use a
        /// system variable font that has all styles for a given style
        /// that is not registered.
        /// </summary>
        public FStreamedTypeface() : base("Streamed Font")
        {
            _isStreamed = true;
            _streamedTypeface = new();
        }

        /// <summary>
        /// Add a typeface variant for the given style options.
        /// The options reflect what the typeface looks like, as a
        /// streamed typeface must have the style built in.
        /// </summary>
        /// <param name="resourceStream">The given typeface stream</param>
        /// <param name="style">The style which the typeface already has</param>
        public void AddVariant(Stream? resourceStream, (SKFontStyleWeight weight, SKFontStyleSlant slant, SKFontStyleWidth width) style)
        {
            _streamedTypeface.Add(style, SKTypeface.FromStream(resourceStream));
        }

        public bool HasStyle((SKFontStyleWeight weight, SKFontStyleSlant slant, SKFontStyleWidth width) style)
            => _streamedTypeface.ContainsKey(style);

        public override SKTypeface CreateSKTypeface(char c,
            SKFontStyleWeight weight = SKFontStyleWeight.Normal,
            SKFontStyleSlant slant = SKFontStyleSlant.Upright,
            SKFontStyleWidth width = SKFontStyleWidth.Normal)
        {
            if (_streamedTypeface.Count == 0)
                throw new InvalidOperationException("Cannot use typeface without variants.");
            
            var script = DetectScript(c);

            if (ScriptFallbacks.TryGetValue(script, out var fallback))
                GetCachedTypeface(fallback, weight, slant, width);
            else
                if (_isStreamed && _streamedTypeface.TryGetValue((weight, slant, width), out SKTypeface? typeface))
                    return typeface;

            return UseSystemFallback ? GetCachedTypeface(SystemFallbackTypefaceName, weight, slant, width) : _streamedTypeface.Values.ElementAt(0);
        }
    }
}