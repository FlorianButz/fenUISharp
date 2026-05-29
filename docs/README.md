# FenUISharp

A native Windows C# UI framework for building modern, unique interfaces. FenUI renders everything through SkiaSharp and DirectComposition, giving you full control over every pixel.

## Features

- **Custom rendering** - All UI is drawn via SkiaSharp. No native Win32 controls.
- **Reactive state system** - Every property is a priority-aware `State<T>` that auto-invalidates on change.
- **Theming system** - Dynamic color themes with per-window theme managers.
- **Materials** - Panel materials, glass blur, gradient highlights, and compositing.
- **Animated vectors** - FAV format for scalable, animatable icons.
- **Rich components** - Buttons, sliders, toggles, color pickers, text inputs, context menus, and more.
- **Post-processing** - Bloom, gradient swipe, and custom per-object effects.
- **Animations** - Easing-based AnimatorComponent, spring physics, and state-driven animation.

## Quick Start

```csharp
using FenUISharp;

public class Program
{
    [STAThread]
    public static void Main()
    {
        FenUI.Init();
        FenUI.SetupAppModel("myapp.id");
        FenUI.Demo();
    }
}
```

See [Getting Started](docs/getting-started.md) for full setup instructions.

## Documentation

- [UIObject](docs/core/uiobject.md) - Base class for all visual elements
- [States](docs/core/states.md) - Reactive priority-based value system
- [Windows](docs/windows.md) - Window types and properties
- [Layout](docs/layout.md) - Manual layout system
- [Views](docs/core/views.md) - Screen management with ModelViewPane
- [Animation](docs/core/animation.md) - Animation methods and utilities
- [Components](docs/components/overview.md) - Built-in UI components
- [Theming](docs/themes/themes-introduction.md) - Theme system
- [Materials](docs/materials/overview.md) - Rendering materials
- [Post Process Chain](docs/core/post-process.md) - Bloom, gradient swipe, custom effects
