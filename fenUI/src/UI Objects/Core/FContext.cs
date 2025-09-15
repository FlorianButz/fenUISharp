using FenUISharp.Objects;
using FenUISharp.Themes;

namespace FenUISharp
{
    public static class FContext
    {
        public static float Time { get => (float)(CurrentWindow?.Time.Time ?? 0); } 
        public static float DeltaTime { get => (float)(CurrentWindow?.Time.DeltaTime ?? 0); }
        public static bool IsDisposingWindow { get => isDisposingWindow; }

        [ThreadStatic]
        internal static bool isDisposingWindow;

        [ThreadStatic]
        private static FWindow? CurrentWindow;

        [ThreadStatic]
        private static Dispatcher? CurrentDispatcher;

        [ThreadStatic]
        private static ModelViewPane? RootViewPane;

        public static FWindow GetCurrentWindow() => CurrentWindow ?? throw new Exception("GetCurrentWindow() cannot be called in an invalid FenUI context");
        public static Dispatcher GetCurrentDispatcher() => CurrentDispatcher ?? throw new Exception("GetCurrentDispatcher() cannot be called in an invalid FenUI context");
        public static ModelViewPane? GetRootViewPane() => RootViewPane;
        public static KeyboardInputManager GetKeyboardInputManager() => CurrentWindow?.WindowKeyboardInput ?? throw new Exception("GetKeyboardInputManager() cannot be called in an invalid FenUI context");
        public static ThemeManager GetCurrentThemeManager() => CurrentWindow?.WindowThemeManager ?? throw new Exception("GetCurrentWindow() cannot be called in an invalid FenUI context");

        internal static void WithWindow(FWindow window)
        {
            CurrentWindow = window;
            CurrentDispatcher = window.LogicDispatcher;
        }

        internal static void WithRootViewPane(ModelViewPane? view)
        {
            RootViewPane = view;
        }

        public static bool IsValidContext() => CurrentWindow != null && CurrentDispatcher != null && !CurrentWindow._disposingOrDisposed;
    }
}