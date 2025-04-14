# Global Hooks

The `GlobalHooks` class is for receiving events that don't happen inside the window's client area. This is especially useful for overlay applications. It is based on a singleton structure.

!> This class does not run on your window's UI thread and your methods that subscribe to the actions should not directly interact with fields / methods from the UI thread. Try to use your window's mouse / keyboard callbacks when possible.

## Public Methods

`void RegisterHooks()` registers all low level hooks for the current process. This is done automatically by calling `FenUI.Init()` (See [FenUI](docs/fen-ui.md ':include'))

`void UnregisterHooks()` unregisters all low level hooks for the current process. This is done automatically by calling `FenUI.Shutdown()` (See [FenUI](docs/fen-ui.md ':include'))

`static string GetKeyName(int vkCode)` returns the name of the key if it's a known `ConsoleKey`, otherwise returns `VK_{vkCode}`.

## Mouse

`Action<float> OnMouseScroll` is invoked whenever a mouse scroll is detected. The first parameter is the scroll delta. 

`Action<Vector2> OnMouseMove` is invoked whenever a position change of the mouse is detected. The first parameter is the new position.

`Action<Vector2> OnMouseMoveDelta` is invoked whenever a position change of the mouse is detected. The first parameter is the delta between old and new position.

`Action<MouseInputCode> OnMouseAction` is invoked whenever an input of the mouse is detected. The first parameter is the input data. (See [Mouse Input Code](docs/utils/mouse-input-code.md ':include'))

`static Vector2 MousePosition { get; }` returns the last recorded global mouse position.

## Keyboard

`Action<int> OnKeyPressed` is invoked whenever a key press is detected. Automatically filters out repeated key presses when key is not released first. The first parameter is the vkCode of the pressed key.

`Action<int> OnKeyTyped` is always invoked whenever a key input is detected. The first parameter is the vkCode of the pressed key.

`Action<int> OnKeyReleased` is invoked whenever a key is released. First parameter is the vkCode.
