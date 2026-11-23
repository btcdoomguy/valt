using Valt.Core.Modules.Budget.FixedExpenses;
using Valt.App.Modules.Budget.FixedExpenses.DTOs;
using Valt.App.Modules.Budget.FixedExpenses.Queries.GetFixedExpensesOfMonth;

namespace Valt.App.Modules.Budget.FixedExpenses.Contracts;

public interface IFixedExpenseQueries
{
    Task<FixedExpenseDTO?> GetFixedExpenseAsync(FixedExpenseId id);
    Task<IEnumerable<FixedExpenseDTO>> GetFixedExpensesAsync();
    Task<FixedExpenseNamesDTO> GetFixedExpenseNamesAsync(FixedExpenseId? current = null);
    Task<FixedExpenseHistoryDTO?> GetFixedExpenseHistoryAsync(FixedExpenseId id);
    Task<IReadOnlyList<FixedExpenseEntryDTO>> GetFixedExpensesOfMonthAsync(DateOnly date);
}
