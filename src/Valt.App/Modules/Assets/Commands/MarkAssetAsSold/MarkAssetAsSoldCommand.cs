using Valt.App.Kernel.Commands;

namespace Valt.App.Modules.Assets.Commands.MarkAssetAsSold;

/// <summary>
/// Command to mark an asset as sold.
/// </summary>
public record MarkAssetAsSoldCommand : ICommand
{
    /// <summary>
    /// The asset ID.
    /// </summary>
    public required string AssetId { get; init; }

    /// <summary>
    /// The date the asset was sold. Defaults to today when null.
    /// </summary>
    public DateOnly? DateSold { get; init; }
}
