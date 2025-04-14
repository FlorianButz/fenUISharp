# Theme Manager

The `ThemeManager` class manages the current theme and can notify a change in the theme. Every `UIComponent` automatically invalidates when a change in theme occurs.
For getting a theme color, an instance of the ThemeManager is required. Every `Window` already has a `ThemeManager` instance and changing the theme there does not change it globally (for all other windows).

However, using the window's `ThemeManager` is not required. Creating another `ThemeManager` that handles a specific area is also possible, however most UIComponents default to using the window's `ThemeManager`.

## Constructors

`ThemeManager(Theme initialTheme)` creates a `ThemeManager` and sets the initial theme to the first parameter.

## Public Methods

`void SetTheme(Theme newTheme)` sets the current theme.

`ThemeColor GetColor(Func<Theme, SKColor> selector)` gets a specific type of color from the `Theme` class using the selector and returns it as a `ThemeColor`.

## Public Fields

`Theme CurrentTheme { get; }` returns the current `Theme`.

`event Action ThemeChanged` is invoked when the current theme changes.