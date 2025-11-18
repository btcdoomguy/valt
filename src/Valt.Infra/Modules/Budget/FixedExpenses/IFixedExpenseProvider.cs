namespace Valt.Infra.Modules.Budget.FixedExpenses;

public interface IFixedExpenseProvider
{
    Task<IEnumerable<FixedExpenseProviderEntry>> GetFixedExpensesOfMonthAsync(DateOnly date);
}