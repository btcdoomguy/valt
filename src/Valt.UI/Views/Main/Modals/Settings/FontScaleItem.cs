using System.Collections.Generic;
using Valt.Infra.Settings;
using Valt.UI.Lang;

namespace Valt.UI.Views.Main.Modals.Settings;

/// <summary>
/// Represents a font scale option for display in the settings UI.
/// </summary>
public class FontScaleItem
{
    public FontScale Scale { get; }
    public string DisplayName { get; }

    private FontScaleItem(FontScale scale, string displayName)
    {
        Scale = scale;
        DisplayName = displayName;
    }

    private static readonly List<FontScaleItem> _all =
    [
        new FontScaleItem(FontScale.Small, language.Settings_FontScale_Small),
        new FontScaleItem(FontScale.Medium, language.Settings_FontScale_Medium),
        new FontScaleItem(FontScale.Large, language.Settings_FontScale_Large)
    ];

    public static IReadOnlyList<FontScaleItem> All => _all;
}
