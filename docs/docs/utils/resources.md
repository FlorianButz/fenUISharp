# Resources & Resource Management

`Resources` is the central registry for embedded assets in FenUI. It manages typefaces, themes, images, and FAV files through in-memory dictionaries. Assets are registered with string keys and retrieved by those keys.

## Initialization

`Resources.LoadDefault()` is called automatically by `FenUI.Init()`. It registers:

- **Typefaces**: "Segoe UI Variable" (system font), "inter-variable" (Inter font with Normal, Bold, Italic, BoldItalic variants)
- **Images**: Built-in images (default, logo, checkmark, clipboard, arrows)
- **Themes**: `default-dark` and `default-light`

## Typefaces

```csharp
// Register a system font
Resources.RegisterTypeface("Segoe UI Variable", "my-font");

// Register a streamed typeface with font variants (from embedded resource)
Resources.RegisterTypeface(resourceStream,
    (SKFontStyleWeight.Bold, SKFontStyleSlant.Upright, SKFontStyleWidth.Normal),
    "my-font");

// Retrieve
FTypeface typeface = Resources.GetTypeface("my-font");
// Falls back to the default typeface if not found
```

Typefaces support multiple font variants (weight, slant, width) via `FStreamedTypeface`. When a specific style is requested, the closest matching variant is selected.

## Themes

```csharp
// Register
Theme myTheme = new() { Primary = SKColors.Blue, ... };
Resources.RegisterTheme(myTheme, "my-theme");

// Retrieve (returns empty Theme if not found)
Theme theme = Resources.GetTheme("my-theme");
```

## Images

```csharp
// Load from file path
SKImage img = Resources.LoadImage(@"C:\path\to\image.png");

// Register
Resources.RegisterImage(img, "my-image");

// Retrieve (returns the "default" image if not found)
SKImage image = Resources.GetImage("my-image");
```

## FAV (Animated Vectors)

```csharp
// Load from file path
AnimatedVector fav = Resources.LoadFav(@"C:\path\to\icon.fav");

// Register
Resources.RegisterFav(fav, "my-icon");

// Retrieve (throws if not found)
AnimatedVector vector = Resources.GetFAV("my-icon");
```

## ContentExtractor

For extracting embedded resources to disk (e.g., for crash handler binaries or icon files):

```csharp
// Extract to a specific file path
string path = ContentExtractor.ExtractToFile(
    "resource-name-in-assembly",
    @"C:\dest\file.exe",
    overwrite: false,
    expectedSha256: "abc123..."
);

// Extract to temp directory
string tempPath = ContentExtractor.ExtractToTemp(
    "resource-name",
    fileName: "output.exe"
);

// Extract to memory (byte array)
byte[] data = ContentExtractor.ExtractToMemory("resource-name");

// List all available resources in an assembly
string[] allResources = ContentExtractor.ListAll();
```

The `ExtractToFile` method supports SHA-256 verification. Resource names can be short (resolved by suffix matching) or fully qualified. If the name is ambiguous, an exception lists the candidates.

## How Resource Registration Works

All resources are stored in static `Dictionary<string, T>` instances inside the `Resources` class:

- `typefaces`: `Dictionary<string, FTypeface>`
- `themes`: `Dictionary<string, Theme>`
- `images`: `Dictionary<string, SKImage>`
- `favs`: `Dictionary<string, AnimatedVector>`

Registration is additive. Registering with an existing key **replaces** the previous value (for images and FAVs) or returns the existing entry (for typefaces).

## Using Embedded Resources in Your Project

FenUI embeds its default resources via .NET embedded resources in the FenUI assembly. You can embed your own resources by adding them to your project with `EmbeddedResource` build action and loading them via `Assembly.GetManifestResourceStream()`.

```csharp
// Example: loading a custom resource
var asm = typeof(Program).Assembly;
using var stream = asm.GetManifestResourceStream("MyApp.Resources.custom.fav");
var fav = AnimatedVectorParser.ParseFAV(new StreamReader(stream).ReadToEnd());
Resources.RegisterFav(fav, "custom-fav");
```
