using Valt.Core.Common;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Infra.Modules.Reports.MonthlyTotals;

namespace Valt.Infra.Modules.Reports.Statistics;

internal class StatisticsReport : IStatisticsReport
{
    private readonly IClock _clock;
    private readonly IMonthlyTotalsReport _monthlyTotalsReport;

    public StatisticsReport(IClock clock, IMonthlyTotalsReport monthlyTotalsReport)
    {
        _clock = clock;
        _monthlyTotalsReport = monthlyTotalsReport;
    }

    public async Task<StatisticsData> GetAsync(FiatCurrency currency, decimal currentWealthInFiat, IReportDataProvider provider)
    {
        var medianExpenses = await CalculateMedianExpensesAsync(currency, provider);
        var (coverageMonths, coverageFormatted) = CalculateWealthCoverage(currentWealthInFiat, medianExpenses);

        return new StatisticsData
        {
            MedianMonthlyExpenses = FiatValue.New(medianExpenses),
            Currency = currency,
            WealthCoverageMonths = coverageMonths,
            WealthCoverageFormatted = coverageFormatted
        };
    }

    private async Task<decimal> CalculateMedianExpensesAsync(FiatCurrency currency, IReportDataProvider provider)
    {
        if (provider.AllTransactions.Count == 0)
        {
            return 0m;
        }

        var currentDate = _clock.GetCurrentLocalDate();

        // Get last 12 months range (excluding current month since it's not complete)
        var endOfLastMonth = new DateOnly(currentDate.Year, currentDate.Month, 1).AddDays(-1);
        var startOf12MonthsAgo = endOfLastMonth.AddMonths(-11);
        startOf12MonthsAgo = new DateOnly(startOf12MonthsAgo.Year, startOf12MonthsAgo.Month, 1);

        var displayRange = new DateOnlyRange(startOf12MonthsAgo, endOfLastMonth);

        try
        {
            var monthlyData = await _monthlyTotalsReport.GetAsync(currentDate, displayRange, currency, provider);

            // Get the absolute values of expenses for each month (expenses are stored as negative)
            var monthlyExpenses = monthlyData.Items
                .Select(item => Math.Abs(item.AllExpensesInFiat))
                .Where(expense => expense > 0) // Only include months with actual expenses
                .OrderBy(x => x)
                .ToList();

            if (monthlyExpenses.Count == 0)
            {
                return 0m;
            }

            return CalculateMedian(monthlyExpenses);
        }
        catch (ApplicationException)
        {
            // No transactions found
            return 0m;
        }
    }

    private static decimal CalculateMedian(List<decimal> sortedValues)
    {
        var count = sortedValues.Count;
        if (count == 0)
            return 0m;

        if (count % 2 == 0)
        {
            // Even number of elements - average the two middle values
            var mid1 = sortedValues[count / 2 - 1];
            var mid2 = sortedValues[count / 2];
            return Math.Round((mid1 + mid2) / 2, 2);
        }

        // Odd number of elements - return the middle value
        return Math.Round(sortedValues[count / 2], 2);
    }

    private static (int months, string formatted) CalculateWealthCoverage(decimal currentWealth, decimal medianExpenses)
    {
        if (currentWealth <= 0 || medianExpenses <= 0)
        {
            return (0, "0");
        }

        var coverageMonths = (int)Math.Floor(currentWealth / medianExpenses);

        if (coverageMonths < 12)
        {
            return (coverageMonths, $"{coverageMonths}");
        }

        var years = coverageMonths / 12;
        var remainingMonths = coverageMonths % 12;

        if (remainingMonths == 0)
        {
            return (coverageMonths, $"{years}y");
        }

        return (coverageMonths, $"{years}y {remainingMonths}m");
    }
}
