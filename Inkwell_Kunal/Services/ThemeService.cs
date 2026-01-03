using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Inkwell_Kunal.Services;

public class ThemeService
{
    private readonly IJSRuntime _js;

    public string CurrentTheme { get; private set; } = "light";
    public string CurrentAccent { get; private set; } = string.Empty;

    public ThemeService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task InitializeAsync()
    {
        try
        {
            var theme = await _js.InvokeAsync<string>("theme.getTheme");
            if (!string.IsNullOrEmpty(theme)) CurrentTheme = theme;
            var accent = await _js.InvokeAsync<string>("theme.getAccent");
            if (!string.IsNullOrEmpty(accent)) CurrentAccent = accent;
            await _js.InvokeVoidAsync("theme.apply", CurrentTheme, CurrentAccent);
        }
        catch { }
    }

    public async Task SetThemeAsync(string theme)
    {
        CurrentTheme = theme;
        await _js.InvokeVoidAsync("theme.setTheme", theme);
    }

    public async Task SetAccentAsync(string hex)
    {
        CurrentAccent = hex;
        await _js.InvokeVoidAsync("theme.setAccent", hex);
    }
}
