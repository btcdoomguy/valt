using Valt.Core.Modules.Budget.FixedExpenses;
using Valt.Infra.Modules.Budget.FixedExpenses.Queries.DTOs;

namespace Valt.Infra.Modules.Budget.FixedExpenses.Queries;

public interface IFixedExpenseQueries
{
    Task<FixedExpenseDto?> GetFixedExpenseAsync(FixedExpenseId id);
    Task<IEnumerable<FixedExpenseDto>> GetFixedExpensesAsync();
    Task<FixedExpenseNamesDTO> GetFixedExpenseNamesAsync(FixedExpenseId? current = null);
    Task<FixedExpenseHistoryDto?> GetFixedExpenseHistoryAsync(FixedExpenseId id);
}