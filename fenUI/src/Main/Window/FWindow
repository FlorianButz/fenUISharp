namespace FenUISharp {
    public class FWindow {
        
        public string WindowTitle { get; private set; }
        public string WindowClass { get; private set; }
        
        public IntPtr hWnd { get; private set; }

        public FWindow(string windowTitle, string windowClass) {
            CreateWin32Window();
        }

        private IntPtr CreateWin32Window(){
            string className = WindowClass;
            WNDCLASSEX wndClass = new WNDCLASSEX
            {
                cbSize = (uint)Marshal.SizeOf(typeof(WNDCLASSEX)),
                style = 0x0020, // CS_OWNDC
                lpfnWndProc = WndProc,
                hInstance = Marshal.GetHINSTANCE(typeof(Program).Module),
                lpszClassName = className
            };

            RegisterClassEx(ref wndClass);

            _width = GetSystemMetrics(0);  // SM_CXSCREEN
            _height = GetSystemMetrics(1); // SM_CYSCREEN

            // Create a borderless popup window with the layered style.
            hWnd = CreateWindowEx(
                WS_EX_LAYERED,
                className,
                WindowTitle,
                WS_POPUP,
                0, 0,
                _width, _height,
                IntPtr.Zero, IntPtr.Zero, wndClass.hInstance, IntPtr.Zero);

            SetAlwaysOnTop(hWnd, true);
        }

    }
}