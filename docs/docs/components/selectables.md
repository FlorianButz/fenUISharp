# Toggles and Selectables

Toggle-able buttons with selection state and visual feedback.

## FToggle

Checkbox-style toggle with an animated checkmark icon.

```csharp
FToggle toggle = new();
toggle.IsSelected = true;
toggle.SetParent(panel);
```

## FRoundToggle

iOS-style round toggle with a spring-animated knob. Changes color when toggled.

```csharp
FRoundToggle toggle = new();
toggle.SetParent(panel);
```

## FSegmentedControl

macOS/iOS-style segmented control with a spring-animated selection highlight.

```csharp
FSegmentedControl segmented = new(
    new Dictionary<TextModel, Action<FSegmentedControl>> {
        [TextModelFactory.CreateBasic("Tab 1")] = (ctrl) => { /* selected */ },
        [TextModelFactory.CreateBasic("Tab 2")] = (ctrl) => { /* selected */ },
    },
    initiallySelectedIndex: 0
);
segmented.SetParent(panel);
```

### Properties

| Property | Description |
|----------|-------------|
| `IsSelected` | Current selection state |
| `SelectionChanged` | Callback when selection changes |
