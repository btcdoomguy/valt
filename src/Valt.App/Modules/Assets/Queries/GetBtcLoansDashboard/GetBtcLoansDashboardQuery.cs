using Valt.App.Kernel.Queries;
using Valt.App.Modules.Assets.DTOs;

namespace Valt.App.Modules.Assets.Queries.GetBtcLoansDashboard;

public record GetBtcLoansDashboardQuery : IQuery<BtcLoansDashboardDTO>
{
    public required string MainCurrencyCode { get; init; }
    public required decimal? BtcPriceUsd { get; init; }
    public required IReadOnlyDictionary<string, decimal>? FiatRates { get; init; }
    public required long TotalBtcStackSats { get; init; }
}
