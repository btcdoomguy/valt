using Valt.App.Kernel.Commands;

namespace Valt.App.Modules.Assets.Commands.SetAssetVisibility;

/// <summary>
/// Command to set the visibility of an asset.
/// </summary>
public record SetAssetVisibilityCommand : ICommand
{
    /// <summary>
    /// The asset ID.
    /// </summary>
    public required string AssetId { get; init; }

    /// <summary>
    /// Whether the asset should be visible.
    /// </summary>
    public required bool Visible { get; init; }
}
