using System.Diagnostics;
using System.Text;
using FenUISharp;
using FenUISharp.Behavior;
using FenUISharp.Objects;
using FenUISharp.Objects.Buttons;
using FenUISharp.Objects.Text;
using FenUISharp.Objects.Text.Model;
using SkiaSharp;

namespace FenUISharpCrashHandler
{
    public class Program
    {
        internal static string stacktrace = "";
        internal static string logLocation = "";

        [STAThread]
        public static void Main(string[] args)
        {
            if (args.Length >= 2)
            {
                stacktrace = Encoding.UTF8.GetString(Convert.FromBase64String(args[0]));
                logLocation = args[1];
            }
            // else { Environment.Exit(0); return; }

            FenUI.Init(new string[] { "disable_crashhandler", "disable_winfeatures" });
            FenUI.EnableDebugFunctions();
            FenUI.SetupAppModel("fenUISharp.crashhandler");

            FNativeWindow nativeWindow = new FNativeWindow("FenUI Crash Handler", "fenUICrashHandler", size: new(500, 300));
            nativeWindow.Shape.MinSize = new(500, 300);
            nativeWindow.Shape.MaxSize = new(1000, 300);
            nativeWindow.Properties.AllowMaximize = false;

            string iconPath = Resources.ExtractResourceToTempFile<FenUI>($"{FenUI.ResourceLibName}.icons.fenui-logo-error.ico");
            nativeWindow.Properties.SetWindowIcon(iconPath);

            nativeWindow.Properties.UseSystemDarkMode = true;
            nativeWindow.Properties.UseMica = true;
            nativeWindow.Properties.MicaBackdropType = FenUISharp.Native.MicaBackdropType.TransientWindow;
            nativeWindow.WindowThemeManager.SetTheme(Resources.GetTheme("default-dark"));
            nativeWindow.WithView(new CrashHandlerTempView());
            nativeWindow.RequestFocus();

            nativeWindow.Properties.IsWindowVisible = true;
            nativeWindow.BeginWindowLoop();
        }
    }

    internal class CrashHandlerTempView : View
    {
        public override List<UIObject> Create()
        {
            List<UIObject> returnList = new();

            var image = new FImage(() => Resources.GetImage("fenui-logo-error"), position: () => new(-165, -40), size: () => new(80, 80));
            returnList.Add(image);

            var desc = new FText(TextModelFactory.CreateBasic("This process ran into an issue and must be restarted.",
                textSize: 22,
                bold: true,
                align: new() { HorizontalAlign = FenUISharp.Objects.Text.Layout.TextAlign.AlignType.Start, VerticalAlign = FenUISharp.Objects.Text.Layout.TextAlign.AlignType.Middle }
            ), () => new(35, 0), size: () => new(310, 95));
            desc.DisableWhenOutOfParentBounds = false;
            desc.Layout.Alignment.SetStaticState(new(1f, 0.5f));
            desc.Layout.AlignmentAnchor.SetStaticState(new(0f, 0.5f));
            desc.SetParent(image);
            returnList.Add(desc);

            var bottom = new FPanel(size: () => new(0, 40));
            bottom.CornerRadius.SetStaticState(0);
            bottom.Layout.Alignment.SetStaticState(new(0.5f, 1f));
            bottom.Layout.AlignmentAnchor.SetStaticState(new(0.5f, 1f));
            bottom.Layout.StretchHorizontal.SetStaticState(true);
            bottom.ImageEffects.SelfOpacity.SetStaticState(0.4f);
            returnList.Add(bottom);

            var prog = new FProgressBar(height: 7.5f);
            prog.Layout.StretchHorizontal.SetStaticState(true);
            prog.Layout.MarginHorizontal.SetStaticState(25);
            prog.SetParent(bottom);
            returnList.Add(prog);

            var text = new FText(TextModelFactory.CreateBasic("Generating crash-log",
                textSize: 16
            ), () => new(0, -45));
            text.Layout.StretchHorizontal.SetStaticState(true);
            text.DisableWhenOutOfParentBounds = false;
            text.SetParent(prog);
            returnList.Add(text);

            // Make sure to add a delay so the crashlog can be written to disk by the other process
            FContext.GetCurrentDispatcher().InvokeLater(() => FContext.GetCurrentWindow().WithView(new CrashHandlerView()), 0.75f);

            return returnList;
        }
    }

    internal class CrashHandlerView : View
    {
        public override List<UIObject> Create()
        {
            List<UIObject> returnList = new();

            var text = new FText(TextModelFactory.CreateBasic("This process ran into an exception.",
                textSize: 18,
                bold: true,
                align: new()
                {
                    HorizontalAlign = FenUISharp.Objects.Text.Layout.TextAlign.AlignType.Middle
                }));
            text.Layout.Alignment.SetStaticState(new(0, 0));
            text.Layout.AlignmentAnchor.SetStaticState(new(0, 0));
            text.Layout.AbsoluteMarginHorizontal.SetStaticState(new(10, 10));
            text.Layout.StretchHorizontal.SetStaticState(true);

            returnList.Add(text);

            var panel = new FPanel(size: () => new(0, 150), position: () => new(0, -10));
            panel.CornerRadius.SetStaticState(5);
            panel.Layout.AbsoluteMarginHorizontal.SetStaticState(new(25, 25));
            panel.Layout.StretchHorizontal.SetStaticState(true);
            returnList.Add(panel);

            var layout = new StackContentComponent(panel, StackContentComponent.ContentStackType.Vertical, StackContentComponent.ContentStackBehavior.Scroll);
            layout.Pad.SetStaticState(7.5f);
            layout.FullUpdateLayout();

            var exception = new FText(TextModelFactory.CreateBasic(Program.stacktrace,
                textSize: 10,
                align: new()
                {
                    VerticalAlign = FenUISharp.Objects.Text.Layout.TextAlign.AlignType.Start,
                    HorizontalAlign = FenUISharp.Objects.Text.Layout.TextAlign.AlignType.Start
                }), size: () => new(0, 75));
            exception.Transform.Size.SetResponsiveState(() => new(0, exception.LayoutModel.GetBoundingRect(exception.Model, SKRect.Create(panel.Shape.LocalBounds.Width, 1000), 1).Height));
            exception.Layout.AbsoluteMarginHorizontal.SetStaticState(new(15, 15));
            exception.Layout.StretchHorizontal.SetStaticState(true);
            exception.SetParent(panel);

            returnList.Add(exception);

            var bottom = new FPanel(size: () => new(0, 40));
            bottom.CornerRadius.SetStaticState(0);
            bottom.Layout.Alignment.SetStaticState(new(0.5f, 1f));
            bottom.Layout.AlignmentAnchor.SetStaticState(new(0.5f, 1f));
            bottom.Layout.StretchHorizontal.SetStaticState(true);
            bottom.ImageEffects.Opacity.SetStaticState(0.4f);
            returnList.Add(bottom);

            var endBtn = new FSimpleButton(new FText(TextModelFactory.CreateBasic("Quit")), position: () => new(-10, -10), onClick: () => Environment.Exit(0));
            endBtn.Layout.Alignment.SetStaticState(new(1, 1));
            endBtn.Layout.AlignmentAnchor.SetStaticState(new(1, 1));
            returnList.Add(endBtn);

            var cpyBtn = new FSimpleButton(new FText(TextModelFactory.CreateBasic("Open Crashlog")), position: () => new(-60, -10), onClick: () => OpenCrashFolder());
            cpyBtn.Layout.Alignment.SetStaticState(new(1, 1));
            cpyBtn.Layout.AlignmentAnchor.SetStaticState(new(1, 1));
            returnList.Add(cpyBtn);

            return returnList;
        }

        void OpenCrashFolder()
            => Process.Start("notepad.exe", Program.logLocation);
    }
}