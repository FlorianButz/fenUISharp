# Context Menus

Floating popup panels for menus and tooltips.

## FPopupPanel

A floating popup with an optional tail arrow pointing to a target, auto-positioning, and close-on-outside-click.

```csharp
FPopupPanel popup = new(
    size: () => new Vector2(200, 300),
    hasTail: true
);
popup.Show(() => targetPosition);
```

### Properties

| Property | Description |
|----------|-------------|
| `HasTail` | If the popup should have a tail or not |
| `TailHeight` / `TailWidth` | Tail dimensions |
| `TailCornerRadius` | The radius of the edges of the tail |
| `ScaleAnimationFromZero` | Whether the popup should animate from zero scale or from a higher one |
| `DistanceToTarget` | The distance the popup keeps to the target position |
| `DisposeOnClose` | Whether the popup should be destroyed once closed or not |
| `AllowEscapeClosing` | Whether the popup should be able to be closed by pressing escape |
| `AutoClose` | Whether the popup should close automatically when clicking somewhere else |
| `CloseOnOtherOpen` | Whether the popup should close automatically when another one opens |

### Methods

| Method | Description |
|--------|-------------|
| `Show(Vector2 position)` | Show the popup at a position |
| `Close()` | Close the popup |

## FContextMenuFactory

Creates a standard context menu with `FContextMenuButton` items in a popup.

```csharp
// Trigger from a button
FSimpleButton btn = new("Open Menu", () => {
    FContextMenuFactory.CreateContextMenu();
});
```

## Creating a Custom Popup

```csharp
FPopupPanel popup = new(
    size: () => new(200, 200),
    hasTail: false
) {
    DisposeOnClose = true
};

FPanel content = new();
content.Layout.StretchHorizontal.SetStaticState(true);
content.SetParent(popup);

FText label = new(TextModelFactory.CreateBasic("Popup content"));
label.SetParent(content);

// Later in mouse click callback

// Make sure to capture the mouse position so the popup doesn't follow the mouse
var mousePosCapture = new Vector2(FContext.GetCurrentWindow().ClientMousePosition.x, FContext.GetCurrentWindow().ClientMousePosition.y);
popup.Show(() => mousePosCapture);
```
