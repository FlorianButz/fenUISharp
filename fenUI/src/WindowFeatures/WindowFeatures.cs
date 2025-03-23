namespace FenUISharp {
    
    public static class WindowFeatures {

        private static bool _hasBeenInitialized;

        public static DesktopCapture DesktopCapture { get; set; }
        public static WindowsMediaControls MediaControls { get; set; }
        public static GlobalHooks GlobalHooks { get; set; }

        public static bool TryInitialize(){
            if(_hasBeenInitialized) return false;
            _hasBeenInitialized = true;

            GlobalHooks = new GlobalHooks();
            GlobalHooks.RegisterHooks();

            DesktopCapture = new DesktopCapture();
            MediaControls = new WindowsMediaControls();

            return true;
        }

    }
}