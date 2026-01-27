using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.FixedExpenses.Contracts;
using Valt.App.Modules.Budget.FixedExpenses.DTOs;
using Valt.Core.Modules.Budget.FixedExpenses;

namespace Valt.App.Modules.Budget.FixedExpenses.Queries.GetFixedExpenseHistory;

internal sealed class GetFixedExpenseHistoryHandler : IQueryHandler<GetFixedExpenseHistoryQuery, FixedExpenseHistoryDTO?>
{
    private readonly IFixedExpenseQueries _fixedExpenseQueries;

    public GetFixedExpenseHistoryHandler(IFixedExpenseQueries fixedExpenseQueries)
    {
        _fixedExpenseQueries = fixedExpenseQueries;
    }

    public Task<FixedExpenseHistoryDTO?> HandleAsync(GetFixedExpenseHistoryQuery query, CancellationToken ct = default)
    {
        return _fixedExpenseQueries.GetFixedExpenseHistoryAsync(new FixedExpenseId(query.FixedExpenseId));
    }
}
