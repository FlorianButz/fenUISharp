# Materials Overview

Materials control how UIObjects are rendered. Every `UIObject` has a `RenderMaterial` state that determines its visual appearance.

## Built-In Materials

### DefaultPanelMaterial

Standard panel rendering with base fill, border, and drop shadow.

```csharp
panel.RenderMaterial.SetStaticState(new DefaultPanelMaterial() {
    BaseColor = () => SKColors.Black, // Can also use theme colors
    BorderSize = 1f
});
```

| Property | Description |
|----------|-------------|
| `BaseColor` | Fill color |
| `BorderColor` | Outer border color |
| `LightBorderColor` | Inner highlight border |
| `BorderSize` | Border thickness |
| `ShadowSize` | Drop shadow size |

### InteractableDefaultMaterial

Button/interactive material with base fill, top highlight gradient, border, inner shadow, and drop shadow.

```csharp
button.RenderMaterial.SetStaticState(new InteractableDefaultMaterial() {
    BaseColor = () => SKColors.Blue, // Can also use theme colors
});
```

| Property | Description |
|----------|-------------|
| `BaseColor` | Fill color |
| `BorderColor` | Outer border color |
| `ShadowColor` | Shadow color |
| `HighlightColor` | Highlight color |
| `DropShadowRadius` | Shadow radius |

### GlassMaterial

Apples liquid-glass style material with displacement + blur

```csharp
panel.RenderMaterial.SetStaticState(
    new GlassMaterial(() => panel.Composition.GrabBehindPlusBuffer(panel.Shape.GlobalBounds, quality: 1f)) 
{
    BlurRadius = () => 10f,
    Distance = () => 8,
    Brightness = () => 1.1f
});
```

| Property | Description |
|----------|-------------|
| `BaseColor` | Fill color |
| `Highlight` | Highlight color |
| `GrabPassFunction` | The function that gives the backdrop |
| `BlurRadius` | Gaussian Blur sigma |
| `Displacement` | The displacement strength of the glass |
| `Distance` | The distance of the displacement |
| `Brightness` | Brightness filter |

### BlurMaterial

Simple Gaussian blur of content behind the element.

```csharp
panel.RenderMaterial.SetStaticState(
    new BlurMaterial(() => panel.Composition.GrabBehindPlusBuffer(panel.Shape.GlobalBounds, quality: 1f)) 
{
    BlurRadius = () => 24f,
});
```

| Property | Description |
|----------|-------------|
| `BaseColor` | Fill color |
| `GrabPassFunction` | The function that gives the backdrop |
| `BlurRadius` | Gaussian Blur sigma |

> **Tip**
> This can be combined with the panel material to have a nicer macOS-style look

### EmptyDefaultMaterial

Minimal / single color material.

```csharp
panel.RenderMaterial.SetStaticState(
    new EmptyDefaultMaterial() { BaseColor = SKColors.White });
```

### MaterialCompose

Composes two materials in sequence (bottom + top).

```csharp
panel.RenderMaterial.SetStaticState(new MaterialCompose(
    bottomMaterial: () => new DefaultPanelMaterial(),
    topMaterial: () => new GlassMaterial(...)
));
```

## Per-Instance Color Override

Use `WithOverride` to substitute properties without subclassing:

```csharp
renderMaterial.CachedValue.WithOverride(new() {
    ["BaseColor"] = () => SKColors.Red,
    ["BorderColor"] = () => SKColors.Transparent
}).DrawWithMaterial(canvas, path, this, paint);
```

## Material and Themes

Materials automatically react to theme changes. When `ThemeManager.SetTheme()` is called, all materials using `ThemeColor` instances update their colors.
