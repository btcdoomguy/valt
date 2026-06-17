using Valt.App.Kernel.Commands;

namespace Valt.App.Modules.Assets.Commands.DeleteLoanStateUpdate;

/// <summary>
/// Command to delete a state snapshot from a BTC-backed loan by its effective date.
/// </summary>
public record DeleteLoanStateUpdateCommand : ICommand
{
    /// <summary>
    /// The asset ID.
    /// </summary>
    public required string AssetId { get; init; }

    /// <summary>
    /// The effective date of the snapshot to delete.
    /// </summary>
    public required DateOnly EffectiveDate { get; init; }
}
