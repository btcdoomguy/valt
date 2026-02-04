using Valt.App.Kernel.Commands;

namespace Valt.App.Modules.Assets.Commands.DeleteAsset;

/// <summary>
/// Command to delete an asset.
/// </summary>
public record DeleteAssetCommand : ICommand
{
    /// <summary>
    /// The asset ID to delete.
    /// </summary>
    public required string AssetId { get; init; }
}
