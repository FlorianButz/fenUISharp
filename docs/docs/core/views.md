# Views

FenUI uses a simple Model-View pattern for screen management. A `View` is an abstract class that produces a list of UIObjects, while a `ModelViewPane` hosts and manages the lifecycle of views.

## View

```csharp
public abstract class View
{
    public ModelViewPane? PaneRoot { get; set; }

    public abstract List<UIObject> Create();
    public virtual void OnViewShown() { }
    public virtual void OnViewDestroyed() { }
    public virtual void Update() { }
}
```

### Create()

The core method. Return all UIObjects that make up this view. They are automatically parented to the `ModelViewPane`:

```csharp
public class MainMenuView : View
{
    public override List<UIObject> Create()
    {
        var objects = new List<UIObject>();

        FPanel panel = new();
        panel.Layout.StretchHorizontal.SetStaticState(true);
        panel.Layout.StretchVertical.SetStaticState(true);
        objects.Add(panel);

        FText title = new(TextModelFactory.CreateBasic("Main Menu", fontSize: 24));
        title.SetParent(panel);
        objects.Add(title);

        return objects; // or just new() { panel, title }
    }
}
```

### Lifecycle

1. `Create()` is called when the view is loaded
2. `OnViewShown()` is called after `Create()` completes
3. `Update()` is called every frame while the view is active
4. `OnViewDestroyed()` is called when the view is replaced or disposed

> **NOTE** The view itself is not an UIObject, however it is displayed inside a ModelViewPane, which is an UIObject.

### Registering UIObjects

Use `AddUIObjectToRegisteredList()` to manually add objects outside of `Create()`:

```csharp
protected void AddUIObjectToRegisteredList(UIObject uiObject)
{
    // Registers the object so it's tracked and disposed with the view
}
```

## ModelViewPane

A `UIObject` subclass that hosts a `View` and manages animated transitions between views.

```csharp
ModelViewPane pane = new(initialView);

// Switch to another view with animation
pane.ViewModel = newMenuView;
```

### Animated Transitions

When the `ViewModel` property is set, the pane automatically animates the transition:

1. The current view animates out (duration: `AnimInDuration`)
2. The current view's objects are disposed
3. The new view's `Create()` is called and objects are parented
4. The new view animates in (duration: `AnimOutDuration`)

```csharp
pane.AnimateViewModelSwap = true;  // default: true
pane.AnimInDuration = 0.4f;
pane.AnimOutDuration = 0.1f;
pane.OnAnimationComplete = () => { /* transition done */ };
```

The animation affects scale, opacity, and blur radius through `ImageEffects`. The pane's `AnimatorComponent` drives the transition using `EaseInCubic` for the out animation and `EaseOutCubic` for the in animation.

### Default Root

Every `FWindow` has a root `ModelViewPane` that serves as the default parent for all UIObjects. Access it via:

```csharp
FContext.GetRootViewPane();
```

New UIObjects that don't explicitly call `SetParent()` are automatically parented to this root pane.

### Manual View Management

```csharp
// Set a view without animation
pane.SilentSetView(myView);

// Dispose current view items
pane.DisposeItems(); // calls OnViewDestroyed, disposes all children
```

## Complete Example

```csharp
public class MyView : View
{
    private FText label;

    public override List<UIObject> Create()
    {
        var result = new List<UIObject>();

        FPanel panel = new();
        panel.Layout.StretchHorizontal.SetStaticState(true);
        panel.Layout.StretchVertical.SetStaticState(true);
        result.Add(panel);

        label = new FText(TextModelFactory.CreateBasic("Hello"));
        label.SetParent(panel);
        result.Add(label);

        return result;
    }

    public override void Update()
    {
        label.Model = TextModelFactory.CreateBasic($"Time: {FContext.Time:F1}");
    }
}

// Usage
ModelViewPane pane = new(new MyView());
```
