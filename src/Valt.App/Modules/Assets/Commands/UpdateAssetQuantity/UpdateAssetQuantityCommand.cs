using Valt.App.Kernel.Commands;

namespace Valt.App.Modules.Assets.Commands.UpdateAssetQuantity;

/// <summary>
/// Command to update the quantity of a basic asset.
/// </summary>
public record UpdateAssetQuantityCommand : ICommand
{
    /// <summary>
    /// The asset ID.
    /// </summary>
    public required string AssetId { get; init; }

    /// <summary>
    /// New quantity.
    /// </summary>
    public required decimal NewQuantity { get; init; }
}
