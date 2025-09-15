using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using FenUISharp.Logging;
using FenUISharp.Mathematics;
using FenUISharp.Native;
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

        internal static List<FWindow> activeInstances { get; private set; } = new();

        public static bool HasBeenInitialized { get; private set; } = false;
        public static string CrashHandlerPath { get; private set; } = "";

        [ThreadStatic]
        private static bool _isMainThread;
        public static bool IsMainThread { get => _isMainThread; }

        public static void Init(string[]? flags = null)
        {
            if (HasBeenInitialized) return;
            HasBeenInitialized = true;

            // Mark this thread as main
            _isMainThread = true;

            // Create array if null
            flags = flags ?? new string[0];

            // Route console to capture
            ConsoleCapture.StartCapture();

            AllowDebugOutput(false);

            if (!flags.Contains("disable_crashhandler"))
            {
                // Extract crash handler
                FLogger.Log<FenUI>("Extracting crash handler...");

                CrashHandlerPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                    ?? throw new InvalidOperationException("Destination path null"), "fenUICrashHandler.exe");
                var crashHandlerDllPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                    ?? throw new InvalidOperationException("Destination path null"), "fenUICrashHandler.dll");
                var crashHandlerRuntimeConfigPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                    ?? throw new InvalidOperationException("Destination path null"), "fenUICrashHandler.runtimeconfig.json");

                // This will fail if the crash handler is still running. However never replacing it is not a solution, so just silently let it fail
                try
                {
                    ContentExtractor.ExtractToFile(
                        "fenUICrashHandler.runtimeconfig.json",
                        destinationPath: crashHandlerRuntimeConfigPath,
                        overwrite: true
                    );

                    ContentExtractor.ExtractToFile(
                        "fenUICrashHandler.dll",
                        destinationPath: crashHandlerDllPath,
                        overwrite: true
                    );

                    ContentExtractor.ExtractToFile(
                        "fenUICrashHandler.exe",
                        destinationPath: CrashHandlerPath,
                        overwrite: true
                    );
                }
                catch (Exception e) { FLogger.Error($"Error while extracting crash-handler: {e.Message}, {e.StackTrace}"); }
            }

            AppDomain.CurrentDomain.UnhandledException += UnhandledException;
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "fenUICrashlogs");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            Resources.LoadDefault();
            WindowFeatures.TryInitialize(flags.Contains("disable_winfeatures")); // Initialize all window features

            // Make sure that Windows isn't handling things it shouldn't handle
            Win32APIs.SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT.PER_MONITOR_AWARE_V2);
        }

        public static void AllowDebugOutput(bool allow)
        {
            Type[] forbiddenTypes = new Type[] {
                typeof(FenUI),
                typeof(FWindow),
                typeof(FWindowCallbacks),
                typeof(FWindowLoop),
                typeof(FWindowProcedure),
                typeof(FWindowProperties),
                typeof(FWindowShape),
                typeof(FWindowSurface),
                typeof(DirectCompositionContext),
                typeof(SkiaDirectCompositionContext)
            };

            foreach (Type t in forbiddenTypes)
            {
                if (allow)
                    FLogger.ForbiddenTypes.Remove(t);
                else
                    FLogger.ForbiddenTypes.Add(t);
            }
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

            activeInstances.ForEach(x => x.Dispose());
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
            Win32APIs.SetCurrentProcessExplicitAppUserModelID(appModelId);
        }

        // TODO: Switch to using debug mode enabled with args
        internal static bool debugEnabled { get; private set; } = false;

        /// <summary>
        /// Enables several debug features like displaying object bounds with F7 and area cache with F8
        /// </summary>
        public static void EnableDebugFunctions()
        {
            if (!HasBeenInitialized) throw new Exception("FenUI has to be initialized first.");
            if (debugEnabled) return;
            debugEnabled = true;

            AllowDebugOutput(true);

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
            FNativeWindow window = new FNativeWindow("Demo", "demoClass", size: new Vector2(900, 800));

            window.Properties.UseSystemDarkMode = true;
            // window.WindowThemeManager.SetTheme(Resources.GetTheme("default-light"));

            window.Properties.AllowResize = true;
            window.Properties.AllowMinimize = true;
            window.Properties.AllowMaximize = true;
            window.Properties.UseMica = true;
            // window.Properties.MicaBackdropType = MicaBackdropType.TransientWindow;
            // window.DebugDisplayAreaCache = true;
            // window.DebugDisplayBounds = true;

            string iconPath = Resources.ExtractResourceToTempFile<FenUI>($"{FenUI.ResourceLibName}.icons.fenui.ico");
            window.Properties.SetWindowIcon(iconPath);

            window.WithView(new DemoViewPane());

            window.Properties.IsWindowVisible = true;
            window.BeginWindowLoop();
        }
    }
}