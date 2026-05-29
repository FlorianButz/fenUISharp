# Images and FAV Display

Display images and animated vector graphics.

## FImage

Displays an image with configurable scale mode.

```csharp
FImage img = new(
    () => Resources.GetImage("my-image"),
    size: () => new Vector2(100, 100)
);
img.ScaleMode.SetStaticState(FImage.ImageScaleMode.Contain);
img.SetParent(panel);
```

### Scale Modes

| Mode | Description |
|------|-------------|
| `Stretch` | Fills the entire bounds, may distort |
| `Fit` | Fits inside bounds, preserves aspect ratio |
| `Contain` | Same as Fit, centers the image |

### Properties

| Property | Description |
|----------|-------------|
| `TintColor` | Tint color applied to the image |
| `ImageScale` | Scale multiplier |
| `CornerRadius` | Corner radius for clipping |

## FAVDisplay

Displays an animated vector (FAV file) with playback control.

```csharp
FAVDisplay fav = new(() => Resources.GetFAV("my-icon"));
fav.SetParent(panel);

// Play an animation
fav.PlayAnimation("action");
```

### Methods

| Method | Description |
|--------|-------------|
| `PlayAnimation(string id)` | Play animation by ID |
| `StopAnimation()` | Stop current animation |

### FAV Animation Naming Conventions

- `in` - Played automatically when icon is shown.
- `out` - Played automatically before icon is hidden.
- `action` - Played on user interaction.

See [FAVs](docs/favs/favs.md) for the full FAV file format specification.
