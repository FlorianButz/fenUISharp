# Behavior Components

Behavior components extend the behavior of a `UIObject`. They are stored in a list inside every UIObject and implement `IDisposable`.

> A `BehaviorComponent` always requires a parent `UIObject`. It auto-registers with the parent on creation and auto-removes on dispose.

Behavior components intercept lifecycle events through the `HandleEvent` method. Every event type corresponds to a point in the UIObject lifecycle (update, render, layout, etc.).

## Creating a BehaviorComponent

```csharp
public class MyComponent : BehaviorComponent
{
    public MyComponent(UIComponent parent) : base(parent) { }

    public override void HandleEvent(
        BehaviorEventType type,
        out object outData,
        object? data = null) 
    {
        switch(type) {
            case BehaviorEventType.BeforeUpdate:
                // Custom update logic
                break;
            case BehaviorEventType.AfterRender:
                // Post-render logic
                break;
        }
    }

    public override void ComponentDestroy()
    {
        // Cleanup when component is disposed
    }
}
```

## Built-in Behavior Components

| Component | Description | Base Type |
|-----------|-------------|-----------|
| `AnimatorComponent` | Drives float animations with easing | BehaviorComponent |
| `CursorComponent` | Changes cursor on hover | BehaviorComponent |
| `DropComponent` | Drop target handling | BehaviorComponent |
| `LayoutComponent` | Abstract base for layout behaviors | BehaviorComponent |
| `LayoutObject` | Per-child layout control | BehaviorComponent |
| `Rotation3DTransformComponent` | 3D rotation transform | BehaviorComponent |
| `SelectableComponent` | Selection state management | BehaviorComponent |
| `StackContentComponent` | Stack/scroll layout of children | LayoutComponent |
| `ImageEffects` | Opacity, blur, saturation, brightness | BehaviorComponent |

## Behavior Event Types

| Event | When Called |
|-------|-------------|
| `BeforeBegin` | Before the Begin() method is called |
| `AfterBegin` | After the Begin() method is called |
| `BeforeLateBegin` | Before the LateBegin() method is called |
| `AfterLateBegin` | After the LateBegin() method is called |
| `BeforeSurfaceDraw` | Before the DrawToSurface() method is called |
| `AfterSurfaceDraw` | After the DrawToSurface() method is called |
| `BeforeRender` | Before the Render() method is called |
| `AfterRender` | After the Render() method is called |
| `BeforeDrawChildren` | Before all children are drawn |
| `AfterDrawChildren` | After all children are drawn |
| `BeforeDrawChild` | Before one child is drawn |
| `AfterDrawChild` | After one child is drawn |
| `BeforeEarlyUpdate` | Before the early update is called |
| `AfterEarlyUpdate` | After the early update is called |
| `BeforeUpdate` | Before the update is called |
| `AfterUpdate` | After the update is called |
| `BeforeLateUpdate` | Before the late update is called |
| `AfterLateUpdate` | After the late update is called |
| `BeforeTransform` | Before the transform is refreshed |
| `AfterTransform` | After the transform is refreshed |
| `BeforeLayout` | Before the layout is rebuilt |
| `AfterLayout` | After the layout is rebuilt |

## Properties

| Property | Description |
|----------|-------------|
| `Parent` | The UIComponent this component belongs to |
| `Enabled` | If the component is enabled |