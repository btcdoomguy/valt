using Valt.App.Kernel.Commands;

namespace Valt.App.Modules.Assets.Commands.AddLoanStateUpdate;

/// <summary>
/// Command to add a new state snapshot to a BTC-backed loan.
/// </summary>
public record AddLoanStateUpdateCommand : ICommand
{
    /// <summary>
    /// The asset ID.
    /// </summary>
    public required string AssetId { get; init; }

    /// <summary>
    /// The effective date of the snapshot. Must be strictly after the latest existing snapshot.
    /// </summary>
    public required DateOnly EffectiveDate { get; init; }

    /// <summary>
    /// The borrowed principal still owed at the time of the snapshot.
    /// </summary>
    public required decimal TotalBorrowed { get; init; }

    /// <summary>
    /// Interest charged up to the snapshot's effective date.
    /// </summary>
    public required decimal InterestAccruedUntilDate { get; init; }

    /// <summary>
    /// BTC collateral amount in satoshis.
    /// </summary>
    public required long CollateralSats { get; init; }

    /// <summary>
    /// Annual percentage rate at the time of the snapshot (e.g., 0.12 for 12%).
    /// </summary>
    public required decimal Apr { get; init; }

    /// <summary>
    /// Fees paid for the loan.
    /// </summary>
    public required decimal Fees { get; init; }

    /// <summary>
    /// Optional note describing the snapshot.
    /// </summary>
    public required string? Note { get; init; }
}
