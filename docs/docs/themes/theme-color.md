# Theme Color

The `ThemeColor` class is what a component should hold. Instead of defining a fixed `SKColor` it is better to store a reference to a `ThemeColor` since it dynamically provides the wanted type of color based on the current theme and can change throughout the runtime of the application.

## Constructors

`ThemeColor(Func<SKColor> colorProvider)` where `colorProvider` can be any color provider.

`ThemeColor(SKColor fixedColor)` where `fixedColor` is a fixed color that can be defined for special cases.


## Public Methods

`void SetOverride(SKColor color)` sets an override color which will be returned when accessing `Value` instead of the color provider.

`void ResetOverride()` resets the overriden color.

## Public Fields

`SKColor Value { get; }` is where the `SKColor` can be retrieved.

## Examples

```csharp

public ThemeColor background;
public ThemeColor primary;

public Foo()
{
    // Using a basic static color
    background = new ThemeColor(SKColor.White); 

    // Using a selector to retrieve the primary color of a given theme.
    // Needs a ThemeManager instance first
    primary = WindowThemeManager.GetColor(t => t.Shadow); 
}

public void DrawToSurface(SKCanvas canvas)
{
    using(var paint = new SKPaint() { Color = background.Value }){
        canvas.DrawRect(..., paint);
    }

    using(var paint = new SKPaint() { Color = primary.Value }){
        canvas.DrawRect(..., paint);
    }
}

```