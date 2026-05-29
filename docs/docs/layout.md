# Layout

FenUISharp uses a manual layout system. There is no automatic flow layout engine. You set sizes, positions, and stretching explicitly, and call `RecursiveUpdateLayout()` when the layout needs to change.

> **NOTE**
> The layout system will most likely be reworked in the future to comply with modern and intuitive layouting standards.

## UIObject Layout Properties

Every `UIObject` has a `Layout` property with the following `State<T>` properties:

| Property | Type | Description |
|----------|------|-------------|
| `StretchHorizontal` | `State<bool>` | Stretch to fill parent width |
| `StretchVertical` | `State<bool>` | Stretch to fill parent height |
| `MarginHorizontal` | `State<float>` | Horizontal margin (same on left and right) |
| `MarginVertical` | `State<float>` | Vertical margin (same on top and bottom) |
| `AbsoluteMarginHorizontal` | `State<Vector2>` | Per-side horizontal margin (x: left, y: right) |
| `AbsoluteMarginVertical` | `State<Vector2>` | Per-side vertical margin (x: top, y: bottom) |
| `Alignment` | `State<Vector2>` | Normalized alignment within parent (0.5 = center) |
| `AlignmentAnchor` | `State<Vector2>` | Anchor point for alignment |
| `MinWidth` / `MinHeight` | `State<float>` | Minimum size constraints |
| `MaxWidth` / `MaxHeight` | `State<float>` | Maximum size constraints |

### How Layout is Computed

The layout system operates in two stages during the transform update:

1. **ApplyLayoutToSize**: Computes the effective size based on stretch and margin settings relative to the parent's bounds. Stretch makes the size fill the parent (minus margins), otherwise the explicit size is used. The result is clamped by Min/Max constraints.

2. **ApplyLayoutToPositioning**: Computes an offset and anchor correction vector based on alignment and alignment anchor, which are added to the local position.

The final position is: `Position = LocalPosition + LayoutOffset - AnchorCorrection`

### Custom Positioning

The `ProcessLayoutPositioning` delegate allows custom layout logic:

```csharp
panel.Layout.ProcessLayoutPositioning = (originalPosition) => {
    return originalPosition + new Vector2(10, 0); // offset all children
};
```

This is used internally by `StackContentComponent` to stack children.

## Setting Up a Panel

```csharp
FPanel panel = new();
panel.Layout.StretchHorizontal.SetStaticState(true);
panel.Layout.StretchVertical.SetStaticState(true);
panel.Layout.MarginVertical.SetStaticState(25);
```

## StackContentComponent

For automatic stacking of children (vertical or horizontal), use `StackContentComponent`. It positions children sequentially and supports scrolling, overflow behavior, and clip effects. See [Stack Content](docs/components/stack-content.md).

## Manual Layout Updates

When you change layout properties or add children, you must propagate the changes:

```csharp
panel.RecursiveUpdateLayout();
// or
layout.FullUpdateLayout();
```

This invalidates the layout of the panel and all its children, causing transform and shape recalculation on the next frame. Some operations require dispatching this to the next frame to ensure parent bounds are up to date:

```csharp
Dispatcher.InvokeLater(() => panel.RecursiveUpdateLayout());
// or
Dispatcher.InvokeLater(() => layout.FullUpdateLayout());
```

## Transform

Each `UIObject` has a `Transform` for position, size, scale, rotation, and anchor:

```csharp
panel.Transform.Position.SetStaticState(new Vector2(100, 50));
panel.Transform.Size.SetStaticState(new Vector2(400, 300));
panel.Transform.Rotation.SetStaticState(15f); // degrees
```

Positions and sizes use a global coordinate system. The `Anchor` property controls the origin point:

```csharp
panel.Transform.Anchor.SetStaticState(new Vector2(0.5f, 0.5f)); // center (default)
panel.Transform.Anchor.SetStaticState(new Vector2(0f, 0f));     // top-left
panel.Transform.Anchor.SetStaticState(new Vector2(1f, 1f));     // bottom-right
```

The pivot point for rotation and scale is calculated as: `Pivot = VisibleSize * Anchor`.

### Size vs VisibleSize

`Transform.Size` is the raw size value. `Transform.VisibleSize` is the size after layout clamping (Min/Max constraints) and stretch adjustments. Always reference `VisibleSize` when you need the actual rendered size.

## RecursiveUpdateLayout and Invalidate

- `RecursiveUpdateLayout()` marks the layout as dirty for the object and all children, triggering a full recalc on the next frame.
- `Invalidate(Invalidation.LayoutDirty)` marks only this object, parents are also notified via `ChildDirty`.
