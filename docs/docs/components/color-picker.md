# Color Picker

HSV color picker.

## FColorPicker

Full HSV color picker with draggable knob, hue slider, and alpha slider.

```csharp
FColorPicker picker = new();
picker.OnColorUpdated = (color) => {
    // SKColor color
};
```

## FColorPatch

A color swatch button that opens an `FColorPicker` in a popup. Shows a checkerboard pattern for alpha transparency.

```csharp
FColorPatch patch = new();
patch.OnColorUpdated = (color) => {
    // SKColor color
};
patch.SetParent(panel);
```

### Behavior

The color patch will display a color along with the alpha. On interaction a popup with a color picker will appear that the user can change the color of the color patch with.

## FHueSlider / FAlphaSlider

Individual sliders used by the color picker, can be used standalone.

```csharp
FHueSlider hueSlider = new();
hueSlider.OnValueChanged = (hue) => { /* 0-360 */ };

FAlphaSlider alphaSlider = new();
alphaSlider.OnValueChanged = (alpha) => { /* 0-1 */ };
```
