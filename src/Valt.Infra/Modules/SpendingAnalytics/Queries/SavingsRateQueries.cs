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

public class SavingsRateQueries : ISavingsRateQueries
{
    private readonly IReportDataProviderFactory _reportDataProviderFactory;
    private readonly IMonthlyTotalsReport _monthlyTotalsReport;
    private readonly IClock _clock;
    private readonly CurrencySettings _currencySettings;

    public SavingsRateQueries(
        IReportDataProviderFactory reportDataProviderFactory,
        IMonthlyTotalsReport monthlyTotalsReport,
        IStatisticsReport statisticsReport,
        IClock clock,
        CurrencySettings currencySettings)
    {
        _reportDataProviderFactory = reportDataProviderFactory;
        _monthlyTotalsReport = monthlyTotalsReport;
        _clock = clock;
        _currencySettings = currencySettings;
    }

    public async Task<SavingsRateDataDto> GetSavingsRateAsync(GetSavingsRateQuery query)
    {
        var currency = FiatCurrency.GetFromCode(_currencySettings.MainFiatCurrency);
        var provider = await _reportDataProviderFactory.CreateAsync();

        if (provider.AllTransactions.Count == 0)
        {
            return new SavingsRateDataDto
            {
                Months = [],
                PrimaryCurrency = currency.Code
            };
        }

        try
        {
            var today = _clock.GetCurrentLocalDate();
            var firstOfCurrentMonth = new DateOnly(today.Year, today.Month, 1);
            var monthlyTotals = await _monthlyTotalsReport.GetAsync(
                today,
                new DateOnlyRange(query.From, query.To),
                currency,
                provider);

            var months = new List<SavingsRateMonthDto>();
            foreach (var item in monthlyTotals.Items)
            {
                if (item.MonthYear >= firstOfCurrentMonth)
                {
                    continue;
                }

                var income = item.AllIncomeInFiat;
                if (income <= 0)
                {
                    months.Add(new SavingsRateMonthDto
                    {
                        Month = item.MonthYear,
                        Rate = null
                    });
                    continue;
                }

                var expenses = Math.Abs(item.AllExpensesInFiat);
                var rate = Math.Round((income - expenses) / income * 100, 2);
                months.Add(new SavingsRateMonthDto
                {
                    Month = item.MonthYear,
                    Rate = rate
                });
            }

            return new SavingsRateDataDto
            {
                Months = months,
                PrimaryCurrency = currency.Code
            };
        }
        catch (ApplicationException)
        {
            return new SavingsRateDataDto
            {
                Months = [],
                PrimaryCurrency = currency.Code
            };
        }
    }
}
