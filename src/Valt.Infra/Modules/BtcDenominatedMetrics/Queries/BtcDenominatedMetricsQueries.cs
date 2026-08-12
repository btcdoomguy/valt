using System.Collections.Frozen;
using LiteDB;
using Valt.App.Modules.BtcDenominatedMetrics.Contracts;
using Valt.App.Modules.BtcDenominatedMetrics.DTOs;
using Valt.App.Modules.BtcDenominatedMetrics.Queries;
using Valt.Core.Common;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.Budget.Transactions;
using Valt.Infra.Modules.Reports;
using Valt.Infra.Modules.Reports.MonthlyTotals;
using Valt.Infra.Settings;

namespace Valt.Infra.Modules.BtcDenominatedMetrics.Queries;

public class BtcDenominatedMetricsQueries : IBtcDenominatedMetricsQueries
{
    private readonly IReportDataProviderFactory _reportDataProviderFactory;
    private readonly IMonthlyTotalsReport _monthlyTotalsReport;
    private readonly IClock _clock;
    private readonly CurrencySettings _currencySettings;

    public BtcDenominatedMetricsQueries(
        IReportDataProviderFactory reportDataProviderFactory,
        IMonthlyTotalsReport monthlyTotalsReport,
        IClock clock,
        CurrencySettings currencySettings)
    {
        _reportDataProviderFactory = reportDataProviderFactory;
        _monthlyTotalsReport = monthlyTotalsReport;
        _clock = clock;
        _currencySettings = currencySettings;
    }

    public async Task<BtcDenominatedMetricsDataDto> GetBtcDenominatedMetricsAsync(GetBtcDenominatedMetricsQuery query)
    {
        var currency = FiatCurrency.GetFromCode(_currencySettings.MainFiatCurrency);
        var provider = await _reportDataProviderFactory.CreateAsync();

        HashSet<ObjectId>? selectedAccountIds = query.AccountIds.Length > 0
            ? query.AccountIds.Select(id => new ObjectId(id)).ToHashSet()
            : null;

        HashSet<ObjectId>? selectedCategoryIds = query.CategoryIds.Length > 0
            ? query.CategoryIds.Select(id => new ObjectId(id)).ToHashSet()
            : null;

        var monthlyData = new Dictionary<(int Year, int Month), MonthSatsAggregation>();

        foreach (var transaction in provider.AllTransactions)
        {
            var transactionDate = DateOnly.FromDateTime(transaction.Date.ToUniversalTime());

            if (transactionDate < query.From || transactionDate > query.To)
                continue;

            if (selectedAccountIds is not null && !selectedAccountIds.Contains(transaction.FromAccountId))
                continue;

            if (selectedCategoryIds is not null && !selectedCategoryIds.Contains(transaction.CategoryId))
                continue;

            // Internal transfers (FiatToFiat / BitcoinToBitcoin) between the user's own accounts are excluded
            // from earned/spent and velocity (D-04). BTC purchases and sales are not transfers (D-01, D-09).
            if (transaction.Type is TransactionEntityType.FiatToFiat or TransactionEntityType.BitcoinToBitcoin
                && transaction.ToAccountId is { } toId
                && provider.Accounts.ContainsKey(toId))
                continue;

            // Transactions with no exact BTC price for the transaction date are silently skipped (D-06, D-07).
            if (!provider.BtcRates.ContainsKey(transactionDate))
                continue;

            if (!provider.Accounts.TryGetValue(transaction.FromAccountId, out var account))
                continue;

            var yearMonth = (transactionDate.Year, transactionDate.Month);
            if (!monthlyData.TryGetValue(yearMonth, out var current))
                current = new MonthSatsAggregation();

            switch (transaction.Type)
            {
                case TransactionEntityType.Fiat:
                    if (transaction.FromFiatAmount is not { } fiatAmount)
                        break;

                    var sats = ConvertFiatToSats(fiatAmount, transactionDate, account, provider);
                    if (fiatAmount > 0)
                        current.SatsEarned += sats;
                    else if (fiatAmount < 0)
                        current.SatsSpent += sats; // sats is negative for debits
                    break;

                case TransactionEntityType.Bitcoin:
                    if (transaction.FromSatAmount is not { } satAmount)
                        break;

                    if (satAmount > 0)
                        current.SatsEarned += satAmount;
                    else if (satAmount < 0)
                        current.SatsSpent += satAmount;
                    break;

                case TransactionEntityType.FiatToBitcoin:
                    if (transaction.ToSatAmount is > 0)
                        current.BtcPurchases += transaction.ToSatAmount.Value;
                    break;

                case TransactionEntityType.BitcoinToFiat:
                    if (transaction.FromSatAmount is < 0)
                        current.BtcSales += transaction.FromSatAmount.Value; // negative value
                    break;
            }

            monthlyData[yearMonth] = current;
        }

        var startMonth = new DateOnly(query.From.Year, query.From.Month, 1);
        var endMonth = new DateOnly(query.To.Year, query.To.Month, 1);
        var sortedMonths = new List<BtcDenominatedMetricsMonthDto>();
        for (var month = startMonth; month <= endMonth; month = month.AddMonths(1))
        {
            var key = (month.Year, month.Month);
            var aggregation = monthlyData.TryGetValue(key, out var current)
                ? current
                : new MonthSatsAggregation();
            sortedMonths.Add(new BtcDenominatedMetricsMonthDto
            {
                Month = month,
                SatsEarned = aggregation.SatsEarned,
                SatsSpent = aggregation.SatsSpent,
                StackVelocity = aggregation.SatsEarned + aggregation.SatsSpent + aggregation.BtcPurchases + aggregation.BtcSales
            });
        }

        return new BtcDenominatedMetricsDataDto
        {
            Months = sortedMonths,
            PrimaryCurrency = currency.Code
        };
    }

    private static long ConvertFiatToSats(decimal fiatAmount, DateOnly transactionDate, AccountEntity account, IReportDataProvider provider)
    {
        return HistoricalRateConverter.ConvertFiatToSats(fiatAmount, account.Currency!, transactionDate, provider);
    }

    private record MonthSatsAggregation
    {
        public long SatsEarned { get; set; }
        public long SatsSpent { get; set; }
        public long BtcPurchases { get; set; }
        public long BtcSales { get; set; }
    }
}
