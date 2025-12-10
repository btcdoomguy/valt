using Valt.Core.Common;

namespace Valt.Infra.Modules.Reports.ExpensesByCategory;

public interface IExpensesByCategoryReport
{
    Task<ExpensesByCategoryData> GetAsync(DateOnly baseDate, DateOnlyRange displayRange, FiatCurrency currency);
}