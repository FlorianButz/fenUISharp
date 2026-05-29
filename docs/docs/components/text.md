# Text and Input

Text display and input components with rich text support via `TextModel` and `TextSpan`.

## TextModel

The `TextModel` is the data model for text rendering. It holds a list of styled `TextSpan` parts, a typeface, and alignment.

```csharp
TextModel model = new(
    textParts: new List<TextSpan>(),
    align: new TextAlign(),
    typeface: FTypeface.Default
);
```

### TextSpan

Each `TextSpan` represents a run of text with its own `TextStyle`:

```csharp
TextSpan span = new("Hello ", new TextStyle()
{
    FontSize = 16,
    Weight = SKFontStyleWeight.Bold,
    Color = () => SKColors.Red,
    BackgroundColor = () => SKColors.LightGray,
    Underlined = false,
    BlurRadius = 0,
    Opacity = 1
});
```

### TextStyle

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Weight` | `SKFontStyleWeight` | Normal | Font weight |
| `Slant` | `SKFontStyleSlant` | Upright | Font slant (Upright, Italic, Oblique) |
| `Underlined` | `bool` | false | Draw underline |
| `FontSize` | `float` | 16 | Font size in points |
| `Color` | `Func<SKColor>` | Theme OnSurface | Text color |
| `BackgroundColor` | `Func<SKColor>` | Transparent | Background highlight color |
| `BlurRadius` | `float` | 0 | Per-glyph blur |
| `Opacity` | `float` | 1 | Per-glyph opacity |

## TextModelFactory

Convenience methods for creating common text models:

```csharp
// Basic single-span text
TextModel basic = TextModelFactory.CreateBasic(
    "Hello World",
    textSize: 14,
    bold: false,
    italic: false,
    underlined: false,
    textColor: () => SKColors.White,
    backgroundColor: () => SKColors.Transparent,
    align: new TextAlign()
);

// Copy with overrides
TextModel copied = TextModelFactory.CopyBasic(
    oldModel,
    textSize: 18,
    bold: true
);

// Copy with new text (preserving style of first span)
TextModel newText = TextModelFactory.CopyBasicNew("New text", oldModel);
```

> **NOTE**
> These helpers fall apart if more complex styles have been built. They are only recommended for uniform text (same style for every character)

## FText

The `FText` UIObject displays a `TextModel` using a `TextLayout` and `TextRenderer`:

```csharp
FText text = new(
    TextModelFactory.CreateBasic("Hello World", textSize: 16),
    size: () => new Vector2(200, 50)
);
text.SetParent(panel);
```

### Dynamic Properties

```csharp
text.Model = newModel;       // Swap text content (invalidates layout)
text.LayoutModel = layout;   // Swap layout processor
text.Renderer = renderer;    // Swap render processor
```

### Change Events

```csharp
text.OnModelChanged += () => { };
text.OnLayoutChanged += () => { };
text.OnRendererChanged += () => { };
text.OnAnyChange += () => { };
```

## TextLayout

An abstract class that converts a `TextModel` and bounding rectangle into positioned `Glyph` objects:

```csharp
public abstract List<Glyph> ProcessModel(TextModel model, SKRect bounds);
```

### WrapLayout (Default)

The default layout that wraps text to fit within bounds, with optional ellipsis truncation:

```csharp
WrapLayout layout = new(text)
{
    EllipsisChar = '\u2026',            // Character for truncation
    AllowLinebreakChar = true,          // Allow \n to force line breaks
    AllowLinebreakOnOverflow = true,    // Auto-wrap on overflow
    AllowEllipsis = true                // Truncate with ellipsis when height exceeded
};
```

The layout processes text in two passes:
1. **CalculateLayout**: Breaks text into lines, measuring each character's width with the correct font for that script
2. **PositionGlyphs**: Positions glyphs according to horizontal and vertical alignment

### TextAlign

```csharp
TextAlign align = new()
{
    HorizontalAlign = TextAlign.AlignType.Start,    // Start, Middle, End
    VerticalAlign = TextAlign.AlignType.Middle      // Start, Middle, End
};
```

## Layout Processors

`LayoutProcessor` extends `TextLayout` and wraps an inner layout, allowing animation effects to be applied to the glyph positions returned by the inner layout.

```csharp
// Base pattern: wrap an inner layout and process its output
public class MyProcessor : LayoutProcessor
{
    public MyProcessor(FText parent, TextLayout inner) : base(parent, inner) { }

    public override List<Glyph> ProcessModel(TextModel model, SKRect bounds)
    {
        var glyphs = base.ProcessModel(model, bounds);
        // Modify glyph positions/scales
        return glyphs;
    }
}
```

### Built-in Layout Processors

All processors wrap an inner `TextLayout` (usually `WrapLayout`):

```csharp
FText text = new(TextModelFactory.CreateBasic("Animated!"));

// Wiggle each character
text.LayoutModel = new WiggleCharsLayoutProcessor(text, new WrapLayout(text));

// Animated number counting
text.LayoutModel = new NumericTextLayoutProcessor(text, new WrapLayout(text));

// Typewriter reveal
text.LayoutModel = new TypewriterLayoutProcessor(text, new WrapLayout(text));

// Typewriter + shatter exit
text.LayoutModel = new TypewriterShatterLayoutProcessor(text, new WrapLayout(text));

// Particle explosion
text.LayoutModel = new ParticleExplosionLayoutProcessor(text, new WrapLayout(text));

// Blur reveal
text.LayoutModel = new BlurLayoutProcessor(text, new WrapLayout(text));
```

#### WiggleCharsLayoutProcessor
Per-character sine-wave position and rotation animation. Characters oscillate vertically and rotate, creating a wiggling effect.

#### NumericTextLayoutProcessor
Designed for numeric displays. Characters slide vertically when the number changes, with old digits sliding out and new digits sliding in.

#### TypewriterLayoutProcessor
Reveals text one character at a time with a typewriter effect. Characters fade or slide in sequentially.

#### TypewriterShatterLayoutProcessor
Combines typewriter reveal with a shatter / particle exit effect for when characters are removed.

#### ParticleExplosionLayoutProcessor
Characters explode outward and rematerialize.

#### BlurLayoutProcessor
Characters slide in /out while also being blurred in / out. A more primitive version of `NumericTextLayoutProcessor`

## TextRenderer

Renders `Glyph` objects to a `SKCanvas`. Each glyph is drawn with proper attributes.

```csharp
public class TextRenderer
{
    public virtual void DrawText(SKCanvas canvas, TextModel model, List<Glyph> glyphs, SKRect localBounds, SKPaint paint);
}
```

### RenderProcessor

Wraps a `TextRenderer` to modify rendering behavior:

```csharp
text.Renderer = new RenderProcessor(text, new TextRenderer(text));
```

## FTextInputField

Full text input field with caret, selection, clipboard, keyboard navigation, and validation:

```csharp
FTextInputField field = new(
    new FText(TextModelFactory.CreateBasic(""))
);
field.PlaceholderText = "Type here...";
field.SetParent(panel);
```

### Input Modes

```csharp
field.TextInputMode = FTextInputField.TextInputFieldMode.Password;
```

### Validation

```csharp
field.InputValidation = FTextInputField.InputValidationMode.Numeric;
// Any, Alphabetic, Numeric, Alphanumeric
```

## FTypeface

Font management with multi-variant support. Can represent either a system font (by family name) or a streamed font from embedded resources.

```csharp
// System font
FTypeface segoe = new("Segoe UI Variable");

// Streamed font (for custom fonts)
FStreamedTypeface custom = new();
custom.AddVariant(resourceStream, (Weight.Bold, Slant.Upright, Width.Normal));
```

`FTypeface.CreateSKTypeface(character, weight, slant)` resolves the appropriate font, considering script-specific fallbacks for CJK or other character ranges.
