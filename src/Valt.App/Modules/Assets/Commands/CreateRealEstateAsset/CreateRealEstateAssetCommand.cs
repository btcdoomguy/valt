using Valt.App.Kernel.Commands;

namespace Valt.App.Modules.Assets.Commands.CreateRealEstateAsset;

/// <summary>
/// Command to create a real estate asset.
/// </summary>
public record CreateRealEstateAssetCommand : ICommand<CreateRealEstateAssetResult>
{
    /// <summary>
    /// Property name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Currency code (e.g., USD, BRL).
    /// </summary>
    public required string CurrencyCode { get; init; }

    /// <summary>
    /// Current market value.
    /// </summary>
    public required decimal CurrentValue { get; init; }

    /// <summary>
    /// Property address (optional).
    /// </summary>
    public string? Address { get; init; }

    /// <summary>
    /// Monthly rental income (optional).
    /// </summary>
    public decimal? MonthlyRentalIncome { get; init; }

    /// <summary>
    /// Include in net worth calculation.
    /// </summary>
    public bool IncludeInNetWorth { get; init; } = true;

    /// <summary>
    /// Visible in list.
    /// </summary>
    public bool Visible { get; init; } = true;

    /// <summary>
    /// Icon identifier (optional).
    /// </summary>
    public string? Icon { get; init; }
}

public record CreateRealEstateAssetResult(string AssetId);
