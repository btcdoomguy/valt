using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Styling;
using Valt.UI.Services.LocalStorage;

namespace Valt.UI.Services.Theming;

/// <summary>
/// Service for managing application themes.
/// Uses Avalonia's ThemeDictionaries feature - themes are defined in ColorResources.axaml
/// and switching is done by changing RequestedThemeVariant.
/// </summary>
public class ThemeService : IThemeService
{
    private static readonly List<ThemeDefinition> Themes =
    [
        new ThemeDefinition("Dark", "Dark", true),
        new ThemeDefinition("Light", "Light", false)
    ];

    private readonly ILocalStorageService _localStorageService;
    private string _currentTheme = "Dark";

    public ThemeService(ILocalStorageService localStorageService)
    {
        _localStorageService = localStorageService;

        // Load and apply theme from local storage
        var savedTheme = _localStorageService.LoadTheme();
        ApplyTheme(savedTheme);
    }

    public IReadOnlyList<ThemeDefinition> AvailableThemes => Themes;

    public string CurrentTheme => _currentTheme;

    public void ApplyTheme(string themeName)
    {
        var theme = Themes.FirstOrDefault(t => t.Name == themeName);
        if (theme == null)
        {
            return;
        }

        var app = Application.Current;
        if (app == null)
        {
            return;
        }

        // Set the theme variant - Avalonia's ThemeDictionaries will automatically
        // use the correct resources based on this setting
        app.RequestedThemeVariant = theme.IsDark ? ThemeVariant.Dark : ThemeVariant.Light;

        _currentTheme = themeName;
    }

    public void SaveTheme(string themeName)
    {
        _localStorageService.ChangeThemeAsync(themeName);
    }
}
