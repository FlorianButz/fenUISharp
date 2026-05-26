using fenUI.Utils;

namespace FenUISharp.WinFeatures {
    
    public static class WindowFeatures {

        private static bool _hasBeenInitialized;

        private static WindowsMediaControls? _mediaControls;
        private static ToastMessageSender? _toastMessageSender;

        public static WindowsMediaControls MediaControls { get { TryInitialize(); return _mediaControls ?? throw new NullReferenceException(); } }
        public static ToastMessageSender ToastMessageSender { get { TryInitialize(); return _toastMessageSender ?? throw new NullReferenceException(); } }

        public static bool TryInitialize(bool disableWinFeatures = false){
            if(_hasBeenInitialized) return false;
            _hasBeenInitialized = true;

            if (disableWinFeatures) return true;

            try
            {
                _mediaControls = new WindowsMediaControls();
                _toastMessageSender = new ToastMessageSender();
            }
            catch (Exception) { return false; }

            return true;
        }

        public static void Uninitialize(){
            if(!_hasBeenInitialized) return;

            /* Dispose */
        }
    }
}