# States

`State<T>` is the reactive, priority-aware value system that underpins the entire FenUI framework. Nearly every configurable property on a UIObject is a `State<T>`; Transform position, Layout stretch, ImageEffects opacity, and more.

States are the core of the framework. They reactively propagate changes to invalidate surfaces and trigger re-renders.

## How States Work

A `State<T>` holds a prioritized list of entries. On each frame, a resolver selects the winning entry and its value is cached. If the value changed since the last frame, all listeners are notified.

```
entries: [ {value: fn, priority: 0}, {value: fn, priority: 5}, ... ]
                  |
            resolver (default: highest priority wins)
                  |
            processor (optional transform)
                  |
            cached value ----> notify listeners
```

## Creating a State

```csharp
// With a default value and a listener
State<float> myState = new(() => 1f, owner, listener);

// With a callback action instead of listener
State<float> myState = new(() => 1f, owner, (value) => { /* on change */ });

// With manual resolve (no auto-update)
State<float> myState = new(() => 1f, owner, null, manualResolve: true);
```

A state always requires a valid `UIObject` owner and a valid FenUI window context. States auto-register with the window's pre-update callback and auto-dispose when the owner is disposed.

## Setting Values

```csharp
// Static value (always returns this value at the given priority)
state.SetStaticState(42f, priority: 10);

// Responsive/function value (evaluated every frame)
state.SetResponsiveState(() => someFunction(), priority: 5);
```

Higher priority values override lower ones if not specified otherwise. The default value has priority 0 and cannot be changed. Calling `SetStaticState` automatically sets the priority to 1.

Reactive states are especially useful for updating values without external events. Example:

```csharp
state.SetResponsiveState(() => MathF.Sin(FContext.Time * MathF.PI * 2), priority: 12);
```

This will evaluate to a sin movement without having to manually invalidating the state in the update code.
The value can be read by using `state.CachedValue`.

### The Default Value

The constructor-provided function is stored as a priority-0 entry. When `IgnoreFirstValueIfSetNotEmpty` is true (default), the default value is excluded from resolution once any other entry exists. This prevents the default from interfering with custom resolvers.

## Priority

The priority system allows multiple sources to control the same property without conflicts:

```csharp
// Animator sets priority 10
state.SetResponsiveState(() => animatedValue, 10);

// User override at priority 20 (takes precedence)
state.SetStaticState(userValue, 20);

// Remove a priority
state.DissolvePriority(10);
```

Default priority: 0. When calling `SetStaticState` or `SetResponsiveState` without explicit priority, the entry gets priority 1 (default+1).

## Resolvers

A resolver selects which entry wins. The default resolver picks the highest-priority entry:

```csharp
// Custom resolver; pick the LOWEST priority instead
state.SetResolver(entries => entries.OrderBy(x => x.Priority).First());

// Reset to default
state.SetResolver(null);
```

### Built-in Resolver Templates

```csharp
// For float: pick the biggest value
StateResolverTemplates.BiggestFloatResolver

// For int: pick the biggest value
StateResolverTemplates.BiggestIntResolver

// For float: pick the smallest value
StateResolverTemplates.SmallestFloatResolver

// For float: multiply all entries together
StateResolverTemplates.MultiplyFloatResolver

// For Vector2: multiply all entries together
StateResolverTemplates.MultiplyVectorResolver
```

These are used throughout the framework. For example, `ImageEffects.Opacity` uses `SmallestFloatResolver` (the most transparent value wins), while `ImageEffects.BlurRadius` uses `BiggestFloatResolver` (the strongest blur wins).

This is done to avoid visual conflicts when animating values and has the practical side effect of not having to deal with more abstractions on the user-end.

## Processors

A processor transforms the resolved value before it is cached:

```csharp
// Clamp between 0 and 1
state.SetProcessor(x => Math.Clamp(x, 0f, 1f));

// Always positive
state.SetProcessor(x => Math.Abs(x));
```

## Listening to Changes

```csharp
// Via IStateListener interface
state.Subscribe(myListener); // listener.OnInternalStateChanged() is called

// Via Action<T> callback
state.Subscribe((value) => Console.WriteLine($"New value: {value}"));

// Unsubscribe
state.Unsubscribe(myListener);
state.Unsubscribe(myAction);
```

## Manual Resolve

By default, states automatically resolve on every frame. Set `manualResolve = true` to disable auto-resolution and call `ReevaluateValue()` manually:

```csharp
State<float> state = new(() => 0f, owner, null, manualResolve: true);
state.ReevaluateValue(); // Manually resolve and notify
```

## CachedValue

The current resolved value is accessed via `.CachedValue` (read-only). This is the value that components read during rendering:

```csharp
float current = myState.CachedValue; // Already resolved and processed
```

## StateEntry

Each entry in a state is a `StateEntry<T>` struct:

```csharp
public struct StateEntry<T>
{
    public bool IsStatic;       // Whether the value is static (not a function)
    public Func<T> Value;       // The function that produces the value
    public uint Priority;       // Priority for resolution
}
```

## Example: Opacity with Multiple Contributors

```csharp
// Default: fully opaque
ImageEffects.Opacity.SetStaticState(1f); // priority 1

// Hover fades slightly
ImageEffects.Opacity.SetResponsiveState(() => 0.8f, 5); // priority 5

// Animation completely hides
ImageEffects.Opacity.SetResponsiveState(() => 0f, 10); // priority 10

// Uses SmallestFloatResolver, so 0.8f wins over 1f
// But the 0f in the animation wins over both
```
