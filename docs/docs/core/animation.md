# Animation

FenUISharp provides two primary methods for animation: the **AnimatorComponent** (a behavior component for tweening float values) and **State-based animation** (changing State values each frame).

## AnimatorComponent

A `BehaviorComponent` that drives a float value from 0 to 1 (or 1 to 0) over a duration with easing.

```csharp
AnimatorComponent animator = new(owner, Easing.EaseOutCubic);
animator.Duration = 0.5f;
animator.OnValueUpdate += (t) => {
    // t is the eased value (0 to 1)
    panel.Transform.Scale.SetStaticState(Vector2.One * (1f + t * 0.1f));
};
animator.OnComplete += () => { /* animation finished */ };
animator.Start();
```

### Constructors

```csharp
// Single easing function (used for both directions)
AnimatorComponent(owner, easing);

// Separate easing for forward and inverse
AnimatorComponent(owner, easing, inverseEasing);
```

### Properties

| Property | Description |
|----------|-------------|
| `Duration` | Length of the animation in seconds |
| `Time` | Current eased progress (0 to 1) |
| `UneasedTime` | Current linear progress (0 to 1) |
| `TimePassed` | Raw time elapsed |
| `Inverse` | Whether to animate from 1 to 0 instead of 0 to 1 |
| `IsRunning` | Whether the animation is currently active |

### Methods

| Method | Description |
|--------|-------------|
| `Start()` | Start from the current value |
| `Restart()` | Reset to beginning and start |
| `Break()` | Stop immediately, reset time |
| `CompleteEarly()` | Stop and fire OnComplete |

### Auto-Update

The AnimatorComponent listens to `BehaviorEventType.BeforeUpdate` and advances itself each frame. It is automatically disposed with its owner.

## State Processors for Animation

Since all UIObject properties are `State<T>`, you can drive animations by simply setting a responsive state to a function that returns an animated value:

```csharp
// Using AnimatorComponent to animate opacity
AnimatorComponent anim = new(panel, Easing.EaseOutCubic);

anim.OnValueUpdate += (t) => {
    panel.ImageEffects.Opacity.SetStaticState(1f - t);
};

// Or using time directly
panel.ImageEffects.Opacity.SetResponsiveState(() => {
    float t = (float)(Math.Sin(FContext.Time * 2) * 0.5 + 0.5);
    return t;
});
```

### Using Resolvers for Layered Animation

Because states use priority-based resolution, you can layer animations:

```csharp
// Base state
panel.Transform.Rotation.SetStaticState(0f); // priority 1

// Animation at higher priority
AnimatorComponent anim = new(panel, Easing.EaseOutBack);
anim.OnValueUpdate += (t) => {
    panel.Transform.Rotation.SetResponsiveState(() => t * 360f, 10);
};
// When animation is done, dissolve the priority to let the base state take over
anim.OnComplete += () => {
    panel.Transform.Rotation.DissolvePriority(10);
};
```

## Spring Physics

The `Spring` class provides physics-based spring animation for smooth, natural motion:

```csharp
Spring spring = new(initialValue: new Vector2(0, 0), speed: 2f, springiness: 1f / 0.85f, damping: 0.1f);

// In update loop:
Vector2 result = spring.Update(deltaTime, targetValue);
```

Springs are used internally by `StackContentComponent` for smooth scroll physics.

## Easing Functions

The `Easing` utility class provides standard easing functions:

```csharp
Easing.EaseInCubic(t);
Easing.EaseOutCubic(t);
Easing.EaseInQuint(t);
Easing.EaseOutQuint(t);
Easing.EaseInBack(t);
Easing.EaseOutBack(t);
Easing.EaseInSin(t);
Easing.EaseOutSin(t);
```

### Combined Easing

```csharp
Func<float, float> custom = Easing.CombineInOut(
    Easing.EaseInCubic,  // Used for first half
    Easing.EaseOutCubic  // Used for second half
);
```

## ImageEffects Animation

`ImageEffects` provides properties ideal for animation:

```csharp
// Fade in
anim.OnValueUpdate += (t) => {
    panel.ImageEffects.Opacity.SetStaticState(t);
};

// Blur in/out
anim.OnValueUpdate += (t) => {
    panel.ImageEffects.BlurRadius.SetStaticState((1f - t) * 10f);
};

// Desaturate
anim.OnValueUpdate += (t) => {
    panel.ImageEffects.Saturation.SetStaticState(1f - t);
};
```

## Advanced: Custom Animation Loop

For complex animations, use the `Update()` lifecycle method or the window's pre-update callback:

```csharp
// Using UIObject.Update
protected override void Update()
{
    float t = (MathF.Sin(FContext.Time * 3f) + 1f) / 2f;
    Transform.Rotation.SetStaticState(t * 360f);
}

// Using window callback
window.Callbacks.OnPreUpdate += () => {
    myObject.Transform.Position.SetResponsiveState(
        () => new Vector2(MathF.Sin(FContext.Time) * 100f, 0)
    );
};
```
