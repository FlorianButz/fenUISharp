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

        private static float lastScrollDelta = 0;
        private static Vector2 lastMousePosition = new Vector2(0, 0);
        private static Vector2 mousePosition = new Vector2(0, 0);
        public static Vector2 MousePosition { get => mousePosition; }
        public static bool MouseDown { get; private set; }

        // These are set in the hook iteself
        private static float scrollDelta = 0f;
        private static Vector2 capturedMousePos;
        private static bool mouseUpdate;

        #endregion

        AutoResetEvent signal = new AutoResetEvent(false);
        ConcurrentQueue<InputEvent> queue = new ConcurrentQueue<InputEvent>();

        bool[] keyFlags = new bool[256];

        private bool _isRegistered = false;

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
            KBDLLHOOKSTRUCT keyInfo = evt.keyInfo;

            if (nCode < 0) return;

            if (evt.type == 1 || mouseUpdate)
            {
                mouseUpdate = false;

                // Mouse coordinates
                int mouseX = (int)capturedMousePos.x;
                int mouseY = (int)capturedMousePos.y;

                // Mouse wheel scrolling
                if (lastScrollDelta != scrollDelta)
                    OnMouseScroll?.Invoke(scrollDelta);

                // Mouse move callbacks
                if (lastMousePosition != capturedMousePos)
                {
                    mousePosition.x = mouseX;
                    mousePosition.y = mouseY;

                    OnMouseMove?.Invoke(new Vector2(mouseX, mouseY));
                    OnMouseMoveDelta?.Invoke(new Vector2(mouseX - lastMousePosition.x, mouseY - lastMousePosition.y));

                    lastMousePosition.x = mouseX;
                    lastMousePosition.y = mouseY;
                }

                if (evt.type != 1) return;
                MSLLHOOKSTRUCT mouseInfo = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);

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
                if (keyInfo.vkCode > 255) return;

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

            MSLLHOOKSTRUCT mouseInfo = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);

            // Mouse coordinates
            int mouseX = mouseInfo.pt.x;
            int mouseY = mouseInfo.pt.y;

            // Mouse wheel scrolling
            if (wParam == (IntPtr)GLOBALHOOKTYPE.WM_MOUSEWHEEL)
            {
                short scrollDelta = (short)((mouseInfo.mouseData >> 16) & 0xFFFF);
                GlobalHooks.scrollDelta += scrollDelta;
            }

            // Mouse move callbacks
            if (wParam == (IntPtr)WindowMessages.WM_MOUSEMOVE)
            {
                capturedMousePos.x = mouseX;
                capturedMousePos.y = mouseY;
            }

            mouseUpdate = true;

            instance.queue.Enqueue(new InputEvent() { type = 1, lParam = lParam, wParam = wParam, nCode = nCode });
            instance.signal.Set();

            return Win32APIs.CallNextHookEx(_mouseHookID, nCode, wParam, lParam);
        }

        private static IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (instance == null)
                return Win32APIs.CallNextHookEx(_keyboardHookID, nCode, wParam, lParam);

            var keyInfo = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            instance.queue.Enqueue(new InputEvent() { type = 2, lParam = lParam, wParam = wParam, nCode = nCode, keyInfo = keyInfo });
            instance.signal.Set();

            return Win32APIs.CallNextHookEx(_keyboardHookID, nCode, wParam, lParam);
        }

        public void RegisterHooks()
        {
            if (_isRegistered) return;
            _isRegistered = true;

            IntPtr moduleHandle = Win32APIs.GetModuleHandle(null);

            _keyboardHookID = Win32APIs.SetWindowsHookEx((int)GLOBALHOOKTYPE.WH_KEYBOARD_LL, _keyboardProc, moduleHandle, 0);
            _mouseHookID = Win32APIs.SetWindowsHookEx((int)GLOBALHOOKTYPE.WH_MOUSE_LL, _mouseProc, moduleHandle, 0);

            AppDomain.CurrentDomain.ProcessExit += UnregHooksExit;
        }

        private void UnregHooksExit(object? sender, EventArgs e)
            => UnregisterHooks();

        public void UnregisterHooks()
        {
            if (!_isRegistered) return;
            _isRegistered = false;

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

            AppDomain.CurrentDomain.ProcessExit -= UnregHooksExit;
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
            internal KBDLLHOOKSTRUCT keyInfo;
        }
    }
}