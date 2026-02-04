using Valt.App.Kernel.Commands;

namespace Valt.App.Modules.Assets.Commands.SetAssetIncludeInNetWorth;

/// <summary>
/// Command to set whether an asset is included in net worth calculation.
/// </summary>
public record SetAssetIncludeInNetWorthCommand : ICommand
{
    /// <summary>
    /// The asset ID.
    /// </summary>
    public required string AssetId { get; init; }

    /// <summary>
    /// Whether the asset should be included in net worth.
    /// </summary>
    public required bool IncludeInNetWorth { get; init; }
}
