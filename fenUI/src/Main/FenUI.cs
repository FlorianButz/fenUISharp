using System.Runtime.InteropServices;

namespace FenUISharp {
    public class FenUI {

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern int SetCurrentProcessExplicitAppUserModelID(string AppID);

        public static bool HasBeenInitialized { get; private set; } = false;

        public static void Init(){
            if(HasBeenInitialized) return;
            HasBeenInitialized = true;

            Resources.LoadDefault();
            WindowFeatures.TryInitialize(); // Initialize all window features
        }

        public static void SetupAppModel(string appModelId){
            if(!HasBeenInitialized) throw new Exception("FenUI has to be initialized first.");
            SetCurrentProcessExplicitAppUserModelID(appModelId);
        }
    }
}