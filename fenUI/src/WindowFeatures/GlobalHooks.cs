using System.Runtime.InteropServices;

namespace FenUISharp
{

    public class GlobalHooks : IDisposable
    {
        private static GlobalHooks? instance;

        private static IntPtr _mouseHookID = IntPtr.Zero;
        private static IntPtr _keyboardHookID = IntPtr.Zero;

        private LowLevelMouseProc _mouseProc = MouseHookCallback;
        private LowLevelKeyboardProc _keyboardProc = KeyboardHookCallback;

        #region Event Callbacks

        public Action<float> onMouseScroll { get; set; }
        public Action<Vector2> onMouseMove { get; set; } // Gives back the mouse position in the Vector2
        public Action<Vector2> onMouseMoveDelta { get; set; } // Gives back the mouse delta in the Vector2

        public Action<int> onKeyPressed { get; set; } // Only when user actually presses key
        public Action<int> onKeyTyped { get; set; } // Raw windows key event callback
        public Action<int> onKeyReleased { get; set; }

        #endregion

        #region Properties

        private static Vector2 lastMousePosition = new Vector2(0, 0);
        private static Vector2 mousePosition = new Vector2(0, 0);
        public static Vector2 MousePosition { get => mousePosition; }

        #endregion

        public GlobalHooks()
        {
            instance = this;
        }

        public void Dispose()
        {
            UnregisterHooks();
            instance = null;
        }

        public void RegisterHooks()
        {
            IntPtr moduleHandle = GetModuleHandle(null);

            _keyboardHookID = SetWindowsHookEx((int)WH_KEYBOARD_LL, _keyboardProc, moduleHandle, 0);
            _mouseHookID = SetWindowsHookEx((int)WH_MOUSE_LL, _mouseProc, moduleHandle, 0);
        }

        public void UnregisterHooks()
        {
            if (_keyboardHookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_keyboardHookID);
                _keyboardHookID = IntPtr.Zero;
            }

            if (_mouseHookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_mouseHookID);
                _mouseHookID = IntPtr.Zero;
            }
        }

        private static IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (instance == null) throw new InvalidOperationException("Mouse Hook Callback was called with no active instance.");

            if (nCode >= 0)
            {
                MSLLHOOKSTRUCT mouseInfo = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);

                // Mouse coordinates
                int mouseX = mouseInfo.pt.x;
                int mouseY = mouseInfo.pt.y;

                // Mouse wheel scrolling
                if (wParam == (IntPtr)WM_MOUSEWHEEL)
                {
                    short scrollDelta = (short)((mouseInfo.mouseData >> 16) & 0xFFFF);
                    instance.onMouseScroll?.Invoke(scrollDelta);
                }

                if (wParam == (IntPtr)WindowMessages.WM_MOUSEMOVE)
                {
                    mousePosition.x = mouseX;
                    mousePosition.y = mouseY;

                    instance.onMouseMove?.Invoke(new Vector2(mouseX, mouseY));
                    instance.onMouseMoveDelta?.Invoke(new Vector2(mouseX - lastMousePosition.x, mouseY - lastMousePosition.y));

                    lastMousePosition.x = mouseX;
                    lastMousePosition.y = mouseY;
                }
            }

            return CallNextHookEx(_mouseHookID, nCode, wParam, lParam);
        }

        private static List<int> _pressedKeys = new List<int>(); // Important for checking if a key was already pressed without having released it prior. (Windows key behavior filter)

        private static IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (instance == null) throw new InvalidOperationException("Keyboard Hook Callback was called with no active instance.");

            if (nCode >= 0)
            {
                KBDLLHOOKSTRUCT keyInfo = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);

                if (wParam == (IntPtr)WM_KEYDOWN)
                {
                    if (!_pressedKeys.Contains(keyInfo.vkCode))
                        instance.onKeyPressed?.Invoke(keyInfo.vkCode);
                    _pressedKeys.Add(keyInfo.vkCode);

                    instance.onKeyTyped?.Invoke(keyInfo.vkCode);
                }
                else if (wParam == (IntPtr)WM_KEYUP)
                {
                    instance.onKeyReleased?.Invoke(keyInfo.vkCode);

                    if (_pressedKeys.Contains(keyInfo.vkCode))
                        _pressedKeys.Remove(keyInfo.vkCode);
                }
            }

            return CallNextHookEx(_keyboardHookID, nCode, wParam, lParam);
        }

        public static string GetKeyName(int vkCode)
        {
            return Enum.IsDefined(typeof(ConsoleKey), vkCode) ? ((ConsoleKey)vkCode).ToString() : $"VK_{vkCode}";
        }

        const int WH_MOUSE_LL = 14;
        const int WH_KEYBOARD_LL = 13;
        const int WM_MOUSEWHEEL = 0x020A;
        const int WM_KEYDOWN = 0x0100;
        const int WM_KEYUP = 0x0101;

        public delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
        public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        public struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KBDLLHOOKSTRUCT
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(
            int idHook,
            LowLevelMouseProc lpfn,
            IntPtr hMod,
            uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(
            int idHook,
            LowLevelKeyboardProc lpfn,
            IntPtr hMod,
            uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(
            IntPtr hhk,
            int nCode,
            IntPtr wParam,
            IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);
    }
}