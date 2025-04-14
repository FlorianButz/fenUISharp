# Mouse Input Code
`MouseInputCode` is a public struct which contains data on what action the mouse performed.

## Constructors

`MouseInputCode(int btn, int state)`

`MouseInputCode(MouseInputButton btn, MouseInputState state)`


## Public Fields

`int button { get; init; }` describes which button.
`int state { get; init; }` describes what happened (up, down).

## MouseInputButton

`MouseInputButton` is an enum which contains the possible values for `button`/`btn`.
```
Left = 0,
Right = 1,
Middle = 2
```

## MouseInputState

`MouseInputState` is an enum which contains the possible values for `state`.
```
Down = 0,
Up = 1
```
