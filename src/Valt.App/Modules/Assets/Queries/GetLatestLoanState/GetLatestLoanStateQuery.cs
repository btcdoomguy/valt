using Valt.App.Kernel.Queries;
using Valt.App.Modules.Assets.DTOs;

namespace Valt.App.Modules.Assets.Queries.GetLatestLoanState;

/// <summary>
/// Query to retrieve the latest recorded state of a BTC loan, including asset metadata.
/// </summary>
public record GetLatestLoanStateQuery : IQuery<LoanStateDTO?>
{
    /// <summary>
    /// The asset ID of the BTC loan.
    /// </summary>
    public required string AssetId { get; init; }
}
