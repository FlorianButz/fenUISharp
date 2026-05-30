# FenUI

The base class for initializing and managing the FenUISharp framework.

## Initialization

```csharp
// Must be called first; loads resources, sets up window features
FenUI.Init();

// Required for some Windows features (notifications, etc.)
FenUI.SetupAppModel("com.example.app"); 

// ... your app code ...

// Cleanup at program end
FenUI.Shutdown();
```

`Init()` calls `Resources.LoadDefault()` to register built-in typefaces (Inter, Segoe UI), images, and themes (default-dark, default-light).

## Properties

| Property | Description |
|----------|-------------|
| `static Version FenUIVersion` | The current FenUISharp version |
| `static bool HasBeenInitialized` | Whether `Init()` has been called |
| `static HashSet<string> Flags` | Feature flags that modify framework behavior |

### Flags

- `"disable_crashhandler"`: Disables the crash handler. This flag can only be passed into the `Init()` function
- `"disable_winfeatures"`: Disables the initialization of window features like the media controls or toast messages. This flag can only be passed into the `Init()` function
- `"disable_pixelsnap"`: Disables pixel snapping on all transforms. Useful for highly dynamic interfaces that require smooth position changes. This flag can be enabled at any point
- `"disable_blureffects"`: Disables all FenUI blur effects. This flag can be enabled at any point
- `"force_snapshotblit"`: Forces `CachedSurface` to use snapshots for composition

## Demo

```csharp
FenUI.Demo(); // Launches the built-in demonstration window
```

## FContext

The `FContext` static class provides access to the current window context from anywhere in your code:

```csharp
FWindow window = FContext.GetCurrentWindow();
Dispatcher dispatcher = FContext.GetCurrentDispatcher();
ModelViewPane rootPane = FContext.GetRootViewPane();
ThemeManager theme = FContext.GetCurrentThemeManager();
KeyboardInputManager keyboard = FContext.GetKeyboardInputManager();

float deltaTime = FContext.DeltaTime;
float time = FContext.Time;
```

All context methods are `[ThreadStatic]`, meaning each UI thread has its own window context. Calls to `FContext` methods are only valid within a window's update/render callbacks.

## Version

```csharp
Console.WriteLine($"FenUISharp v{FenUI.FenUIVersion}");
```
