using Valt.App.Kernel.Queries;
using Valt.App.Modules.Assets.DTOs;

namespace Valt.App.Modules.Assets.Queries.GetAsset;

/// <summary>
/// Query to get a single asset by its ID.
/// </summary>
public record GetAssetQuery : IQuery<AssetDTO?>
{
    /// <summary>
    /// The asset ID to retrieve.
    /// </summary>
    public required string AssetId { get; init; }
}
