using Microsoft.Extensions.Logging;
using Valt.Core.Common;

namespace Valt.Infra.Modules.Reports.ExpensesByCategory;

internal class ExpensesByCategoryReport : IExpensesByCategoryReport
{
    private readonly ILogger<ExpensesByCategoryReport> _logger;

    public ExpensesByCategoryReport(ILogger<ExpensesByCategoryReport> logger)
    {
        _logger = logger;
    }

    public Task<ExpensesByCategoryData> GetAsync(
        DateOnly baseDate,
        DateOnlyRange displayRange,
        FiatCurrency currency,
        IExpensesByCategoryReport.Filter filter,
        IReportDataProvider provider)
    {
        try
        {
            var items = CategoryBreakdownCalculator.Calculate(
                currency,
                provider,
                displayRange.Start,
                displayRange.End,
                filter.AccountIds,
                filter.CategoryIds,
                valuePredicate: value => value < 0,
                valueMultiplier: -1m,
                itemFactory: (categoryId, icon, categoryName, fiatTotal) => new ExpensesByCategoryData.Item
                {
                    CategoryId = categoryId,
                    Icon = icon,
                    CategoryName = categoryName,
                    FiatTotal = fiatTotal
                },
                sortKeySelector: item => item.CategoryName);

            return Task.FromResult(new ExpensesByCategoryData
            {
                MainCurrency = currency,
                Items = items
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while calculating expenses by category");
            throw;
        }
    }
}
