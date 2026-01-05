using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Infra.Modules.Budget.Accounts;

namespace Valt.Infra.Modules.Reports.ExpensesByCategory;

public interface IExpensesByCategoryReport
{
    Task<ExpensesByCategoryData> GetAsync(DateOnly baseDate, DateOnlyRange displayRange, FiatCurrency currency, Filter filter, IReportDataProvider provider);

    public record Filter(IEnumerable<AccountId> AccountIds, IEnumerable<CategoryId> CategoryIds);
}