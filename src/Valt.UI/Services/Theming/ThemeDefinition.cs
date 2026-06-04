using Avalonia.Controls;

namespace Valt.UI.Services.Theming;

/// <summary>
/// Represents a theme definition with its metadata.
/// </summary>
/// <param name="Name">Internal name used for storage and lookup</param>
/// <param name="DisplayName">User-friendly name shown in UI</param>
/// <param name="BaseTheme">Base theme variant: "Dark" or "Light"</param>
/// <param name="Palette">The color palette for this theme</param>
public record ThemeDefinition(string Name, string DisplayName, string BaseTheme, ThemePalette Palette)
{
    public ResourceDictionary CreateResources() => ThemeBuilder.Create(Palette);
}
