using Valt.App.Kernel.Queries;
using Valt.App.Modules.SpendingAnalytics.Contracts;
using Valt.App.Modules.SpendingAnalytics.DTOs;

namespace Valt.App.Modules.SpendingAnalytics.Queries;

internal sealed class GetFixedVsVariableHandler : IQueryHandler<GetFixedVsVariableQuery, FixedVsVariableDataDto>
{
    private readonly IFixedVsVariableQueries _fixedVsVariableQueries;

    public GetFixedVsVariableHandler(IFixedVsVariableQueries fixedVsVariableQueries)
    {
        _fixedVsVariableQueries = fixedVsVariableQueries;
    }

    public Task<FixedVsVariableDataDto> HandleAsync(GetFixedVsVariableQuery query, CancellationToken ct = default)
    {
        return _fixedVsVariableQueries.GetFixedVsVariableAsync(query);
    }
}
