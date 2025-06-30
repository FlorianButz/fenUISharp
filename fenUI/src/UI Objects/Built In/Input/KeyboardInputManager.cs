using FenUISharp.WinFeatures;

namespace FenUISharp
{
    public class KeyboardInputManager : IDisposable
    {
        public bool IsControlPressed { get; private set; }

        public KeyboardInputManager()
        {
            WindowFeatures.GlobalHooks.OnKeyPressed += KeyPressed;
            WindowFeatures.GlobalHooks.OnKeyReleased += KeyReleased;
        }

        private void KeyReleased(int obj)
        {
            // if (!FContext.GetCurrentWindow().IsWindowFocused) return;

            char c = (char)obj;

            if (char.IsControl(c)) IsControlPressed = false;
        }

        private void KeyPressed(int obj)
        {
            // if (!FContext.GetCurrentWindow().IsWindowFocused) return;

            char c = (char)obj;

            if (char.IsControl(c)) IsControlPressed = true;
        }

        public void Dispose()
        {
            WindowFeatures.GlobalHooks.OnKeyPressed -= KeyPressed;
            WindowFeatures.GlobalHooks.OnKeyReleased -= KeyReleased;
        }
    }
}