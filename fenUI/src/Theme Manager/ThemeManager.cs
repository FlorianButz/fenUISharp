using SkiaSharp;

namespace FenUISharp.Themes
{
    public class ThemeManager
    {
        private Theme _currentTheme;
        public Theme CurrentTheme
        {
            get => _currentTheme;
            private set
            {
                _currentTheme = value;
                ThemeChanged?.Invoke();
            }
        }

        public event Action? ThemeChanged;

        public ThemeManager(Theme initialTheme)
        {
            _currentTheme = initialTheme;
        }

        public void SetTheme(Theme newTheme)
        {
            if (newTheme == CurrentTheme) return;
            CurrentTheme = newTheme;
        }
        
        public void ForceUpdate()
            => ThemeChanged?.Invoke();
    }
}