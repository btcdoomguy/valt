using Valt.Core.Kernel.Abstractions;

namespace Valt.Core.Modules.Budget.FixedExpenses.Contracts;

public interface IFixedExpenseRepository : IRepository
{
    Task<FixedExpense?> GetFixedExpenseByIdAsync(FixedExpenseId fixedExpenseId);
    Task SaveFixedExpenseAsync(FixedExpense fixedExpense);
    Task DeleteFixedExpenseAsync(FixedExpenseId fixedExpenseId);
}