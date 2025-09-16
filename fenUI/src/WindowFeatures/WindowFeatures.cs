namespace FenUISharp.WinFeatures {
    
    public static class WindowFeatures {

        private static bool _hasBeenInitialized;

        private static DesktopCapture? _desktopCapture;
        private static WindowsMediaControls? _mediaControls;
        private static GlobalHooks? _globalHooks;
        private static ToastMessageSender? _toastMessageSender;

        public static DesktopCapture DesktopCapture { get { TryInitialize(); return _desktopCapture ?? throw new NullReferenceException(); } }
        public static WindowsMediaControls MediaControls { get { TryInitialize(); return _mediaControls ?? throw new NullReferenceException(); } }
        public static GlobalHooks GlobalHooks { get { TryInitialize(); return _globalHooks ?? throw new NullReferenceException(); } }
        public static ToastMessageSender ToastMessageSender { get { TryInitialize(); return _toastMessageSender ?? throw new NullReferenceException(); } }

        public static bool TryInitialize(bool disableWinFeatures = false){
            if(_hasBeenInitialized) return false;
            _hasBeenInitialized = true;

            _globalHooks = new GlobalHooks();
            _globalHooks.RegisterHooks();

            if (disableWinFeatures) return true;

            try
            {
                _desktopCapture = new DesktopCapture();
                _mediaControls = new WindowsMediaControls();
                _toastMessageSender = new ToastMessageSender();
            }
            catch (Exception) { return false; }

            return true;
        }

        public static void Uninitialize(){
            if(!_hasBeenInitialized) return;

            _globalHooks?.Dispose();
        }
    }
}