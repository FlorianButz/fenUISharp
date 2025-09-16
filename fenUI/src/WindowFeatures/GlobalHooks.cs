using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using FenUISharp.Mathematics;
using FenUISharp.Native;

namespace FenUISharp.WinFeatures
{
    public class GlobalHooks : IDisposable
    {
        private static GlobalHooks? instance;

        private static IntPtr _mouseHookID = IntPtr.Zero;
        private static IntPtr _keyboardHookID = IntPtr.Zero;

        private LowLevelMouseProc _mouseProc = MouseHookCallback;
        private LowLevelKeyboardProc _keyboardProc = KeyboardHookCallback;

        #region Event Callbacks

        public Action<float>? OnMouseScroll { get; set; }
        public Action<Vector2>? OnMouseMove { get; set; } // Gives back the mouse position in the Vector2
        public Action<Vector2>? OnMouseMoveDelta { get; set; } // Gives back the mouse delta in the Vector2
        public Action<MouseInputCode>? OnMouseAction { get; set; }

        public Action<int>? OnKeyPressed { get; set; } // Only when user actually presses key
        public Action<int>? OnKeyTyped { get; set; } // Raw windows key event callback
        public Action<int>? OnKeyReleased { get; set; }

        #endregion

        #region Properties

        private static Vector2 lastMousePosition = new Vector2(0, 0);
        private static Vector2 mousePosition = new Vector2(0, 0);
        public static Vector2 MousePosition { get => mousePosition; }
        public static bool MouseDown { get; private set; }

        #endregion

        AutoResetEvent signal = new AutoResetEvent(false);
        ConcurrentQueue<InputEvent> queue = new ConcurrentQueue<InputEvent>();

        bool[] keyFlags = new bool[256];

        public GlobalHooks()
        {
            if (instance == null) instance = this;
            else throw new InvalidOperationException("You can not create multiple instances of the GlobalHooks class.");

            Thread dispatchThread = new Thread(() =>
            {
                while (instance != null)
                {
                    signal.WaitOne();
                    HandleConcurrentQueue();
                }
            });
            dispatchThread.IsBackground = true;
            dispatchThread.Start();
        }

        private void HandleConcurrentQueue()
        {
            while (queue.TryDequeue(out var evt))
                HandleEvent(evt);
        }

        private void HandleEvent(InputEvent evt)
        {
            int nCode = evt.nCode;
            IntPtr wParam = evt.wParam;
            IntPtr lParam = evt.lParam;

            if (nCode < 0) return;

            if (evt.type == 1)
            {
                MSLLHOOKSTRUCT mouseInfo = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);

                // Mouse coordinates
                int mouseX = mouseInfo.pt.x;
                int mouseY = mouseInfo.pt.y;

                // Mouse wheel scrolling
                if (wParam == (IntPtr)GLOBALHOOKTYPE.WM_MOUSEWHEEL)
                {
                    short scrollDelta = (short)((mouseInfo.mouseData >> 16) & 0xFFFF);
                    OnMouseScroll?.Invoke(scrollDelta);
                }

                // Mouse move callbacks
                if (wParam == (IntPtr)WindowMessages.WM_MOUSEMOVE)
                {
                    mousePosition.x = mouseX;
                    mousePosition.y = mouseY;

                    OnMouseMove?.Invoke(new Vector2(mouseX, mouseY));
                    OnMouseMoveDelta?.Invoke(new Vector2(mouseX - lastMousePosition.x, mouseY - lastMousePosition.y));

                    lastMousePosition.x = mouseX;
                    lastMousePosition.y = mouseY;
                }

                // Mouse button events
                switch ((MouseMessages)wParam)
                {
                    case MouseMessages.WM_LBUTTONDOWN:
                        OnMouseAction?.Invoke(new MouseInputCode(MouseInputButton.Left, MouseInputState.Down));
                        MouseDown = true;
                        break;
                    case MouseMessages.WM_LBUTTONUP:
                        OnMouseAction?.Invoke(new MouseInputCode(MouseInputButton.Left, MouseInputState.Up));
                        MouseDown = false;
                        break;
                    case MouseMessages.WM_RBUTTONDOWN:
                        OnMouseAction?.Invoke(new MouseInputCode(MouseInputButton.Right, MouseInputState.Down));
                        break;
                    case MouseMessages.WM_RBUTTONUP:
                        OnMouseAction?.Invoke(new MouseInputCode(MouseInputButton.Right, MouseInputState.Up));
                        break;
                    case MouseMessages.WM_MBUTTONDOWN:
                        OnMouseAction?.Invoke(new MouseInputCode(MouseInputButton.Middle, MouseInputState.Down));
                        break;
                    case MouseMessages.WM_MBUTTONUP:
                        OnMouseAction?.Invoke(new MouseInputCode(MouseInputButton.Middle, MouseInputState.Up));
                        break;
                }
            }
            else if (evt.type == 2)
            {
                // Extracting the input info from the lParam
                KBDLLHOOKSTRUCT keyInfo = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);

                // Check if key up or key down event

                if (wParam == (IntPtr)GLOBALHOOKTYPE.WM_KEYDOWN)
                {
                    if (!keyFlags[keyInfo.vkCode])
                    {
                        keyFlags[keyInfo.vkCode] = true;
                        OnKeyPressed?.Invoke(keyInfo.vkCode);
                    }

                    OnKeyTyped?.Invoke(keyInfo.vkCode);
                }
                else if (wParam == (IntPtr)GLOBALHOOKTYPE.WM_KEYUP)
                {
                    OnKeyReleased?.Invoke(keyInfo.vkCode);
                    keyFlags[keyInfo.vkCode] = false;
                }
            }
        }

        private static IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (instance == null)
                return Win32APIs.CallNextHookEx(_mouseHookID, nCode, wParam, lParam);

            instance.queue.Enqueue(new InputEvent() { type = 1, lParam = lParam, wParam = wParam, nCode = nCode });
            instance.signal.Set();

            return Win32APIs.CallNextHookEx(_mouseHookID, nCode, wParam, lParam);
        }

        private static IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (instance == null)
                return Win32APIs.CallNextHookEx(_keyboardHookID, nCode, wParam, lParam);

            instance.queue.Enqueue(new InputEvent() { type = 2, lParam = lParam, wParam = wParam, nCode = nCode });
            instance.signal.Set();

            return Win32APIs.CallNextHookEx(_keyboardHookID, nCode, wParam, lParam);
        }

        public void RegisterHooks()
        {
            IntPtr moduleHandle = Win32APIs.GetModuleHandle(null);

            _keyboardHookID = Win32APIs.SetWindowsHookEx((int)GLOBALHOOKTYPE.WH_KEYBOARD_LL, _keyboardProc, moduleHandle, 0);
            _mouseHookID = Win32APIs.SetWindowsHookEx((int)GLOBALHOOKTYPE.WH_MOUSE_LL, _mouseProc, moduleHandle, 0);
        }

        public void UnregisterHooks()
        {
            if (_keyboardHookID != IntPtr.Zero)
            {
                Win32APIs.UnhookWindowsHookEx(_keyboardHookID);
                _keyboardHookID = IntPtr.Zero;
            }

            if (_mouseHookID != IntPtr.Zero)
            {
                Win32APIs.UnhookWindowsHookEx(_mouseHookID);
                _mouseHookID = IntPtr.Zero;
            }
        }

        public void Dispose()
        {
            UnregisterHooks();
            instance = null;
        }

        // TODO: ConsoleKey is very limited. Use GetKeyNameText or MapVirtualKey in the future
        public static string GetKeyName(int vkCode)
        {
            return Enum.IsDefined(typeof(ConsoleKey), vkCode) ? ((ConsoleKey)vkCode).ToString() : $"VK_{vkCode}";
        }

        internal struct InputEvent
        {
            public int type;
            public int nCode;
            public IntPtr wParam;
            public IntPtr lParam;
        }
    }
}