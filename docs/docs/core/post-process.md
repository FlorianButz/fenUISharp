# Post Process Chain

The post process chain is a per-UIObject pipeline of effects that run after the object is rendered to its cached surface but before it is composited to the screen. This allows effects like bloom and gradient swipes to be applied to individual elements.

## Architecture

Every `UIObject` has a `PostProcessChain` property that holds a list of `IPostProcessEffect` instances.

```
Object renders to cached surface
         |
    PostProcessChain runs
    - OnBeforeRender (prep)
    - OnAfterRender  (apply effect)
    - OnLateAfterRender (cleanup)
         |
    Surface composited to screen
```

## IPostProcessEffect

```csharp
public interface IPostProcessEffect
{
    void OnBeforeRender(PPInfo info);
    void OnAfterRender(PPInfo info);
    void OnLateAfterRender(PPInfo info);
}
```

### PPInfo

A struct passed to each effect method containing rendering context:

```csharp
public struct PPInfo
{
    public SKSurface source;        // The source (rendered) surface
    public SKImageInfo sourceInfo;  // Info about the source surface
    public SKSurface target;        // The target surface (can be same as source for in-place)
    public UIObject owner;          // The UIObject this effect belongs to
}
```

## Attaching Effects

```csharp
FBloomEffect bloom = new();
panel.PostProcessChain.Attach(bloom);

// To remove
panel.PostProcessChain.Detatch(bloom);
```

Multiple effects can be attached to the same object. They run in the order they were attached.

## Built-in Effects

### FBloomEffect

A bloom (glow) effect implemented as an SKSL shader. It extracts bright areas from the rendered content, downsamples them, applies blur, and composites them back with additive blending.

```csharp
FBloomEffect bloom = new()
{
    BloomIntensity = 0.8f,    // Glow strength (0-10)
    BloomThreshold = 0.75f,   // Minimum brightness for bloom (0-1)
    BloomSpread = 10f,        // Blur radius of the glow (0-35)
    Downsampling = 2          // Downsampling factor for performance (1-12)
};

panel.PostProcessChain.Attach(bloom);
```

### FGradientSwipeEffect

Applies a gradient overlay that sweeps across the surface. Useful for reveal animations, transitions, and loading effects.

```csharp
FGradientSwipeEffect swipe = new(panel)
{
    GradientWidth = 1f,              // Width of the gradient band
    GradientPosition = ...           // State<float> position of the gradient (-1 to 1)
    GradientRotation = 45f,          // Rotation of the gradient in degrees
    RotationDegrees = 0f,            // Additional rotation for the effect
    RepeatGradient = false,          // Whether the gradient repeats
    MapGradientPosition = true,      // Wrap position to [-1, 1] range
    ModuloRangeMultiplicator = 1f,   // Multiplier for position wrapping
};

// Colors
swipe.Primary = new(() => SKColors.White, panel, ...);
swipe.Secondary = new(() => SKColors.Transparent, panel, ...);

panel.PostProcessChain.Attach(swipe);

// Animate the gradient
float pos = -1f;
window.Callbacks.OnPreUpdate += () =>
{
    pos += (float)FContext.DeltaTime * 0.5f;
    if (pos > 3f) pos = -1f;
    swipe.GradientPosition.SetStaticState(pos);
};
```

The gradient is rendered using `SKShaper.CreateLinearGradient` with `SKBlendMode.SrcIn` so it only affects opaque areas of the source content.

## Custom Effects

Implement `IPostProcessEffect` to create custom effects:

```csharp
public class MyEffect : IPostProcessEffect
{
    public void OnBeforeRender(PPInfo info)
    {
        // Called before the object renders
        // Modify info.source here if needed
    }

    public void OnAfterRender(PPInfo info)
    {
        // Called after the object renders
        // info.source has the rendered content
        // info.target is where the final output goes
    }

    public void OnLateAfterRender(PPInfo info)
    {
        // Called last, for cleanup or additional passes
    }
}
```
