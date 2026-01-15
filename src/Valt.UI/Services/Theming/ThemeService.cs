using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using Valt.UI.Services.LocalStorage;
using Valt.UI.Services.Theming.Themes;

namespace Valt.UI.Services.Theming;

/// <summary>
/// Service for managing application themes.
/// Dynamically loads theme resource dictionaries.
/// Always uses ThemeVariant.Dark to ensure Fluent theme uses dark mode styling.
/// </summary>
public class ThemeService : IThemeService
{
    private static readonly List<ThemeDefinition> Themes =
    [
        new ThemeDefinition("Default", "Default", () => DefaultTheme.Create()),
        new ThemeDefinition("Ocean", "Ocean", () => OceanTheme.Create()),
        new ThemeDefinition("MidnightGalaxy", "Midnight Galaxy", () => MidnightGalaxyTheme.Create()),
        new ThemeDefinition("GoldenHour", "Golden Hour", () => GoldenHourTheme.Create()),
        new ThemeDefinition("ArcticFrost", "Arctic Frost", () => ArcticFrostTheme.Create()),
        new ThemeDefinition("ForestCanopy", "Forest Canopy", () => ForestCanopyTheme.Create())
    ];

    private readonly ILocalStorageService _localStorageService;
    private string _currentTheme = "Default";
    private ResourceDictionary? _currentThemeResources;

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
            // Fall back to default theme
            theme = Themes.First();
        }

        var app = Application.Current;
        if (app == null)
        {
            return;
        }

        // Always use Dark theme variant to ensure Fluent theme uses dark mode styling
        app.RequestedThemeVariant = ThemeVariant.Dark;

        // Remove previous theme resources if loaded
        if (_currentThemeResources != null)
        {
            app.Resources.MergedDictionaries.Remove(_currentThemeResources);
            _currentThemeResources = null;
        }

        // Create and add new theme resources
        try
        {
            var themeResources = theme.CreateResources();
            app.Resources.MergedDictionaries.Insert(0, themeResources);
            _currentThemeResources = themeResources;
        }
        catch (Exception)
        {
            // If theme fails to load, try default theme
            if (theme.Name != "Default")
            {
                var defaultTheme = Themes.First();
                var themeResources = defaultTheme.CreateResources();
                app.Resources.MergedDictionaries.Insert(0, themeResources);
                _currentThemeResources = themeResources;
                _currentTheme = defaultTheme.Name;
                return;
            }
        }

        _currentTheme = theme.Name;
    }

    public void SaveTheme(string themeName)
    {
        _localStorageService.ChangeThemeAsync(themeName);
    }
}
