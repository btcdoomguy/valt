using Valt.App.Kernel.Queries;
using Valt.App.Modules.BtcDenominatedMetrics.Contracts;
using Valt.App.Modules.BtcDenominatedMetrics.DTOs;

namespace Valt.App.Modules.BtcDenominatedMetrics.Queries;

internal sealed class GetBtcDenominatedMetricsHandler : IQueryHandler<GetBtcDenominatedMetricsQuery, BtcDenominatedMetricsDataDto>
{
    private readonly IBtcDenominatedMetricsQueries _btcDenominatedMetricsQueries;

    public GetBtcDenominatedMetricsHandler(IBtcDenominatedMetricsQueries btcDenominatedMetricsQueries)
    {
        _btcDenominatedMetricsQueries = btcDenominatedMetricsQueries;
    }

    public Task<BtcDenominatedMetricsDataDto> HandleAsync(GetBtcDenominatedMetricsQuery query, CancellationToken ct = default)
    {
        return _btcDenominatedMetricsQueries.GetBtcDenominatedMetricsAsync(query);
    }
}
