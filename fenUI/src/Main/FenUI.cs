using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using FenUISharp.Mathematics;
using FenUISharp.Objects;
using FenUISharp.States;
using FenUISharp.WinFeatures;
using SkiaSharp;

namespace FenUISharp
{
    public class FenUI
    {
        public static string ResourceLibName => "fenUI";
        public static Version FenUIVersion => new(0, 0, 2);

        internal static List<Window> activeInstances { get; private set; } = new();


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
        public static string CrashHandlerPath { get; private set; } = "";

        public static void Init(string[]? flags = null)
        {
            if (HasBeenInitialized) return;
            HasBeenInitialized = true;

            // Create array if null
            flags = flags ?? new string[0];

            // Route console to capture
            ConsoleCapture.StartCapture();

            if (!flags.Contains("disable_crashhandler"))
            {
                // Extract crash handler

                ContentExtractor.ExtractToFile(
                    "glfw3.dll",
                    destinationPath: Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "glfw3.dll"),
                    overwrite: false
                );
                ContentExtractor.ExtractToFile(
                    "libSkiaSharp.dll",
                    destinationPath: Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "libSkiaSharp.dll"),
                    overwrite: false
                );
                CrashHandlerPath = ContentExtractor.ExtractToFile(
                    "fenUICrashHandler.exe",
                    destinationPath: Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "fenUICrashHandler.exe"),
                    overwrite: false // Setting this to true could break the application and stops it from running if the crashhandler is still active
                );
            }

            AppDomain.CurrentDomain.UnhandledException += UnhandledException;
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "fenUICrashlogs");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            Resources.LoadDefault();
            WindowFeatures.TryInitialize(flags.Contains("disable_winfeatures")); // Initialize all window features

            // Make sure that Windows isn't handling things it shouldn't handle
            SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT.PER_MONITOR_AWARE_V2);
        }

        private static void UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "fenUICrashlogs");
            var crashlogPath = Path.Combine(path, $"{appModelId}-{DateTime.Now.ToLongTimeString().Replace(':', '-')}-crash-log.txt");

            var ex = (Exception)e.ExceptionObject;

            Console.WriteLine();
            Console.WriteLine("======== UNHANDLED EXCEPTION ========");
            Console.WriteLine(ex.ToString());

            Console.WriteLine($"-> Inner: {ex.InnerException} -> Msg: {ex.Message} -> Src: {ex.Source}");

            ConsoleCapture.SaveErrorLogToFile(crashlogPath);

            // Running crash handler
            if (!string.IsNullOrEmpty(CrashHandlerPath))
            {
                var p = new Process();
                p.StartInfo.FileName = CrashHandlerPath;
                p.StartInfo.UseShellExecute = false;

                // Encoding to prevent newlines or other special chars messing up everything
                var traceB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(ex.ToString()));
                p.StartInfo.Arguments = $"\"{traceB64}\" \"{crashlogPath}\"";

                p.Start();
            }

            activeInstances.ForEach(x => x.DisposeAndDestroyWindow());
            WindowFeatures.Uninitialize();
        }

        public static void Shutdown()
        {
            if (!HasBeenInitialized) return;

            WindowFeatures.Uninitialize();
        }

        private static string appModelId = "";

        public static void SetupAppModel(string appModelId)
        {
            if (!HasBeenInitialized) throw new Exception("FenUI has to be initialized first.");
            FenUI.appModelId = appModelId;
            SetCurrentProcessExplicitAppUserModelID(appModelId);
        }

        internal static bool debugEnabled { get; private set; } = false;

        /// <summary>
        /// Enables several debug features like displaying object bounds with F7 and area cache with F8
        /// </summary>
        public static void EnableDebugFunctions()
        {
            if (!HasBeenInitialized) throw new Exception("FenUI has to be initialized first.");
            if (debugEnabled) return;
            debugEnabled = true;

            WindowFeatures.GlobalHooks.OnKeyPressed += (x) =>
            {
                if (x == 0x77)
                { // F8
                    activeInstances.ForEach(x => x.DebugDisplayAreaCache = !x.DebugDisplayAreaCache);
                    activeInstances.ForEach(x => x.FullRedraw());
                }
                else if (x == 0x76)
                { // F7
                    activeInstances.ForEach(x => x.DebugDisplayBounds = !x.DebugDisplayBounds);
                    activeInstances.ForEach(x => x.FullRedraw());
                }
            };
        }

        public static void Demo()
        {
            NativeWindow window = new NativeWindow("Demo", "demoClass", Window.RenderContextType.DirectX, windowSize: new Vector2(900, 800));

            window.SystemDarkMode = true;
            // window.WindowThemeManager.SetTheme(Resources.GetTheme("default-light"));

            window.AllowResizing = true;
            window.CanMaximize = true;
            window.CanMinimize = true;
            // window.DebugDisplayAreaCache = true;
            // window.DebugDisplayBounds = true;

            string iconPath = Resources.ExtractResourceToTempFile<FenUI>($"{FenUI.ResourceLibName}.icons.fenui.ico");
            window.SetWindowIcon(iconPath);

            window.WithView(new DemoViewPane());

            window.SetWindowVisibility(true);
            window.BeginWindowLoop();
        }
    }
}