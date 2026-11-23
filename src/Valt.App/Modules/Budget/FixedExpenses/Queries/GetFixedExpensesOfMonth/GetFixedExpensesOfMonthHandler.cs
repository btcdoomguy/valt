using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.FixedExpenses.Contracts;

namespace Valt.App.Modules.Budget.FixedExpenses.Queries.GetFixedExpensesOfMonth;

internal sealed class GetFixedExpensesOfMonthHandler(IFixedExpenseQueries fixedExpenseQueries)
    : IQueryHandler<GetFixedExpensesOfMonthQuery, IReadOnlyList<FixedExpenseEntryDTO>>
{
    public Task<IReadOnlyList<FixedExpenseEntryDTO>> HandleAsync(
        GetFixedExpensesOfMonthQuery query,
        CancellationToken ct = default)
    {
        return fixedExpenseQueries.GetFixedExpensesOfMonthAsync(query.MonthYear);
    }
}
