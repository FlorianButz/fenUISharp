using FenUISharp.WinFeatures;

namespace FenUISharp
{
    public class KeyboardInputManager : IDisposable
    {
        public bool IsControlPressed { get; private set; }
        public bool IsShiftPressed { get; private set; }
        public bool IsAltPressed { get; private set; }

        /// <summary>
        /// Does not filter for keys that did not trigger a release event before pressing again
        /// </summary>
        public Action<char>? OnKeyTyped { get; set; }

        /// <summary>
        /// Filters out keys that have triggered a press event without having triggered a release event before
        /// </summary>
        public Action<char>? OnKeyPressed { get; set; }
        public Action<char>? OnKeyReleased { get; set; }

        /// <summary>
        /// Gives the text ready callback for receiving keyboard inputs
        /// </summary>
        public Action<char>? OnTextTyped { get; set; }

        private Window _window { get; init; }

        private const char VK_CONTROL = (char)0x11;
        private const char VK_SHIFT = (char)0x10;
        private const char VK_LMENU = (char)0xA4;
        private const char VK_RMENU = (char)0xA5;

        private List<KeyBind> keybinds;

        public KeyboardInputManager(Window window)
        {
            this._window = window;
            keybinds = new();

            WindowFeatures.GlobalHooks.OnKeyTyped += KeyTyped;
            WindowFeatures.GlobalHooks.OnKeyPressed += KeyPressed;
            WindowFeatures.GlobalHooks.OnKeyReleased += KeyReleased;
            FContext.GetCurrentWindow().OnKeyboardInputTextReceived += TextTyped;
        }

        private void KeyTyped(int obj)
        {
            _window.Dispatcher.Invoke(() => OnKeyTyped?.Invoke((char)obj));
        }

        private void TextTyped(char c)
        {
            _window.Dispatcher.Invoke(() => OnTextTyped?.Invoke(c));
        }

        private void KeyReleased(int vkCode)
        {
            if (!_window.IsWindowFocused) return;
            vkCode = NormalizeVKCode(vkCode);

            char c = (char)vkCode;

            switch (c)
            {
                case VK_CONTROL:
                    IsControlPressed = false;
                    break;
                case VK_SHIFT:
                    IsShiftPressed = false;
                    break;
                case VK_RMENU:
                case VK_LMENU:
                    IsAltPressed = false;
                    break;
            }

            _window.Dispatcher.Invoke(() => OnKeyReleased?.Invoke(c));
        }

        private void KeyPressed(int vkCode)
        {
            if (!_window.IsWindowFocused) return;
            vkCode = NormalizeVKCode(vkCode);

            char c = (char)vkCode;

            switch (c)
            {
                case VK_CONTROL:
                    IsControlPressed = true;
                    break;
                case VK_SHIFT:
                    IsShiftPressed = true;
                    break;
                case VK_RMENU:
                case VK_LMENU:
                    IsAltPressed = true;
                    break;
            }

            foreach (var keybind in keybinds.ToList())
            {
                if (vkCode == keybind.VKCode || keybind.AliasVKCodes != null && keybind.AliasVKCodes.Contains(vkCode))
                {
                    if (keybind.Flags.HasFlag(KeyBindFlags.Control) && !IsControlPressed) continue;
                    if (keybind.Flags.HasFlag(KeyBindFlags.Alt) && !IsAltPressed) continue;
                    if (keybind.Flags.HasFlag(KeyBindFlags.Shift) && !IsShiftPressed) continue;

                    if (!keybind.Flags.HasFlag(KeyBindFlags.Control) && IsControlPressed) continue;
                    if (!keybind.Flags.HasFlag(KeyBindFlags.Alt) && IsAltPressed) continue;
                    if (!keybind.Flags.HasFlag(KeyBindFlags.Shift) && IsShiftPressed) continue;

                    _window.Dispatcher.Invoke(() => keybind.OnKeybindExecuted?.Invoke());
                }
            }

            _window.Dispatcher.Invoke(() => OnKeyPressed?.Invoke(c));
        }

        private int NormalizeVKCode(int vkCode)
        {
            if (vkCode == 0xA0 || vkCode == 0xA1) vkCode = 0x10; // SHIFT
            if (vkCode == 0xA2 || vkCode == 0xA3) vkCode = 0x11; // CONTROL
            if (vkCode == 0xA4 || vkCode == 0xA5) vkCode = 0x12; // ALT (MENU)

            return vkCode;
        }

        public void RegisterKeybind(KeyBind keybind) =>
            keybinds.Add(keybind);

        public void UnregisterKeybind(KeyBind keybind) =>
            keybinds.Remove(keybind);

        public void Dispose()
        {
            WindowFeatures.GlobalHooks.OnKeyPressed -= KeyPressed;
            WindowFeatures.GlobalHooks.OnKeyReleased -= KeyReleased;
        }
    }

    [Flags]
    public enum KeyBindFlags { None = 0, Control = 1, Alt = 2, Shift = 4 }

    public struct KeyBind
    {
        public KeyBindFlags Flags { get; init; }
        public int VKCode { get; init; }
        public int[] AliasVKCodes { get; init; }
        
        public Action? OnKeybindExecuted { get; set; }
    }
}