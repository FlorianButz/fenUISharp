# Stack Content

`StackContentComponent` arranges children in a horizontal or vertical stack with optional scrolling, overflow fade, and blur.

It extends `LayoutComponent`, which is a `BehaviorComponent` specializing in layout management.

## Basic Usage

```csharp
FPanel panel = new();
panel.Layout.StretchHorizontal.SetStaticState(true);
panel.Layout.StretchVertical.SetStaticState(true);

StackContentComponent layout = new(
    panel,
    StackContentComponent.ContentStackType.Vertical,
    StackContentComponent.ContentStackBehavior.Scroll
);
```

## Stack Types

| Type | Description |
|------|-------------|
| `Vertical` | Stack children top to bottom |
| `Horizontal` | Stack children left to right |

## Stack Behaviors

| Behavior | Description |
|----------|-------------|
| `Scroll` | Scrollable when content overflows (adds a scrollbar) |
| `Overflow` | No scrolling, content clips |
| `SizeToFit` | Sizes to content on the stack axis, keeps explicit size on the other |
| `SizeToFitAll` | Sizes to content on both axes |

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `Gap` | `State<float>` | Distance between each element |
| `Pad` | `State<float>` | Padding of the content inside |
| `StartAlignment` | `Vector2` | Starting alignment of content items |
| `AlignInside` | `Vector2` | Alignment of content when smaller than container |
| `ContentFade` | `bool` | Fade out content at overflow edges |
| `FadeLength` | `float` | Length of the fade-out region in pixels |
| `ContentBlur` | `bool` | Progressively blur content at overflow edges |
| `BlurSigma` | `float` | Maximum Gaussian blur sigma at edge |
| `BlurLength` | `float` | Length of the blur region in pixels |
| `BlurQuality` | `float` | Quality of the blur (0-1, lower = faster) |
| `ContentClip` | `bool` | Clip overflowing content to bounds |
| `ScrollSpeed` | `float` | Mouse wheel scroll speed multiplier |
| `ScrollSpring` | `Spring` | Optional spring physics for smooth scrolling |
| `SnappingProvider` | `Func<float, float>` | Optional snapping function |

## Adding Children

Children are added with `SetParent()`. They are stacked in order of creation:

```csharp
FText item1 = new(TextModelFactory.CreateBasic("First"));
item1.SetParent(panel);

FText item2 = new(TextModelFactory.CreateBasic("Second"));
item2.SetParent(panel);

layout.FullUpdateLayout();
```

### LayoutObject

Children can opt out of the stack layout by adding a `LayoutObject` behavior component with `IgnoreParentLayout` set to true:

```csharp
new LayoutObject(myChild).IgnoreParentLayout.SetStaticState(true);
```

## Scrollbar

When using `Scroll` behavior, an `FScrollBar` is automatically created and positioned at the edge of the container (right for vertical, bottom for horizontal). It auto-fades and auto-hides when content fits within the page.

## Overflow Effects

When `ContentFade` or `ContentBlur` are enabled, the component applies Skia shader-based masks to create fade / blur transitions at the scroll boundaries. These effects are implemented as linear gradient masks and progressive multi-step blur filters:

- **Fade**: Transparent-to-opacity gradient at both edges
- **Blur**: Multi-layer blur filter with increasing sigma toward the edges, composited with a sharp center region

Both effects can be combined simultaneously

## ContentClipBehaviorProvider

An abstract class that allows custom per-child clip behaviors based on scroll position. The provider is called every frame during the stack's layout process and can modify each child's transform (position, scale, opacity, etc.) based on its proximity to the clip boundary.

```csharp
public abstract class ContentClipBehaviorProvider
{
    public float ClipStart { get; set; }                // Start of clip region
    public float ClipLength { get; set; }               // Length of clip region
    public Func<float, float> ClipEase { get; set; }    // Easing for clip transition
    public bool Inverse { get; set; }                   // Invert clip direction

    public virtual void Update(StackContentComponent layout, List<UIObject> children);
    public abstract void ClipBehavior(float t, StackContentComponent layout, UIObject child, int childIndex, bool isBottom);
}
```

### Built-in Providers

#### ScaleContentClipBehavior
Scales children as they approach the clip boundary. Items near the edge shrink.

```csharp
ScaleContentClipBehavior scaleClip = new(layout)
{
    DefaultScale = new Vector2(1, 1),  // Normal scale
    ClipScale = new Vector2(0, 0)      // Scale when fully clipped
};
layout.ContentClipBehaviorProvider = scaleClip;
```

#### StackContentClipBehavior
Creates a stacking/depth effect. Children near the edge slide toward the boundary, scale down, and separate from each other.

```csharp
StackContentClipBehavior stackClip = new(layout)
{
    DistanceFromEdge = 25f,             // Distance to start sliding
    Scale = new Vector2(0.85f, 0.85f),  // Scale when fully clipped
    SeparationDistance = 25f,           // Additional separation between items
    SlideStart = 65f                    // Offset for slide start
};
layout.ContentClipBehaviorProvider = stackClip;
```

#### RandomContentClipBehavior
Children fly off in random directions when clipped.

```csharp
RandomContentClipBehavior randomClip = new(layout)
{
    Spread = 500,             // Random spread distance
    PerpendicularSpread = 100 // Random spread in perpendicular axis
};
layout.ContentClipBehaviorProvider = randomClip;
```

### Custom Provider

```csharp
public class MyClipBehavior : ContentClipBehaviorProvider
{
    public MyClipBehavior(StackContentComponent layout) : base(layout) { }

    public override void ClipBehavior(float t, StackContentComponent layout, UIObject child, int childIndex, bool isBottom)
    {
        // t is 0 at the clip boundary, 1 when fully clipped
        // Modify child.Transform state based on t
        child.ImageEffects.Opacity.SetStaticState(1f - t);
    }
}
```

The `GetClipFactors` helper method provides per-child start/end clip factors that range from 0 (not clipped) to values beyond 1 (fully clipped), based on each child's position relative to the clip region:

```csharp
layout.GetClipFactors(childIndex, clipStart, clipLength, out float startFactor, out float endFactor);
```
