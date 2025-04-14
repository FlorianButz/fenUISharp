# Introduction to Components

Components are a way to extend the behaivor of a `UIComponent`. Components are stored in a list inside every UIComponent. `Component` implements the `IDisposable` interface.

!> A `Component` always need a `UIComponent` as parent in order to work. This also means every Component automatically adds itself to the Components list inside the parent UIComponent. It also automatically removes itself from the list when it gets disposed.

## Constructors

`Component(UIComponent parent)`

## Virtual Methods

`virtual void OnBeforeRender(SKCanvas canvas) { }`

`virtual void OnAfterRender(SKCanvas canvas) { }`

`virtual void OnBeforeRenderCache(SKCanvas canvas) { }`

`virtual void OnAfterRenderCache(SKCanvas canvas) { }`

`virtual void OnBeforeRenderChildren(SKCanvas canvas) { }`

`virtual void OnAfterRenderChildren(SKCanvas canvas) { }`

`virtual void ComponentSetup() { }`

`virtual void ComponentUpdate() { }`

`virtual void ComponentDestroy() { }`

`virtual void Selected() { }`

`virtual void SelectedLost() { }`

`virtual void MouseEnter() { }`

`virtual void MouseExit() { }`

`virtual void MouseAction(MouseInputCode inputCode) { }`

`virtual void GlobalMouseAction(MouseInputCode inputCode) { }`

`virtual void MouseMove(Vector2 pos) { }`

## Public Fields

`UIComponent Parent { get; init; }` returns the parent it belongs to.