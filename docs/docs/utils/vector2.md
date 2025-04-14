# Vector 2

`Vector2` is a struct which implements basic functionality to store a 2-Dimensional vector.

## Constructors

`Vector2(float x, float y)`

`Vector2(Vector2 v)` copies the vector.

## Public Methods

`static Vector2 Clamp(Vector2 value, Vector2 min, Vector2 max)` clamps a vector inside given bounds.

`static Vector2 Lerp(Vector2 from, Vector2 to, float t)` linearly interpolates between two given vectors using `t`.

`void Swap()` swaps the vector's values.

## Public Fields

`float Magnitude { get; }` returns the magnitude of the vector.

`Vector2 Swapped { get; }` returns a new vector that represents the swapped version of the original.

`float x` the horizontal value.

`float y` the vertical value.