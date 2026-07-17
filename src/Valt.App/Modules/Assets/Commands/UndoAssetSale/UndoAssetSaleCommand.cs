using Valt.App.Kernel.Commands;

namespace Valt.App.Modules.Assets.Commands.UndoAssetSale;

/// <summary>
/// Command to undo an asset sale and restore it to the active view.
/// </summary>
public record UndoAssetSaleCommand : ICommand
{
    /// <summary>
    /// The asset ID.
    /// </summary>
    public required string AssetId { get; init; }
}
