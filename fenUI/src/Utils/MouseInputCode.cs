using System.Diagnostics.CodeAnalysis;

namespace FenUISharp
{
    public struct MouseInputCode
    {
        public int button { get; init; }
        public int state { get; init; }

        public MouseInputCode(int btn, int state)
        {
            this.button = btn;
            this.state = state;
        }

        public MouseInputCode(MouseInputButton btn, MouseInputState state)
        {
            this.button = (int)btn;
            this.state = (int)state;
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return button == ((MouseInputCode)obj).button && state == ((MouseInputCode)obj).state;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(button, state);
        }
    }

    public enum MouseInputState : int { Down = 0, Up = 1 }
    public enum MouseInputButton : int { Left = 0, Right = 1, Middle = 2 } 
}