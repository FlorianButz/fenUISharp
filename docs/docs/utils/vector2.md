# Vector2

A struct for 2D vectors.

## Constructors

```csharp
Vector2 v = new Vector2(100f, 200f);
Vector2 copy = new Vector2(v); // copy constructor
```

## Methods

| Method | Description |
|--------|-------------|
| `Clamp(min, max)` | Clamp within bounds |
| `Lerp(from, to, t)` | Linear interpolation |
| `Swap()` | Swap x and y |

## Properties

| Property | Description |
|----------|-------------|
| `x` | Horizontal value |
| `y` | Vertical value |
| `Magnitude` | Vector length |
| `Swapped` | New vector with x/y swapped |
