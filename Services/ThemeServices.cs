namespace Inkwell_Kunal.Services
{
    public class ThemeService
    {
        public bool IsDarkMode { get; private set; }

        public event Action? OnThemeChanged;

        public void ToggleTheme()
        {
            IsDarkMode = !IsDarkMode;
            OnThemeChanged?.Invoke();
        }
    }
}
