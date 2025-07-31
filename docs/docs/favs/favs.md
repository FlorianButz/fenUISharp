# FenUI Animatable Vectors

FAV (FenUI Animatable Vector) is a special file format that allows for animated scalable vector graphics. While it has a few things common with plain SVGs, they are not the same and have slightly different syntax.  

## Structure

``` XML
<fav id="eye-symbol">

    <!-- The shape of the symbol is defined in the 'symbol' element -->
    <symbol fill="#c5c5c503"
            stroke="#d9d9d9ff"
            stroke-width="2"
            extend-bounds="10"
            viewBox="0 0 48 48">

    <!-- The inner content of the symbol is the shape data. This is equivalent to the inner content of an SVG block.
    It can be copied from an SVG directly into here -->
        <path d="M2.062 12.348a1 1 0 0 1 0-.696 10.75 10.75 0 0 1 19.876 0 1 1 0 0 1 0 .696 10.75 10.75 0 0 1-19.876 0" />
        <circle cx="12"
                cy="12"
                r="3" />

    </symbol>

    <!-- Animations are defined in a separate block -->
    <animations>

        <!-- All animations must have an id and define the affected paths. Typing '*' as the value will affect all paths -->
        <animation affected-paths="*"
                    id="eye-blink"
                    duration="2"
                    easing="linear">

            <!-- Animations contain keyframes. A good practice is to always add a keyframe for time = 0 and time = 1 -->
            <keyframe time="0.2">
                <scale-y value="1" />
            </keyframe>
            <keyframe time="0.3">
                <scale-y value="0" />
            </keyframe>
            <keyframe time="0.4">
                <scale-y value="1" />
            </keyframe>
            <keyframe time="0.6">
                <scale-y value="1" />
            </keyframe>
            <keyframe time="0.7">
                <scale-y value="0" />
            </keyframe>
            <keyframe time="0.8">
                <scale-y value="1" />
            </keyframe>

        </animation>

        <!-- 1 is the first element inside the symbol block (not necesarily a path but can be a circle, rectangle, etc... as well) -->
        <animation affected-paths="1"
                    id="eye-blink"
                    duration="2"
                    per-keyframe-ease="true"
                    easing="cubic-bezier(.73,0,.29,.99)">

            <keyframe time="0">
                <translate-x value="0" />
            </keyframe>
            <keyframe time="0.25">
                <translate-x value="1" />
            </keyframe>
            <keyframe time="0.75">
                <translate-x value="-1" />
            </keyframe>
            <keyframe time="1">
                <translate-x value="0" />
            </keyframe>

        </animation>
    </animations>
</fav>
```

## Attributes

### Symbol

The `Symbol` block has following attributes:
- `fill="#FFFFFFFF"`: The color which fills the shape. If not specified fill will be transparent
- `stroke="#FFFFFFFF"`: The color of the shape's stroke. If not specified fill will be transparent
- `stroke-width="1"`: The stroke's thickness
- `stroke-linecap="round|butt|square"`
- `stroke-linejoin="round|bevel|miter"`
- `viewBox="0 0 24 24"`: The viewbox of the symbol. Can be copied from SVG as well

### Animation

The `Animation` block has following attributes:
- `id="eye-blink`: The name of the animation which is later required for playing the animation in C#
> Multiple animations can have the same identifier (`id`). They will be triggered together when calling the animation from C#
- `affected-paths=""`: The paths which this animation wants to affect. Value can be either individual indecies separated by whitespace or '*' to select all paths.
- `duration="1"`: The length of the animation in seconds
- `easing=""`: The type of easing used for the animation. Types of easings include:
    - `linear`
    - `ease`
    - `ease-in`
    - `ease-out`
    - `ease-in-out`
    - `snap`: Will snap the value to either the last or next keyframe
    - `cubic-bezier(x1, y1, x2, y2)`: Same as the cubic bezier animation type from CSS
    - `spring(speed, springieness)`: Uses the internal `Spring` component
    - `snap-spring(speed, springieness)`: Uses the internal `Spring` component, but in a way that is optimized for springs. (Looks better)
- `dont-reset="true"`: Will keep the values after the animation is done
- `use-object-anchor="true"`: Will use the `UIObject`'s bounds to calculate the anchor point instead of the path's bounds.
- `use-object-size-translation="true"`: Will use the `UIObject`'s bounds to calculate translation instead of the path's bounds.
> Due to the scalable nature of FAVs, translations are done relative to the path's size instead of absolute sizes (like pixels).
> 1 translation is the size of the path (or the parent `UIObject` if the above flag is set to true).
- `per-keyframe-ease="true"`: Will ease the time inbetween individual keyframes instead of the animation as a whole.

### keyframe

The `Keyframe` block has following attributes:
- `time="0"`: Specifies the time at which the keyframe sits

### Animatable Attributes

Following blocks can be placed inside of a `Keyframe`:
- `<translate-x value="0">`: Translation on x-axis
- `<translate-y value="0">`: Translation on y-axis
- `<scale-x value="0">`: Scale on x-axis
- `<scale-y value="0">`: Scale on y-axis
- `<anchor-x value="0.5">`: Anchor on x-axis (0.5 is middle)
- `<anchor-y value="0.5">`: Anchor on y-axis (0.5 is middle)
- `<rotate value="0">`: Rotation around z-axis
- `<opacity value="1">`: Opacity of the path
- `<blur-radius value="0">`: Amount of blurring
- `<stroke-trace value="1">`: Will animate the stroke to appear along the path

## Animation Naming

The best naming practices are:
- Animations with id `in` are played after the icon is switched to (This is done automatically by fenUI).
- Animations with id `out` are played before the icon is being switched (This is done automatically by fenUI).
- Animations with id `action` are played when the action is executed.