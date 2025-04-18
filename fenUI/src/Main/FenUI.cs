using System.Runtime.InteropServices;
using FenUISharp.WinFeatures;

namespace FenUISharp {
    public class FenUI {

        public static Version FenUIVersion => new(0, 0, 1);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern int SetCurrentProcessExplicitAppUserModelID(string AppID);

        [DllImport("user32.dll")]
        static extern bool SetProcessDpiAwarenessContext(IntPtr dpiContext);

        static class DPI_AWARENESS_CONTEXT
        {
            public static readonly IntPtr UNAWARE = new IntPtr(-1);
            public static readonly IntPtr SYSTEM_AWARE = new IntPtr(-2);
            public static readonly IntPtr PER_MONITOR_AWARE = new IntPtr(-3);
            public static readonly IntPtr PER_MONITOR_AWARE_V2 = new IntPtr(-4);
            public static readonly IntPtr UNAWARE_GDISCALED = new IntPtr(-5);
        }


        public static bool HasBeenInitialized { get; private set; } = false;

        public static void Init()
        {
            if (HasBeenInitialized) return;
            HasBeenInitialized = true;

            Resources.LoadDefault();
            WindowFeatures.TryInitialize(); // Initialize all window features
            
            // Make sure that Windows isn't handling things it shouldn't handle
            SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT.PER_MONITOR_AWARE_V2);
        }

        public static void Shutdown(){
            if(!HasBeenInitialized) return;

            WindowFeatures.Uninitialize();
        }

        public static void SetupAppModel(string appModelId){
            if(!HasBeenInitialized) throw new Exception("FenUI has to be initialized first.");
            SetCurrentProcessExplicitAppUserModelID(appModelId);
        }
    }
}