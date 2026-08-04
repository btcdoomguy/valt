using Valt.App.Kernel.Queries;
using Valt.App.Modules.SpendingAnalytics.Contracts;
using Valt.App.Modules.SpendingAnalytics.DTOs;

namespace Valt.App.Modules.SpendingAnalytics.Queries;

internal sealed class GetBurnRateHandler : IQueryHandler<GetBurnRateQuery, BurnRateDataDto>
{
    private readonly IBurnRateQueries _burnRateQueries;

    public GetBurnRateHandler(IBurnRateQueries burnRateQueries)
    {
        _burnRateQueries = burnRateQueries;
    }

    public Task<BurnRateDataDto> HandleAsync(GetBurnRateQuery query, CancellationToken ct = default)
    {
        return _burnRateQueries.GetBurnRateAsync(query);
    }
}
