# UIObject

`UIObject` is the abstract base class for every visual element in FenUI. All components (FPanel, FText, FSlider, etc.) inherit from it.

## Core Sub-Systems

Every UIObject owns a set of sub-systems that control its appearance, behavior, and layout:

| Property | Type | Description |
|----------|------|-------------|
| `Transform` | `Transform` | Position, size, scale, rotation, anchor |
| `Layout` | `Layout` | Stretch, margin, alignment, min/max size |
| `Shape` | `Shape` | Local and global bounding rectangles |
| `Composition` | `Compositor` | Z-index, z-order traversal |
| `InteractiveSurface` | `InteractiveSurface` | Mouse hit-testing, hover, drag, scroll |
| `ImageEffects` | `ImageEffects` | Opacity, blur, saturation, brightness |
| `PostProcessChain` | `PostProcessChain` | Bloom, gradient swipe, custom effects |
| `RenderMaterial` | `State<Material>` | The material used for rendering |
| `ObjectSurface` | `CachedSurface` | Cached offscreen render surface |

## Transform

Controls the object's spatial properties. All values are `State<T>` so they can be animated.

```csharp
panel.Transform.LocalPosition // local position relative to parent
panel.Transform.Size          // size of the object
panel.Transform.Scale         // scale multiplier (default (1, 1))
panel.Transform.Rotation      // rotation in degrees
panel.Transform.Anchor        // origin point ((0.5, 0.5) = center)
```

The transform builds an `SKMatrix` (DrawMatrix) that combines translation, rotation, and scale around the pivot point. A `RecursiveDrawMatrix` is also computed by concatenating all parent transforms.

### Coordinate Conversion

```csharp
Vector2 global = transform.DrawLocalToGlobal(localPoint);
SKRect global = transform.DrawLocalToGlobal(localRect);
Vector2 local = transform.GlobalToDrawLocal(globalPoint);
```

> **NOTE**
> 'DrawLocal' and 'Local' are not the same and have to be used differently depending on the context.

### TransformMatrixProcessor

An abstract class that can intercept and modify the transform matrix. Useful for custom 3D-like effects or complex transformations:

```csharp
public class MyMatrixProcessor : TransformMatrixProcessor
{
    protected override SKMatrix Process(SKMatrix matrix) { ... }
}
transform.MatrixProcessor = new MyMatrixProcessor();
```

## Layout

Controls how the object is positioned within its parent's bounds using an explicit manual layout system.

| State | Description |
|-------|-------------|
| `StretchHorizontal` / `StretchVertical` | Fill parent width/height |
| `MarginHorizontal` / `MarginVertical` | Relative margin (same on both sides) |
| `AbsoluteMarginHorizontal` / `AbsoluteMarginVertical` | Per-side absolute margin (left/right or top/bottom) |
| `Alignment` | Normalized alignment within parent (0-1) |
| `AlignmentAnchor` | Anchor point for alignment calculations |
| `MinWidth` / `MinHeight` / `MaxWidth` / `MaxHeight` | Size clamping |

Layout calculates a size offset and anchor correction that feeds into the final position. When any layout state changes, the object is marked `LayoutDirty` and the transform is recalculated on the next frame.

## Shape

Represents the bounding rectangles of the object:

```csharp
Shape.LocalBounds      // Bounds in local coordinates (0, 0, width, height)
Shape.GlobalBounds     // Bounds transformed through all parent matrices
Shape.SurfaceDrawRect  // Local bounds plus padding (for blur overflow)
```

`Shape.UpdateShape()` is called automatically when transform or layout changes.

## Compositor

Manages z-order and tree traversal:

```csharp
composition.LocalZIndex  // Z-index as a State<int>
composition.CreationIndex // Auto-incrementing creation order
```

Children are sorted by z-index first, then by creation order. The compositor also provides utility methods:

```csharp
compositor.TestIfTopMost()  // Whether this object is top-most among enabled/visible objects
compositor.GrabBehindPlusBuffer(globalBounds, quality)  // Captures the rendered content behind this object
```

## InteractiveSurface

The input handling system for mouse interactions. Every UIObject has one, but it is disabled by default. Must be explicitly enabled:

```csharp
surface.EnableMouseActions = ...  // State<bool> - enable click/hover
surface.EnableMouseScrolling = ... // State<bool> - enable scroll wheel
```

### Mouse Events

```csharp
surface.OnMouseEnter += () => { };
surface.OnMouseStay += () => { };
surface.OnMouseExit += () => { };
surface.OnMouseMove += (pos) => { };
surface.OnMouseAction += (code) => { };
surface.OnDoubleMouseAction += (button) => { };
surface.OnLongMouseAction += (button) => { };
surface.OnMouseScroll += (delta) => { };
```

### Drag Events

```csharp
surface.OnDragStart += () => { };
surface.OnDrag += (cumulativeDelta) => { };
surface.OnDragDelta += (perFrameDelta) => { };
surface.OnDragEnd += () => { };
```

### Properties

| Property | Description |
|----------|-------------|
| `IgnoreInteractions` | Whether this object ignores all input |
| `IgnoreChildInteractions` | Whether children ignore input when this is hovered |
| `ExtendInteractionRadius` | Extra pixels for hit-test area |
| `IsMouseHovering` | Whether the mouse is currently over this object |
| `IsMouseDown` | Whether the mouse button is held on this object |
| `DoubleMouseActionTimeFrame` | Time window for double-click detection (default 0.4s) |
| `LongMouseActionTime` | Time window for long-click detection (default 1s) |

Hit-testing is done front-to-back using the cached z-order list. Only the top-most enabled object that contains the mouse point receives events.

> **NOTE**
> Elements are separated into groups of mouse action and mouse scroll. If an element does not have EnableMouseScrolling enabled, it will be considered as passthrough when scrolling on it and the events will automatically be redirected to the topmost surface that has EnableMouseScrolling enabled. The same concept works the other way around.

## ImageEffects

A `BehaviorComponent` that applies image-level filters to the object and optionally its children.

| State | Description | Default |
|-------|-------------|---------|
| `SelfOpacity` | Opacity of this object only (not children) (uses `SmallestFloatResolver`) | 1.0 |
| `Opacity` | Opacity including children (uses `SmallestFloatResolver`) | 1.0 |
| `BlurRadius` | Gaussian blur radius (uses `BiggestFloatResolver`) | 0 |
| `Saturation` | Color saturation (0 = grayscale, 1 = normal, 4 = max) | 1.0 |
| `Brightness` | Brightness multiplier (0 = black, 1 = normal, 2 = max) | 1.0 |

These effects are applied through SkiaSharp `SaveLayer` with composed `SKImageFilter` instances. The `SelfOpacity` is applied before the object's own render, while the other effects wrap the entire render including children.

## CachedSurface

Each UIObject has a `CachedSurface` that stores the result of `Render()` in an offscreen GPU texture. This surface is only re-rendered when invalidated, avoiding excessive re-rendering of unchanging content.

```csharp
objectSurface.InvalidateSurface(dimensions, quality, padding);
objectSurface.Draw(effectChain);
objectSurface.DrawFullChainToTarget(canvas, targetRect, paint, effectChain);
```

The CachedSurface is entirely handled by the UIObject and should not be touched unless necessary.

The render quality can be adjusted per-object via the `Quality` state (0.01 to 4.0). 
When the `UseSnapshotBlit` flag is set (automatically when there is rotation/scale), the surface uses a snapshot-based blit path to avoid filtering and aliasing issues.
The surface optimization can be disabled entirely (if the side effects still occur) by flipping the `SurfaceBlitFallback` inside `UIObject` to false.

## Lifecycle

UIObjects follow a strict per-frame lifecycle:

1. **EarlyUpdate** - Runs before everything else
2. **ReverseUpdate** - Shape recalculation before main update
3. **Update** - Main logic update, state resolution, transform/layout recalculation
4. **LateUpdate** - Post-update logic
5. **Render** - Draw to cached surface (only when invalidated)
6. **DrawToSurface** - Composite cached surface to parent canvas, draw children

### Begin / LateBegin

Called once on the first frame after creation:

```csharp
protected override void Begin() { }
protected override void LateBegin() { }
```

## Invalidation System

Objects track dirty state with an `Invalidation` flags enum:

| Flag | Meaning |
|------|---------|
| `TransformDirty` | Position/size/scale/rotation changed |
| `SurfaceDirty` | Visual content needs redraw |
| `LayoutDirty` | Layout properties changed |
| `ChildDirty` | A child was added/removed/changed |

When any state changes (via `IStateListener.OnInternalStateChanged`), the object is invalidated and re-rendered on the next frame.

```csharp
obj.Invalidate(UIObject.Invalidation.SurfaceDirty);
obj.RecursiveInvalidate(UIObject.Invalidation.All); // Invalidate self and all children
```

## Parenting

Objects form a parent-child tree via `SetParent()`:

```csharp
child.SetParent(parent);
// child is now a child of parent
// Setting null defaults to the root view pane
```

Children are rendered in z-order (controlled by `Composition.LocalZIndex`). The root parent is the window's `ModelViewPane`.

## Disposal

Call `Dispose()` to clean up and remove the object from its parent and window:

```csharp
obj.Dispose(); // Disposes children recursively, removes behavior components
```
