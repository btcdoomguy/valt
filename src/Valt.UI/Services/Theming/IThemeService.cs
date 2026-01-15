using System.Collections.Generic;

namespace Valt.UI.Services.Theming;

/// <summary>
/// Service for managing application themes.
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Gets the list of available themes.
    /// </summary>
    IReadOnlyList<ThemeDefinition> AvailableThemes { get; }

    /// <summary>
    /// Gets the currently active theme name.
    /// </summary>
    string CurrentTheme { get; }

    /// <summary>
    /// Applies the specified theme to the application.
    /// </summary>
    /// <param name="themeName">The name of the theme to apply.</param>
    void ApplyTheme(string themeName);

    /// <summary>
    /// Saves the specified theme to local storage.
    /// </summary>
    /// <param name="themeName">The name of the theme to save.</param>
    void SaveTheme(string themeName);
}
