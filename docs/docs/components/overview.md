# Components Overview

FenUISharp ships with a set of built-in UI components. All components extend `UIObject` and support theming, materials, and behavior components.

## Container

- **FPanel** - Rounded-corner panel with squircle support. The base container for most UI.
- **FBlurPane** - Panel with real-time Gaussian blur of content behind it.

## Buttons

- **FSimpleButton** - Text-labeled button with auto-sizing.
- **FDisplayButton** - Button that shows an image or FAV icon.
- **FLargeSelectButton** - Large selectable button with animated border.
- **FButtonGroup** - Manages a group of selectable buttons.

## Toggles

- **FToggle** - Checkbox-style toggle with animated checkmark.
- **FRoundToggle** - iOS-style round toggle with spring-animated knob.
- **FSegmentedControl** - macOS-style segmented control with animated highlight.

## Numeric

- **FSlider** - Horizontal slider with knob, snapping, and keyboard support.
- **FNumericScroller** - Click-to-open popup numeric scroller.
- **FProgressBar** - Linear progress bar (determinate and indeterminate).
- **FRadialProgressBar** - Circular progress bar.

## Text

- **FText** - Text display with rich text models and layout processors.
- **FTextInputField** - Full text input with caret, selection, clipboard, and validation.

## Display

- **FImage** - Image display with Stretch/Fit/Contain scale modes.
- **FAVDisplay** - Animated vector display with animation playback.

## Context Menus

- **FPopupPanel** - Floating popup with tail, auto-positioning, and close-on-outside-click.
- **FContextMenuFactory** - Creates standard context menus with buttons.

## Color Picker

- **FColorPicker** - HSV color picker with shader-rendered gradient.
- **FColorPatch** - Color swatch button that opens a picker in a popup.

## Scrollbar

- **FScrollBar** - Drag-scrollable scrollbar with auto-fading, horizontal/vertical.

## Separator

- **FSeparator** - Thin horizontal or vertical line separator.
