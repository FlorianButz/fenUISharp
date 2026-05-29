# Windows

FenUI provides three window types, each built on DirectComposition for hardware-accelerated rendering.

## FNativeWindow

Standard Win32 overlapped window with a title bar, resize, minimize, and maximize.

```csharp
FNativeWindow window = new();
window.Properties.Title = "My App";
window.Properties.Width = 800;
window.Properties.Height = 600;
window.Show();
```

## FTransparentWindow

Popup-style transparent window with hit-test control. Useful for tooltips, dropdowns, and overlays.

```csharp
FTransparentWindow window = new();
window.Properties.HitTestMode = FTransparentWindow.HitTestModes.Passthrough;

// Shows the window
window.Properties.IsWindowVisible = true;

// Blocking call, will continue after the window closes
window.BeginWindowLoop();
```

Hit test modes:
- `Passthrough` - Mouse passes through entirely.
- `Partial` - Some areas are clickable. (Interactable UIObjects)
- `Always` - Window always receives input. (Blocking)

## FOverlayWindow

Full-screen overlay that spans a monitor, excluded from the taskbar and Aero Peek. Useful for system-wide overlays.

```csharp
FOverlayWindow overlay = new();
overlay.Show();
```

## Window Properties

`FWindowProperties` controls window behavior:

| Property | Description |
|----------|-------------|
| `Title` | Window title |
| `Width` / `Height` | Window dimensions |
| `Resize` | Allow resizing |
| `Minimize` / `Maximize` | Allow minimize/maximize |
| `UseSystemDarkMode` | Follow system dark mode |
| `MicaBackdropType` | Mica backdrop (Normal, TransientWindow) |
| `IsVisible` | Window visibility |

## Window Callbacks

`FWindowCallbacks` provides events for input and lifecycle:

```csharp
window.Callbacks.OnPreUpdate += () => { /* before each frame */ };
window.Callbacks.OnResize += (size) => { /* window resized */ };
```

A list of all available window callbacks from the `FWindowCallbacks` class:

```csharp
public Action<FDropData?>? OnDragEnter { get; set; } // When the drag operation enters the window
public Action<FDropData?>? OnDragOver { get; set; } // When the drag operation is over the window
public Action<FDropData?>? OnDragDrop { get; set; } // When the drag operation is dropped on the window
public Action? OnDragLeave { get; set; } // When the drag operation leaves the window

public Action<MouseInputCode>? ClientMouseAction { get; set; } // Mouse actions in the client area
public Action<MouseInputCode>? TrayMouseAction { get; set; } // Mouse actions in the tray icon

public Action<float>? OnMouseScroll { get; set; } // When the mouse or trackpad is scrolled
public Action<Vector2>? OnMouseMove { get; set; } // Gives back the mouse position in the Vector2

public Action? OnMouseLeft { get; set; } // When the mouse leaves the client area

public Action? OnBeginRender { get; set; } // Before the render call
public Action? OnEndRender { get; set; } // After the render call

public Action<SKSurface>? OnWindowBeforeDraw { get; set; } // Before the window draw call
public Action<SKSurface>? OnWindowAfterDraw { get; set; } // After the window draw call

public Action? OnPostUpdate { get; set; } // After the logic update
public Action? OnPreUpdate { get; set; } // Before the logic update

public Action? OnDevicesChanged { get; set; }

public Action? OnFocusLost { get; set; } // When the window loses focus
public Action? OnFocusGained { get; set; } // When the window gains focus

public Action<Vector2>? OnWindowResize { get; set; } // When resizing
public Action<Vector2>? OnWindowEndResize { get; set; } // Once the resizing is done
public Action<Vector2>? OnWindowMove { get; set; } // When moving the window
public Action<Vector2>? OnWindowEndMove { get; set; } // After moving the window
public Action? DPIChanged { get; set; }

public Action? OnWindowClose { get; set; } // When the window is closed
public Action? OnWindowDestroy { get; set; } // When the window is destroyed

internal Action<char>? OnKeyboardInputTextReceived { get; set; } // When a character is typed in the window
// internal Action<char>? OnKeyTyped { get; set; } // After a key has been typed
internal Action<int>? OnKeyPressed { get; set; } // When a key is pressed
internal Action<int>? OnKeyReleased { get; set; } // When a key is released
```

## Theme Manager

Each window has its own `ThemeManager`. Changing the theme on one window does not affect others. See [Theming](docs/themes/themes-introduction.md).
