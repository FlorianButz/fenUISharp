# User Drag Component

This component is for detecting a drag action from the user on a specific `UIComponent`.

## Public Fields

`Action? OnDragStart { get; set; }` will be invoked when the dragging is initiated.

`Action? OnDragEnd { get; set; }` will be invoked when the dragging is done.

`Action<Vector2>? OnDrag { get; set; }` will be invoked every update call if the user is currently dragging. First parameter is the delta of the global mouse position from before the drag started and the current global mouse position.

`Action<Vector2>? OnDragDelta { get; set; }` will be invoked every update call if the user is currently dragging. First parameter is the delta of the global mouse position from the last update call and the current global mouse position.

`bool IsDragging { get; }`
