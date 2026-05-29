# Theme

A `Theme` holds all color fields that define a color scheme. Create a new instance and set all values, as they default to black.

```csharp
Theme darkTheme = new() {
    Primary = SKColor.Parse("#6d9eeb"),
    PrimaryVariant = SKColor.Parse("#3b6cb5"),
    Secondary = SKColor.Parse("#4ade80"),
    Background = SKColor.Parse("#1f1f1f"),
    Surface = SKColor.Parse("#272727"),
    OnPrimary = SKColors.White,
    OnBackground = SKColors.White,
    Shadow = SKColors.Black,
    // ... set all fields
};
```

## Fields

| Field | Description |
|-------|-------------|
| `Primary` | Main accent color |
| `PrimaryVariant` | Darker/lighter accent variant |
| `Secondary` | Secondary accent |
| `SecondaryVariant` | Secondary variant |
| `Background` | Window background |
| `Surface` | Panel/card surface |
| `SurfaceVariant` | Alternate surface |
| `OnPrimary` | Text on primary |
| `OnSecondary` | Text on secondary |
| `OnBackground` | Text on background |
| `OnSurface` | Text on surface |
| `Shadow` | Drop shadow color |
| `DisabledMix` | Color mixed in when disabled |
| `HoveredMix` | Color mixed in on hover |
| `PressedMix` | Color mixed in on press |
| `SelectedMix` | Color mixed in when selected |
| `Error` | Error state color |
| `Success` | Success state color |
| `Warning` | Warning state color |

## Material Factories

Each theme also provides factory functions for materials:

| Factory | Returns |
|---------|---------|
| `DefaultMaterial()` | Empty default material |
| `PanelMaterial()` | DefaultPanelMaterial |
| `InteractableMaterial()` | InteractableDefaultMaterial |
| `TransparentInteractableMaterial()` | Transparent interactable |
