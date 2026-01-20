using System;
using Avalonia.Controls;

namespace Valt.UI.Services.Theming;

/// <summary>
/// Represents a theme definition with its metadata.
/// </summary>
/// <param name="Name">Internal name used for storage and lookup</param>
/// <param name="DisplayName">User-friendly name shown in UI</param>
/// <param name="BaseTheme">Base theme variant: "Dark" or "Light"</param>
/// <param name="CreateResources">Factory function that creates the theme's ResourceDictionary</param>
public record ThemeDefinition(string Name, string DisplayName, string BaseTheme, Func<ResourceDictionary> CreateResources);
