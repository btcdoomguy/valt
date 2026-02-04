using Valt.App.Kernel.Commands;

namespace Valt.App.Modules.Assets.Commands.UpdateAssetPrice;

/// <summary>
/// Command to update the current price of an asset.
/// </summary>
public record UpdateAssetPriceCommand : ICommand
{
    /// <summary>
    /// The asset ID.
    /// </summary>
    public required string AssetId { get; init; }

    /// <summary>
    /// New current price.
    /// </summary>
    public required decimal NewPrice { get; init; }
}
