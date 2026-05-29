# FenUI Animatable Vectors

FAV (FenUI Animatable Vector) is a file format for animated scalable vector graphics. Similar to SVG, but with built-in native animation support.

## Structure

```xml
<fav id="my-icon">
    <symbol fill="#00000000"
            stroke="#d9d9d9ff"
            stroke-width="2"
            viewBox="0 0 48 48">
        <path d="M2.062 12.348a1 1 0 0 1 0-.696..." />
        <circle cx="12" cy="12" r="3" />
    </symbol>

    <animations>
        <animation affected-paths="*"
                   id="in"
                   duration="0.5"
                   easing="snap-spring(1, 0.6)">
            <keyframe time="0">
                <scale-x value="0" />
                <scale-y value="0" />
                <opacity value="0" />
            </keyframe>
            <keyframe time="1">
                <scale-x value="1" />
                <scale-y value="1" />
                <opacity value="1" />
            </keyframe>
        </animation>
    </animations>
</fav>
```

## Symbol Attributes

| Attribute | Description |
|-----------|-------------|
| `fill` | Shape fill color (transparent if omitted) |
| `stroke` | Stroke color (transparent if omitted) |
| `stroke-width` | Stroke thickness |
| `stroke-linecap` | `round`, `butt`, or `square` |
| `stroke-linejoin` | `round`, `bevel`, or `miter` |
| `viewBox` | Viewbox, same as SVG |

## Animation Attributes

| Attribute | Description |
|-----------|-------------|
| `id` | Animation name for C# playback |
| `affected-paths` | Paths to affect (`*` for all, or indices separated by whitespace) |
| `duration` | Length in seconds |
| `easing` | Easing type |
| `dont-reset` | Keep values after animation ends |
| `use-object-anchor` | Use parent bounds for anchor |
| `use-object-size-translation` | Use parent bounds for translation |
| `per-keyframe-ease` | Ease between keyframes individually |

## Easing Types

- `linear`, `ease`, `ease-in`, `ease-out`, `ease-in-out`
- `snap` - Snaps to last or next keyframe
- `cubic-bezier(x1, y1, x2, y2)` - CSS-style cubic bezier
- `spring(speed, springiness)` - Spring physics
- `snap-spring(speed, springiness)` - Optimized spring (looks better)

## Animatable Attributes

Place these inside `<keyframe>`:

| Attribute | Description |
|-----------|-------------|
| `translate-x` | X translation (relative to path size) |
| `translate-y` | Y translation |
| `scale-x` | X scale |
| `scale-y` | Y scale |
| `anchor-x` | X anchor (0.5 = center) |
| `anchor-y` | Y anchor |
| `rotate` | Z-axis rotation |
| `opacity` | Path opacity |
| `blur-radius` | Blur amount |
| `stroke-trace` | Animate stroke along path |

## Animation Naming

- `in` - Played when icon is shown (automatic).
- `out` - Played before icon is hidden (automatic).
- `action` - Played on user interaction.
