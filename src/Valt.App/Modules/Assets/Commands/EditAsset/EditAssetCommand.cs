using Valt.App.Kernel.Commands;
using Valt.App.Modules.Assets.DTOs;

namespace Valt.App.Modules.Assets.Commands.EditAsset;

/// <summary>
/// Command to edit an existing asset.
/// </summary>
public record EditAssetCommand : ICommand
{
    /// <summary>
    /// The asset ID to edit.
    /// </summary>
    public required string AssetId { get; init; }

    /// <summary>
    /// New asset name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Asset details (polymorphic based on asset type).
    /// </summary>
    public required AssetDetailsInputDTO Details { get; init; }

    /// <summary>
    /// Icon identifier (optional).
    /// </summary>
    public string? Icon { get; init; }

    /// <summary>
    /// Include in net worth calculation.
    /// </summary>
    public required bool IncludeInNetWorth { get; init; }

    /// <summary>
    /// Visible in list.
    /// </summary>
    public required bool Visible { get; init; }
}
