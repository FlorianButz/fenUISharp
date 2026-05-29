# Cursor Component

Changes the cursor type when the mouse hovers over a `UIObject`.

## Usage

```csharp
CursorComponent cursor = new(panel);
cursor.CursorOnHover = Cursor.HAND;
```

## Properties

| Property | Description |
|----------|-------------|
| `CursorOnHover` | Cursor type shown on hover |

See [Cursor Enum](docs/utils/cursor.md) for available cursor types.
