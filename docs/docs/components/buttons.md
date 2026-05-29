# Buttons

All buttons extend the abstract `Button` class, which provides hover animation (scale + color mix), click handling, and squircle shape.

## FSimpleButton

A text-labeled button that auto-sizes based on text measurement.

```csharp
var text = new FText(TextModelFactory.CreateBasic("Click Me!"));
FSimpleButton btn = new(text, () => {
    // clicked
});
btn.SetParent(panel);
```

## FDisplayButton

A button that displays an `FDisplayableType` (image, FAV, or icon).

```csharp
FDisplayButton btn = new(myFavDisplay, () => {
    // clicked
});
```

## FLargeSelectButton

A large selectable button with animated border, display icon, and label. Changes opacity on selection.

```csharp
FLargeSelectButton btn = new(myIcon, text);
btn.SetParent(panel);
```

## FContextMenuButton

A compact button for use inside context menus.

```csharp
FContextMenuButton btn = new("Menu Item", () => {
    // action
});
```

## FButtonGroup

Manages a group of `SelectableButton` instances. Supports multi-select and always-one-selected modes.

```csharp
FButtonGroup group = new();
group.Add(toggle1);
group.Add(toggle2);
group.AllowMultiSelect = false;
group.AlwaysMustSelectOne = true;
```

### Properties

| Property | Description |
|----------|-------------|
| `AllowMultiSelect` | Allow multiple selections |
| `AlwaysMustSelectOne` | Always keep one item selected |
| `OnSelectionChanged` | Callback when selection changes |
