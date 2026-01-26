using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.FixedExpenses.Contracts;
using Valt.App.Modules.Budget.FixedExpenses.DTOs;

namespace Valt.App.Modules.Budget.FixedExpenses.Queries.GetFixedExpenses;

internal sealed class GetFixedExpensesHandler : IQueryHandler<GetFixedExpensesQuery, IReadOnlyList<FixedExpenseDTO>>
{
    private readonly IFixedExpenseQueries _fixedExpenseQueries;

    public GetFixedExpensesHandler(IFixedExpenseQueries fixedExpenseQueries)
    {
        _fixedExpenseQueries = fixedExpenseQueries;
    }

    public async Task<IReadOnlyList<FixedExpenseDTO>> HandleAsync(GetFixedExpensesQuery query, CancellationToken ct = default)
    {
        var result = await _fixedExpenseQueries.GetFixedExpensesAsync();
        return result.ToList();
    }
}
