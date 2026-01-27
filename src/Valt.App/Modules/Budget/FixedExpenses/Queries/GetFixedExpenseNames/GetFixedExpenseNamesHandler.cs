using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.FixedExpenses.Contracts;
using Valt.App.Modules.Budget.FixedExpenses.DTOs;
using Valt.Core.Modules.Budget.FixedExpenses;

namespace Valt.App.Modules.Budget.FixedExpenses.Queries.GetFixedExpenseNames;

internal sealed class GetFixedExpenseNamesHandler : IQueryHandler<GetFixedExpenseNamesQuery, FixedExpenseNamesDTO>
{
    private readonly IFixedExpenseQueries _fixedExpenseQueries;

    public GetFixedExpenseNamesHandler(IFixedExpenseQueries fixedExpenseQueries)
    {
        _fixedExpenseQueries = fixedExpenseQueries;
    }

    public Task<FixedExpenseNamesDTO> HandleAsync(GetFixedExpenseNamesQuery query, CancellationToken ct = default)
    {
        FixedExpenseId? currentId = string.IsNullOrWhiteSpace(query.CurrentFixedExpenseId)
            ? null
            : new FixedExpenseId(query.CurrentFixedExpenseId);

        return _fixedExpenseQueries.GetFixedExpenseNamesAsync(currentId);
    }
}
