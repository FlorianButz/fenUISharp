using System.Runtime.InteropServices;
using FenUISharp.Components;
using FenUISharp.Components.Text;
using FenUISharp.Components.Text.Model;
using FenUISharp.Mathematics;
using FenUISharp.WinFeatures;

namespace FenUISharp {
    public class FenUI
    {

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

        public static void Shutdown()
        {
            if (!HasBeenInitialized) return;

            WindowFeatures.Uninitialize();
        }

        public static void SetupAppModel(string appModelId)
        {
            if (!HasBeenInitialized) throw new Exception("FenUI has to be initialized first.");
            SetCurrentProcessExplicitAppUserModelID(appModelId);
        }

        public static void Demo()
        {
            NativeWindow window = new NativeWindow("Test 1", "testClass", Window.RenderContextType.DirectX, windowSize: new Vector2(1200, 800));

            window.SystemDarkMode = true;
            // window.WindowThemeManager.SetTheme(Resources.GetTheme("default-light"));

            window.AllowResizing = true;
            window.CanMaximize = true;
            window.CanMinimize = true;

            window.SetWindowIcon("icons/TrayIcon.ico");

            FPanel panel = new(window, Vector2.Zero, Vector2.Zero, 10, window.WindowThemeManager.GetColor(t => t.Background));
            panel.Transform.StretchHorizontal = true;
            panel.Transform.StretchVertical = true;

            panel.BorderSize = 1;
            panel.BorderColor = window.WindowThemeManager.GetColor(t => t.Surface);

            // Setup panel layout

            StackContentComponent layout = new(panel, StackContentComponent.ContentStackType.Vertical, StackContentComponent.ContentStackBehavior.Scroll);

            // Setup all components

            {
                FText title = new(window, Vector2.Zero, new Vector2(200, 75), TextModelFactory.CreateBasic("Button Types", 20, bold: true));
                title.Transform.SetParent(panel.Transform);

                FPanel subpanel = new(window, Vector2.Zero, new(500, 500), 10, window.WindowThemeManager.GetColor(t => t.Background));
                subpanel.Transform.SetParent(panel.Transform);

                subpanel.BorderSize = 1;
                subpanel.BorderColor = window.WindowThemeManager.GetColor(t => t.Surface);

                StackContentComponent sublayout = new(subpanel, StackContentComponent.ContentStackType.Horizontal, StackContentComponent.ContentStackBehavior.SizeToFitAll);

                FSimpleButton primary = new(window, Vector2.Zero, "Primary", color: window.WindowThemeManager.GetColor(t => t.Primary), textColor: window.WindowThemeManager.GetColor(t => t.OnPrimary));
                primary.Transform.SetParent(subpanel.Transform);
                FSimpleButton secondary = new(window, Vector2.Zero, "Secondary", color: window.WindowThemeManager.GetColor(t => t.Secondary), textColor: window.WindowThemeManager.GetColor(t => t.OnSecondary));
                secondary.Transform.SetParent(subpanel.Transform);
            }

            // Begin

            window.SetWindowVisibility(true);
            window.BeginWindowLoop();
        }
    }
}