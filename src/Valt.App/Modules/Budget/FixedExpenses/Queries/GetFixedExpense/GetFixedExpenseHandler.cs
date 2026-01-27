using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.FixedExpenses.Contracts;
using Valt.App.Modules.Budget.FixedExpenses.DTOs;
using Valt.Core.Modules.Budget.FixedExpenses;

namespace Valt.App.Modules.Budget.FixedExpenses.Queries.GetFixedExpense;

internal sealed class GetFixedExpenseHandler : IQueryHandler<GetFixedExpenseQuery, FixedExpenseDTO?>
{
    private readonly IFixedExpenseQueries _fixedExpenseQueries;

    public GetFixedExpenseHandler(IFixedExpenseQueries fixedExpenseQueries)
    {
        _fixedExpenseQueries = fixedExpenseQueries;
    }

    public Task<FixedExpenseDTO?> HandleAsync(GetFixedExpenseQuery query, CancellationToken ct = default)
    {
        return _fixedExpenseQueries.GetFixedExpenseAsync(new FixedExpenseId(query.FixedExpenseId));
    }
}
