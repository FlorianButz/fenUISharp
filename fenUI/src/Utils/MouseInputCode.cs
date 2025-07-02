using System.Diagnostics.CodeAnalysis;

namespace FenUISharp
{
    public struct MouseInputCode
    {
        public MouseInputButton button { get; init; }
        public MouseInputState state { get; init; }

        public MouseInputCode(MouseInputButton btn, MouseInputState state)
        {
            this.button = btn;
            this.state = state;
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj != null)
                return button == ((MouseInputCode)obj).button && state == ((MouseInputCode)obj).state;
            else return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(button, state);
        }
    }

    public enum MouseInputState : int { Down = 0, Up = 1 }
    public enum MouseInputButton : int { Left = 0, Right = 1, Middle = 2, None = 3 } 
}