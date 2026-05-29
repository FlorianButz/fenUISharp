# Theme Manager

Manages the current theme for a window and notifies when it changes. Every `UIObject` auto-invalidates on theme change.

## Creating a ThemeManager

```csharp
ThemeManager tm = new ThemeManager(myTheme);
```

## Getting Colors

```csharp
SKColor primary = tm.CurrentTheme.Primary;
SKColor bg      = tm.CurrentTheme.Background;
```

## Switching Themes

```csharp
tm.SetTheme(newTheme);
```

## Window ThemeManager

Every `FWindow` has its own `ThemeManager`. Changing the theme on one window does not affect others.

```csharp
FContext.GetCurrentWindow().WindowThemeManager.SetTheme(darkTheme);
```

## System Dark Mode

```csharp
window.Properties.UseSystemDarkMode = true;
// The window theme will be set
```

Changing the system theme does not affect the FenUI window theme.

## Events

```csharp
tm.ThemeChanged += () => {
    // Theme changed
};
```
