using Valt.App.Modules.SpendingAnalytics.Contracts;
using Valt.App.Modules.SpendingAnalytics.DTOs;
using Valt.App.Modules.SpendingAnalytics.Queries;
using Valt.Core.Common;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Infra.Modules.Reports;
using Valt.Infra.Modules.Reports.MonthlyTotals;
using Valt.Infra.Modules.Reports.Statistics;
using Valt.Infra.Settings;

namespace Valt.Infra.Modules.SpendingAnalytics.Queries;

public class BurnRateQueries : IBurnRateQueries
{
    private readonly IReportDataProviderFactory _reportDataProviderFactory;
    private readonly IMonthlyTotalsReport _monthlyTotalsReport;
    private readonly IStatisticsReport _statisticsReport;
    private readonly IClock _clock;
    private readonly CurrencySettings _currencySettings;

    public BurnRateQueries(
        IReportDataProviderFactory reportDataProviderFactory,
        IMonthlyTotalsReport monthlyTotalsReport,
        IStatisticsReport statisticsReport,
        IClock clock,
        CurrencySettings currencySettings)
    {
        _reportDataProviderFactory = reportDataProviderFactory;
        _monthlyTotalsReport = monthlyTotalsReport;
        _statisticsReport = statisticsReport;
        _clock = clock;
        _currencySettings = currencySettings;
    }

    public async Task<BurnRateDataDto> GetBurnRateAsync(GetBurnRateQuery query)
    {
        var currency = FiatCurrency.GetFromCode(_currencySettings.MainFiatCurrency);
        var provider = await _reportDataProviderFactory.CreateAsync();
        var today = _clock.GetCurrentLocalDate();

        var emptyDto = new BurnRateDataDto
        {
            HasData = false,
            SpentSoFar = 0m,
            AvgDailySpend = 0m,
            ProjectedMonthEnd = null,
            MedianMonthlyExpenses = 0m,
            VsMedianPercent = null,
            DayOfMonth = today.Day,
            PrimaryCurrency = currency.Code
        };

        if (provider.AllTransactions.Count == 0)
        {
            return emptyDto;
        }

        try
        {
            var mtdRange = new DateOnlyRange(new DateOnly(today.Year, today.Month, 1), today);
            var mtd = await _monthlyTotalsReport.GetAsync(today, mtdRange, currency, provider);
            var spentMtd = Math.Abs(mtd.Total.AllExpensesInFiat);

            if (spentMtd == 0)
            {
                return emptyDto;
            }

            var avgDaily = spentMtd / today.Day;
            var daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);
            decimal? projected = today.Day >= 5 ? avgDaily * daysInMonth : null;

            var stats = await _statisticsReport.GetAsync(currency, query.CurrentWealthInFiat, provider);
            var median = stats.MedianMonthlyExpenses.Value;

            decimal? vsMedian = projected.HasValue && median > 0
                ? Math.Round((projected.Value - median) / median * 100, 2)
                : null;

            return new BurnRateDataDto
            {
                HasData = true,
                SpentSoFar = spentMtd,
                AvgDailySpend = avgDaily,
                ProjectedMonthEnd = projected,
                MedianMonthlyExpenses = median,
                VsMedianPercent = vsMedian,
                DayOfMonth = today.Day,
                PrimaryCurrency = currency.Code
            };
        }
        catch (ApplicationException)
        {
            return emptyDto;
        }
    }
}
