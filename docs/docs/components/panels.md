# Panels

Panels are the basic building blocks for layout in FenUISharp.

## FPanel

A rounded-corner panel with optional squircle shape. Supports clipping children.

```csharp
FPanel panel = new();
panel.Layout.StretchHorizontal.SetStaticState(true);
panel.Layout.StretchVertical.SetStaticState(true);
panel.Layout.MarginVertical.SetStaticState(25);
panel.UseSquircle.SetStaticState(true); // iOS-style corners
```

### Properties

| Property | Description |
|----------|-------------|
| `UseSquircle` | Use superellipse (squircle) corners instead of round rect |
| `CornerRadius` | Corner radius for the panel shape |
| `ClipChildren` | Whether children are clipped to panel bounds |

### Materials

Panels use `DefaultPanelMaterial` by default, which renders a base fill, border, and drop shadow. You can swap the material:

```csharp
panel.RenderMaterial.SetStaticState(new DefaultPanelMaterial() {
    BaseColor = () => SKColors.DarkSlateGray,
    BorderSize = 1f
});
```

Or use an empty material for a transparent panel:

```csharp
panel.RenderMaterial.SetStaticState(new EmptyDefaultMaterial() {
    BaseColor = () => SKColors.Transparent
});
```

See [Materials](docs/materials/overview.md) for the full list.

## FBlurPane

A panel that applies real-time Gaussian blur to content behind it. Useful for frosted glass-like effects.

```csharp
FBlurPane blurPane = new();
blurPane.RenderMaterial.SetStaticState(new BlurMaterial() { ... });
```

See [Materials](docs/materials/overview.md) for glass and blur materials.
