using Valt.App.Kernel.Queries;
using Valt.App.Modules.Assets.DTOs;

namespace Valt.App.Modules.Assets.Queries.GetLoanStateTimeline;

/// <summary>
/// Query to retrieve the full chronological snapshot timeline of a BTC loan.
/// </summary>
public record GetLoanStateTimelineQuery : IQuery<IReadOnlyList<LoanStateSnapshotDTO>>
{
    /// <summary>
    /// The asset ID of the BTC loan.
    /// </summary>
    public required string AssetId { get; init; }
}
