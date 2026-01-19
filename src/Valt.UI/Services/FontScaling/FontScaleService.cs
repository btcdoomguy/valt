using Avalonia;
using Avalonia.Controls;
using Valt.Infra.Settings;

namespace Valt.UI.Services.FontScaling;

/// <summary>
/// Service for managing application font scaling.
/// Dynamically updates font size resources based on the selected scale.
/// </summary>
public class FontScaleService : IFontScaleService
{
    private readonly DisplaySettings _displaySettings;

    // Base font sizes (Medium scale = 1.0x)
    private const double BaseFontSizeXSmall = 11;
    private const double BaseFontSizeSmall = 12;
    private const double BaseFontSizeNormal = 13;
    private const double BaseFontSizeMedium = 14;
    private const double BaseFontSizeLarge = 16;
    private const double BaseFontSizeXLarge = 18;
    private const double BaseFontSizeXXLarge = 20;
    private const double BaseFontSizeIcon = 22;
    private const double BaseFontSizeIconLarge = 28;

    public FontScaleService(DisplaySettings displaySettings)
    {
        _displaySettings = displaySettings;
        ApplyScale(_displaySettings.FontScale);
    }

    public FontScale CurrentScale => _displaySettings.FontScale;

    public void ApplyScale(FontScale scale)
    {
        var app = Application.Current;
        if (app == null)
            return;

        var multiplier = GetScaleMultiplier(scale);

        // Update font size resources
        app.Resources["FontSizeXSmall"] = BaseFontSizeXSmall * multiplier;
        app.Resources["FontSizeSmall"] = BaseFontSizeSmall * multiplier;
        app.Resources["FontSizeNormal"] = BaseFontSizeNormal * multiplier;
        app.Resources["FontSizeMedium"] = BaseFontSizeMedium * multiplier;
        app.Resources["FontSizeLarge"] = BaseFontSizeLarge * multiplier;
        app.Resources["FontSizeXLarge"] = BaseFontSizeXLarge * multiplier;
        app.Resources["FontSizeXXLarge"] = BaseFontSizeXXLarge * multiplier;
        app.Resources["FontSizeIcon"] = BaseFontSizeIcon * multiplier;
        app.Resources["FontSizeIconLarge"] = BaseFontSizeIconLarge * multiplier;
    }

    private static double GetScaleMultiplier(FontScale scale) => scale switch
    {
        FontScale.Small => 0.85,
        FontScale.Medium => 1.0,
        FontScale.Large => 1.15,
        _ => 1.0
    };
}
