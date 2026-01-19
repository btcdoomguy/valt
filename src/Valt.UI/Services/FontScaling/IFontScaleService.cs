using Valt.Infra.Settings;

namespace Valt.UI.Services.FontScaling;

/// <summary>
/// Service for managing application font scaling.
/// </summary>
public interface IFontScaleService
{
    /// <summary>
    /// Gets the current font scale setting.
    /// </summary>
    FontScale CurrentScale { get; }

    /// <summary>
    /// Applies the specified font scale to the application.
    /// </summary>
    /// <param name="scale">The font scale to apply.</param>
    void ApplyScale(FontScale scale);

    /// <summary>
    /// Saves the specified font scale to local storage.
    /// </summary>
    /// <param name="scale">The font scale to save.</param>
    void SaveScale(FontScale scale);
}
