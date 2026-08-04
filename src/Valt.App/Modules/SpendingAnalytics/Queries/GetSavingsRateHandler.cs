using Valt.App.Kernel.Queries;
using Valt.App.Modules.SpendingAnalytics.Contracts;
using Valt.App.Modules.SpendingAnalytics.DTOs;

namespace Valt.App.Modules.SpendingAnalytics.Queries;

internal sealed class GetSavingsRateHandler : IQueryHandler<GetSavingsRateQuery, SavingsRateDataDto>
{
    private readonly ISavingsRateQueries _savingsRateQueries;

    public GetSavingsRateHandler(ISavingsRateQueries savingsRateQueries)
    {
        _savingsRateQueries = savingsRateQueries;
    }

    public Task<SavingsRateDataDto> HandleAsync(GetSavingsRateQuery query, CancellationToken ct = default)
    {
        return _savingsRateQueries.GetSavingsRateAsync(query);
    }
}
