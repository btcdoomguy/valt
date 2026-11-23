using Microsoft.Extensions.Logging;
using Valt.Core.Common;

namespace Valt.Infra.Modules.Reports.IncomeByCategory;

internal class IncomeByCategoryReport : IIncomeByCategoryReport
{
    private readonly ILogger<IncomeByCategoryReport> _logger;

    public IncomeByCategoryReport(ILogger<IncomeByCategoryReport> logger)
    {
        _logger = logger;
    }

    public Task<IncomeByCategoryData> GetAsync(
        DateOnly baseDate,
        DateOnlyRange displayRange,
        FiatCurrency currency,
        IIncomeByCategoryReport.Filter filter,
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
                valuePredicate: value => value > 0,
                valueMultiplier: 1m,
                itemFactory: (categoryId, icon, categoryName, fiatTotal) => new IncomeByCategoryData.Item
                {
                    CategoryId = categoryId,
                    Icon = icon,
                    CategoryName = categoryName,
                    FiatTotal = fiatTotal
                },
                sortKeySelector: item => item.CategoryName);

            return Task.FromResult(new IncomeByCategoryData
            {
                MainCurrency = currency,
                Items = items
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while calculating income by category");
            throw;
        }
    }
}
