using FenUISharp.Objects;

namespace FenUISharp
{
    public static class FContext
    {
        public static float Time { get => (float)(CurrentWindow?.Time ?? 0); } 
        public static float DeltaTime { get => (float)(CurrentWindow?.DeltaTime ?? 0); } 

        [ThreadStatic]
        private static Window? CurrentWindow;

        [ThreadStatic]
        private static Dispatcher? CurrentDispatcher;

        [ThreadStatic]
        private static ModelViewPane? RootViewPane;

        public static Window GetCurrentWindow() => CurrentWindow ?? throw new Exception("GetCurrentWindow() cannot be called in an invalid FenUI context");
        public static Dispatcher GetCurrentDispatcher() => CurrentDispatcher ?? throw new Exception("GetCurrentDispatcher() cannot be called in an invalid FenUI context");
        public static ModelViewPane? GetRootViewPane() => RootViewPane;
        public static KeyboardInputManager GetKeyboardInputManager() => CurrentWindow?.WindowKeyboardInput ?? throw new Exception("GetKeyboardInputManager() cannot be called in an invalid FenUI context");

        internal static void WithWindow(Window window)
        {
            CurrentWindow = window;
            CurrentDispatcher = window.Dispatcher;
        }

        internal static void WithRootViewPane(ModelViewPane? view)
        {
            RootViewPane = view;
        }

        public static bool IsValidContext() => CurrentWindow != null && CurrentDispatcher != null;
    }
}