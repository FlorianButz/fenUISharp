using System.Runtime.InteropServices;
using FenUISharp.Components;
using FenUISharp.Components.Buttons;
using FenUISharp.Components.Text;
using FenUISharp.Components.Text.Layout;
using FenUISharp.Components.Text.Model;
using FenUISharp.Mathematics;
using FenUISharp.WinFeatures;
using SkiaSharp;

namespace FenUISharp
{
    public class FenUI
    {
        public static string ResourceLibName => "fenUI";
        public static Version FenUIVersion => new(0, 0, 1, 2);

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
            NativeWindow window = new NativeWindow("Test 1", "testClass", Window.RenderContextType.DirectX, windowSize: new Vector2(900, 800));

            window.SystemDarkMode = true;
            // window.WindowThemeManager.SetTheme(Resources.GetTheme("default-light"));

            window.AllowResizing = true;
            window.CanMaximize = true;
            window.CanMinimize = true;
            // window.DebugDisplayAreaCache = true;
            // window.DebugDisplayBounds = true;

            string iconPath = Resources.ExtractResourceToTempFile<FenUI>($"{FenUI.ResourceLibName}.icons.TrayIcon.ico");
            window.SetWindowIcon(iconPath);

            FPanel panel = new(window, Vector2.Zero, Vector2.Zero, 10, window.WindowThemeManager.GetColor(t => t.Background));
            panel.Transform.StretchHorizontal = true;
            panel.Transform.StretchVertical = true;
            panel.Transform.MarginVertical += 25;

            panel.BorderSize = 1;
            panel.BorderColor = window.WindowThemeManager.GetColor(t => t.Surface);

            // Setup panel layout

            StackContentComponent layout = new(panel, StackContentComponent.ContentStackType.Vertical, StackContentComponent.ContentStackBehavior.Scroll);

            FInputField f = new(window, Vector2.Zero);
            f.Transform.SetParent(panel.Transform);

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
                FText title = new(window, Vector2.Zero, new Vector2(200, 75), TextModelFactory.CreateBasic("Text", 20, bold: true));
                title.Transform.SetParent(panel.Transform);

                FPanel subpanel = new(window, Vector2.Zero, new(500, 500), 10, window.WindowThemeManager.GetColor(t => t.Background));
                subpanel.Transform.SetParent(panel.Transform);

                subpanel.BorderSize = 1;
                subpanel.BorderColor = window.WindowThemeManager.GetColor(t => t.Surface);

                StackContentComponent sublayout = new(subpanel, StackContentComponent.ContentStackType.Vertical, StackContentComponent.ContentStackBehavior.SizeToFitAll);
                sublayout.Gap = 20;

                FText text1 = new(window, Vector2.Zero, new(0, 250), TextModelFactory.CreateBasic("Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est"));
                text1.Transform.StretchHorizontal = true;
                text1.Transform.Size = new(0, text1.Layout.GetBoundingRect(text1.Model, SKRect.Create(subpanel.Transform.Size.x, 250)).Height);
                text1.Transform.SetParent(subpanel.Transform);

                FText text2 = new(window, Vector2.Zero, new(0, 150), TextModelFactory.CreateTest("Multiple text styles in one component!"));
                text2.Transform.StretchHorizontal = true;
                text2.Transform.Size = new(0, text2.Layout.GetBoundingRect(text2.Model, SKRect.Create(subpanel.Transform.Size.x, 250)).Height);
                text2.Transform.SetParent(subpanel.Transform);

                FText text3 = new(window, Vector2.Zero, new(0, 150), TextModelFactory.CreateBasic("Dynamic glyph layout processors"));
                text3.Layout = new WiggleCharsLayoutProcessor(text3, new WrapLayout(text3));
                text3.Transform.StretchHorizontal = true;
                text3.Transform.Size = new(0, text3.Layout.GetBoundingRect(text3.Model, SKRect.Create(subpanel.Transform.Size.x, 250)).Height);
                text3.Transform.SetParent(subpanel.Transform);

                FText text4 = new(window, Vector2.Zero, new(0, 150), TextModelFactory.CreateBasic("Text change animation (Text 1)"));
                text4.Layout = new BlurLayoutProcessor(text4, new WrapLayout(text4));
                text4.Transform.StretchHorizontal = true;
                text4.Transform.Size = new(0, text4.Layout.GetBoundingRect(text4.Model, SKRect.Create(subpanel.Transform.Size.x, 250)).Height);
                text4.Transform.SetParent(subpanel.Transform);

                float val = 0;
                int lastText = 0;
                window.OnUpdate += () =>
                {
                    val = ((float)Math.Sin(window.Time) + 1) / 2 + 1.5f;
                    
                    int text = (int)val;
                    if(lastText != text)
                        text4.Model = TextModelFactory.CreateBasic($"Text change animation (Text {text})");
                    lastText = text;
                };
            }

            {
                FText title = new(window, Vector2.Zero, new Vector2(200, 75), TextModelFactory.CreateBasic("Sliders", 20, bold: true));
                title.Transform.SetParent(panel.Transform);

                FPanel subpanel = new(window, Vector2.Zero, new(500, 500), 10, window.WindowThemeManager.GetColor(t => t.Background));
                subpanel.Transform.SetParent(panel.Transform);

                subpanel.BorderSize = 1;
                subpanel.BorderColor = window.WindowThemeManager.GetColor(t => t.Surface);

                StackContentComponent sublayout = new(subpanel, StackContentComponent.ContentStackType.Horizontal, StackContentComponent.ContentStackBehavior.SizeToFitAll);

                FSlider slider1 = new(window, Vector2.Zero, 200);
                slider1.Transform.SetParent(subpanel.Transform);
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
                FText title = new(window, Vector2.Zero, new Vector2(200, 75), TextModelFactory.CreateBasic("Toggles", 20, bold: true));
                title.Transform.SetParent(panel.Transform);

                FPanel subpanel = new(window, Vector2.Zero, new(500, 500), 10, window.WindowThemeManager.GetColor(t => t.Background));
                subpanel.Transform.SetParent(panel.Transform);

                subpanel.BorderSize = 1;
                subpanel.BorderColor = window.WindowThemeManager.GetColor(t => t.Surface);

                StackContentComponent sublayout = new(subpanel, StackContentComponent.ContentStackType.Horizontal, StackContentComponent.ContentStackBehavior.SizeToFitAll);

                FRoundToggle toggle1 = new(window, Vector2.Zero);
                toggle1.Transform.SetParent(subpanel.Transform);
            }

            {
                FText title = new(window, Vector2.Zero, new Vector2(200, 75), TextModelFactory.CreateBasic("Button Groups", 20, bold: true));
                title.Transform.SetParent(panel.Transform);

                {
                    FPanel subpanel = new(window, Vector2.Zero, new(500, 500), 10, window.WindowThemeManager.GetColor(t => t.Background));
                    subpanel.Transform.SetParent(panel.Transform);

                    subpanel.BorderSize = 1;
                    subpanel.BorderColor = window.WindowThemeManager.GetColor(t => t.Surface);

                    StackContentComponent sublayout = new(subpanel, StackContentComponent.ContentStackType.Horizontal, StackContentComponent.ContentStackBehavior.SizeToFitAll);

                    FRoundToggle toggle1 = new(window, Vector2.Zero);
                    toggle1.Transform.SetParent(subpanel.Transform);

                    FRoundToggle toggle2 = new(window, Vector2.Zero);
                    toggle2.Transform.SetParent(subpanel.Transform);

                    FRoundToggle toggle3 = new(window, Vector2.Zero);
                    toggle3.Transform.SetParent(subpanel.Transform);

                    FButtonGroup btnGroup = new();
                    btnGroup.Add(toggle1);
                    btnGroup.Add(toggle2);
                    btnGroup.Add(toggle3);

                    btnGroup.AllowMultiSelect = false;
                    btnGroup.AlwaysMustSelectOne = true;
                }

                {
                    FPanel subpanel = new(window, Vector2.Zero, new(500, 500), 10, window.WindowThemeManager.GetColor(t => t.Background));
                    subpanel.Transform.SetParent(panel.Transform);

                    subpanel.BorderSize = 1;
                    subpanel.BorderColor = window.WindowThemeManager.GetColor(t => t.Surface);

                    StackContentComponent sublayout = new(subpanel, StackContentComponent.ContentStackType.Horizontal, StackContentComponent.ContentStackBehavior.SizeToFitAll);

                    FRoundToggle toggle1 = new(window, Vector2.Zero);
                    toggle1.Transform.SetParent(subpanel.Transform);

                    FRoundToggle toggle2 = new(window, Vector2.Zero);
                    toggle2.Transform.SetParent(subpanel.Transform);

                    FRoundToggle toggle3 = new(window, Vector2.Zero);
                    toggle3.Transform.SetParent(subpanel.Transform);

                    FButtonGroup btnGroup = new();
                    btnGroup.Add(toggle1);
                    btnGroup.Add(toggle2);
                    btnGroup.Add(toggle3);

                    btnGroup.AllowMultiSelect = true;
                    btnGroup.AlwaysMustSelectOne = true;
                }
                
                {
                    FPanel subpanel = new(window, Vector2.Zero, new(500, 500), 10, window.WindowThemeManager.GetColor(t => t.Background));
                    subpanel.Transform.SetParent(panel.Transform);

                    subpanel.BorderSize = 1;
                    subpanel.BorderColor = window.WindowThemeManager.GetColor(t => t.Surface);

                    StackContentComponent sublayout = new(subpanel, StackContentComponent.ContentStackType.Horizontal, StackContentComponent.ContentStackBehavior.SizeToFitAll);

                    FRoundToggle toggle1 = new(window, Vector2.Zero);
                    toggle1.Transform.SetParent(subpanel.Transform);

                    FRoundToggle toggle2 = new(window, Vector2.Zero);
                    toggle2.Transform.SetParent(subpanel.Transform);

                    FRoundToggle toggle3 = new(window, Vector2.Zero);
                    toggle3.Transform.SetParent(subpanel.Transform);

                    FButtonGroup btnGroup = new();
                    btnGroup.Add(toggle1);
                    btnGroup.Add(toggle2);
                    btnGroup.Add(toggle3);

                    btnGroup.AllowMultiSelect = false;
                    btnGroup.AlwaysMustSelectOne = false;
                }
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

                FProgressBar prog5 = new(window, Vector2.Zero, 600) { LeftToRight = false };
                prog5.Transform.SetParent(subpanel.Transform);

                FProgressBar prog2 = new(window, Vector2.Zero, 600) { Indeterminate = true };
                prog2.Transform.SetParent(subpanel.Transform);

                FHorizontalSeparator separator = new(window, subpanel.Transform);

                // Radial

                FPanel subpanel2 = new(window, Vector2.Zero, new(500, 500), 10, new(SKColors.Transparent));
                subpanel2.Transform.SetParent(subpanel.Transform);

                StackContentComponent sublayout2 = new(subpanel2, StackContentComponent.ContentStackType.Horizontal, StackContentComponent.ContentStackBehavior.SizeToFitAll);
                sublayout2.Gap = 75;

                FRadialProgressBar prog3 = new(window, Vector2.Zero, new(100, 100));
                prog3.Transform.SetParent(subpanel2.Transform);

                FRadialProgressBar prog4 = new(window, Vector2.Zero, new(100, 100)) { Indeterminate = true };
                prog4.Transform.SetParent(subpanel2.Transform);

                // window.OnUpdate += () =>
                // {
                //     var prog1Value = ((float)Math.Sin(window.Time / 5) + 1) / 2;
                //     prog1.Value = prog1Value;
                //     prog3.Value = 1 - prog1Value;
                //     prog5.Value = prog1Value;
                // };
            }

            FSimpleButton switchTheme = new(window, new(0, 40), "Switch Theme", color: window.WindowThemeManager.GetColor(t => t.Secondary), textColor: window.WindowThemeManager.GetColor(t => t.OnSecondary));
            switchTheme.Transform.Alignment = new(0.5f, 0);
            switchTheme.OnClick += () => { window.SystemDarkMode = !window.SystemDarkMode; window.WindowThemeManager.SetTheme(window.SystemDarkMode ? Resources.GetTheme("default-dark") : Resources.GetTheme("default-light")); };

            // Begin

            window.SetWindowVisibility(true);
            window.BeginWindowLoop();
        }
    }
}