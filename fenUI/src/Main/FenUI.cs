using System.Runtime.InteropServices;
using FenUISharp.Components;
using FenUISharp.Components.Text;
using FenUISharp.Components.Text.Model;
using FenUISharp.Mathematics;
using FenUISharp.WinFeatures;
using SkiaSharp;

namespace FenUISharp
{
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
            // window.DebugDisplayAreaCache = true;

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
                FText title = new(window, Vector2.Zero, new Vector2(200, 75), TextModelFactory.CreateBasic("Images", 20, bold: true));
                title.Transform.SetParent(panel.Transform);

                FPanel subpanel = new(window, Vector2.Zero, new(500, 500), 10, window.WindowThemeManager.GetColor(t => t.Background));
                subpanel.Transform.SetParent(panel.Transform);

                subpanel.BorderSize = 1;
                subpanel.BorderColor = window.WindowThemeManager.GetColor(t => t.Surface);

                StackContentComponent sublayout = new(subpanel, StackContentComponent.ContentStackType.Horizontal, StackContentComponent.ContentStackBehavior.SizeToFitAll);

                FImage img1 = new(window, Vector2.Zero, new(75, 75), Resources.GetImage("test-img"), 15);
                img1.ScaleMode = FImage.ImageScaleMode.Stretch;
                img1.Transform.SetParent(subpanel.Transform);

                FImage img3 = new(window, Vector2.Zero, new(133, 75), Resources.GetImage("test-img"), 15);
                img3.ScaleMode = FImage.ImageScaleMode.Contain;
                img3.Transform.SetParent(subpanel.Transform);

                FImage img2 = new(window, Vector2.Zero, new(75, 75), Resources.GetImage("test-img"), 15);
                img2.ScaleMode = FImage.ImageScaleMode.Fit;
                img2.Transform.SetParent(subpanel.Transform);
            }

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


            {
                FText title = new(window, Vector2.Zero, new Vector2(200, 75), TextModelFactory.CreateBasic("Progress Bars", 20, bold: true));
                title.Transform.SetParent(panel.Transform);

                FPanel subpanel = new(window, Vector2.Zero, new(500, 500), 10, window.WindowThemeManager.GetColor(t => t.Background));
                subpanel.Transform.SetParent(panel.Transform);

                subpanel.BorderSize = 1;
                subpanel.BorderColor = window.WindowThemeManager.GetColor(t => t.Surface);

                StackContentComponent sublayout = new(subpanel, StackContentComponent.ContentStackType.Vertical, StackContentComponent.ContentStackBehavior.SizeToFitAll);

                // Normal

                FProgressBar prog1 = new(window, Vector2.Zero, 600);
                prog1.Transform.SetParent(subpanel.Transform);

                FProgressBar prog2 = new(window, Vector2.Zero, 600) { Indeterminate = true };
                prog2.Transform.SetParent(subpanel.Transform);

                // Radial

                FPanel subpanel2 = new(window, Vector2.Zero, new(500, 500), 10, new(SKColors.Transparent));
                subpanel2.Transform.SetParent(subpanel.Transform);

                StackContentComponent sublayout2 = new(subpanel2, StackContentComponent.ContentStackType.Horizontal, StackContentComponent.ContentStackBehavior.SizeToFitAll);
                sublayout2.Gap = 75;

                FRadialProgressBar prog3 = new(window, Vector2.Zero, new(100, 100));
                prog3.Transform.SetParent(subpanel2.Transform);

                FRadialProgressBar prog4 = new(window, Vector2.Zero, new(100, 100)) { Indeterminate = true };
                prog4.Transform.SetParent(subpanel2.Transform);

                float t = 0;
                window.OnUpdate += () =>
                {
                    t++;
                    var prog1Value = ((float)Math.Sin(t / 50) + 1) / 2;
                    prog1.Value = prog1Value;
                    prog3.Value = 1 - prog1Value;
                };

                
                FSimpleButton switchTheme = new(window, new(0, 50), "Switch Theme", color: window.WindowThemeManager.GetColor(t => t.Secondary), textColor: window.WindowThemeManager.GetColor(t => t.OnSecondary));
                switchTheme.Transform.Alignment = new(0.5f, 0);
                switchTheme.OnClick += () => { window.SystemDarkMode = !window.SystemDarkMode; window.WindowThemeManager.SetTheme(window.SystemDarkMode ? Resources.GetTheme("default-dark") : Resources.GetTheme("default-light")); };
            }

            // Begin

            window.SetWindowVisibility(true);
            window.BeginWindowLoop();
        }
    }
}