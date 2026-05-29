# Mouse Input Code

`MouseInputCode` contains data about a mouse input event.

## Constructor

```csharp
MouseInputCode code = new MouseInputCode(btn, state);
```

## Fields

| Field | Description |
|-------|-------------|
| `button` | Which button was pressed |
| `state` | Whether it was pressed or released |

## MouseInputButton

| Value | Button |
|-------|--------|
| `0` | Left |
| `1` | Right |
| `2` | Middle |

## MouseInputState

| Value | State |
|-------|-------|
| `0` | Down |
| `1` | Up |
