# Sliders and Scrollers

Numeric input components for values, progress, and scrolling.

## FSlider

Horizontal slider with a draggable knob, bar fill, and snapping hotspots.

```csharp
FSlider slider = new(width: 200);
slider.SnappingInterval = 0.25f; // snap to quarters
slider.OnValueChanged = (val) => {
    // val is 0.0 to 1.0
};
slider.SetParent(panel);
```

### Properties

| Property | Description |
|----------|-------------|
| `SnappingInterval` | Snap to intervals (0.25 = quarters) |
| `KnobSize` | Size of the knob as `Vector2` |
| `BarHeight` | Height of the slider bar |
| `BarColor` / `KnobColor` | Custom colors |
| `MinValue` / `MaxValue` | Minimum / Maximum Value |
| `ExtraHotspots` | A list of values that will add an indicator at that position |

Keyboard arrow keys are supported for fine adjustment.

## FNumericScroller

Click-to-open popup with increment / decrement buttons and scroll wheel support.

```csharp
FNumericScroller scroller = new(
    label: new FText(TextModelFactory.CreateBasic(""))
);
scroller.Suffix.SetStaticState("px");
scroller.MaxValue.SetStaticState(100);
scroller.Value = 10;
scroller.SetParent(panel);
```

### Properties

| Property | Description |
|----------|-------------|
| `Value` | Current numeric value |
| `MaxValue` | Maximum value (as state) |
| `MinValue` | Minimum value (as state) |
| `FormatProvider` | Number format provider |
| `Culture` | The used culture for the number. |

## FProgressBar

Linear progress bar with determinate and indeterminate modes.

```csharp
FProgressBar bar = new();
bar.Value.SetStaticState(0.6f); // 60%
bar.SetParent(panel);
```

Indeterminate mode shows animated stripes:

```csharp
bar.IsIndeterminate.SetStaticState(true);
```

### Properties

| Property | Description |
|----------|-------------|
| `Value` | Current progress value (as state) |
| `MaxValue` | Maximum value (as state) |
| `MinValue` | Minimum value (as state) |
| `Indeterminate` | If the progress bar is indeterminate |
| `IndeterminateSpeed` | The speed of the indeterminate stripes |
| `IndeterminateLinesRepeat` | The amount of stripes |
| `LeftToRight` | If the progress goes from left to right or the other way around |

## FRadialProgressBar

Circular progress bar with arc rendering and sweep gradient overlay.

```csharp
FRadialProgressBar radial = new();
radial.Progress.SetStaticState(0.75f);
radial.SetParent(panel);
```

### Properties

| Property | Description |
|----------|-------------|
| `Thickness` | The thickness of the progress bar circle |
| `IndeterminateArc` | The arc of the indeterminate stripes |