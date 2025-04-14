using System.Runtime.InteropServices;
using FenUISharp.WinFeatures;

namespace FenUISharp {
    public class FenUI {

        public static Version FenUIVersion => new(0, 0, 1);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern int SetCurrentProcessExplicitAppUserModelID(string AppID);

        public static bool HasBeenInitialized { get; private set; } = false;

        public static void Init(){
            if(HasBeenInitialized) return;
            HasBeenInitialized = true;

            Resources.LoadDefault();
            WindowFeatures.TryInitialize(); // Initialize all window features
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