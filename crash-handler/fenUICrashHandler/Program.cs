using FenUISharp;
using FenUISharp.Objects;
using FenUISharp.Objects.Buttons;
using FenUISharp.Objects.Text;
using FenUISharp.Objects.Text.Model;

namespace FenUISharpCrashHandler
{
    public class Program
    {
        internal static string stacktrace = "";
        internal static string logLocation = "";

        [STAThread]
        public static void Main(string[] args)
        {
            FenUI.Init();
            FenUI.SetupAppModel("fenUISharp.crashhandler");

            NativeWindow nativeWindow = new NativeWindow("FenUI Crash Handler", "fenUICrashHandler", Window.RenderContextType.Software, new(400, 200));
            nativeWindow.SystemDarkMode = true;
            nativeWindow.WindowThemeManager.SetTheme(Resources.GetTheme("default-dark"));

            string iconPath = Resources.ExtractResourceToTempFile<FenUI>($"{FenUI.ResourceLibName}.icons.fenui.ico");
            nativeWindow.SetWindowIcon(iconPath);

            nativeWindow.WithView(new CrashHandlerTempView());

            nativeWindow.SetWindowVisibility(true);
            nativeWindow.BeginWindowLoop();
        }
    }

    internal class CrashHandlerTempView : View
    {
        public override List<UIObject> Create()
        {
            List<UIObject> returnList = new();

            var image = new FImage(() => Resources.GetImage("fenui-logo-error"), position: () => new(-116, -30), size: () => new(50, 50));
            returnList.Add(image);

            var desc =  new FText(TextModelFactory.CreateBasic("This process ran into an issue and must be restarted.",
                textSize: 16,
                bold: true,
                align: new() { HorizontalAlign = FenUISharp.Objects.Text.Layout.TextAlign.AlignType.Start }
            ), () => new(37, -30 / 2), size: () => new(220, 75));
            returnList.Add(desc);

            var prog = new FProgressBar(position: () => new(0, 30));
            prog.Layout.StretchHorizontal.SetStaticState(true);
            prog.Layout.MarginHorizontal.SetStaticState(25);

            returnList.Add(prog);

            var text = new FText(TextModelFactory.CreateBasic("Generating crash-log",
                textSize: 14
            ), () => new(0, 55f));
            text.Layout.StretchHorizontal.SetStaticState(true);

            returnList.Add(text);

            FContext.GetCurrentWindow().WithView(new CrashHandlerView());

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

            var exception = new FText(TextModelFactory.CreateBasic("System.IndexOutOfRangeException: 'Index was outside the bounds of the array.' ",
                textSize: 14,
                align: new()
                {
                    VerticalAlign = FenUISharp.Objects.Text.Layout.TextAlign.AlignType.Start,
                    HorizontalAlign = FenUISharp.Objects.Text.Layout.TextAlign.AlignType.Start
                }), size: () => new(0, 75), position: () => new(0, -10));
            exception.Layout.AbsoluteMarginHorizontal.SetStaticState(new(25, 25));
            exception.Layout.StretchHorizontal.SetStaticState(true);

            returnList.Add(exception);

            var endBtn = new FSimpleButton(new FText(TextModelFactory.CreateBasic("Quit")), position: () => new(-10, -10), onClick: () => Environment.Exit(0));
            endBtn.Layout.Alignment.SetStaticState(new(1, 1));
            endBtn.Layout.AlignmentAnchor.SetStaticState(new(1, 1));

            var cpyBtn = new FSimpleButton(new FText(TextModelFactory.CreateBasic("Copy Crashlog")), position: () => new(-60, -10));
            cpyBtn.Layout.Alignment.SetStaticState(new(1, 1));
            cpyBtn.Layout.AlignmentAnchor.SetStaticState(new(1, 1));

            return returnList;
        }
    }
}