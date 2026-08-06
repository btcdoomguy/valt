using System.Collections.Frozen;
using LiteDB;
using Valt.App.Modules.BtcDenominatedMetrics.Contracts;
using Valt.App.Modules.BtcDenominatedMetrics.DTOs;
using Valt.App.Modules.BtcDenominatedMetrics.Queries;
using Valt.Core.Common;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.Budget.Categories;
using Valt.Infra.Modules.Budget.Transactions;
using Valt.Infra.Modules.Reports;
using Valt.Infra.Modules.Reports.MonthlyTotals;
using Valt.Infra.Settings;

namespace Valt.Infra.Modules.BtcDenominatedMetrics.Queries;

public class BtcDenominatedMetricsQueries : IBtcDenominatedMetricsQueries
{
    private const decimal SatoshisPerBitcoin = 100_000_000m;

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
        var categorySatsSpent = new Dictionary<ObjectId, long>();

        foreach (var transaction in provider.AllTransactions)
        {
            var transactionDate = DateOnly.FromDateTime(transaction.Date.ToUniversalTime());

            if (transactionDate < query.From || transactionDate > query.To)
                continue;

            if (selectedAccountIds is not null && !selectedAccountIds.Contains(transaction.FromAccountId))
                continue;

            if (selectedCategoryIds is not null && !selectedCategoryIds.Contains(transaction.CategoryId))
                continue;

            // Internal transfers between the user's own accounts are excluded from earned/spent (D-04).
            if (transaction.ToAccountId is { } toId && provider.Accounts.ContainsKey(toId))
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
                    {
                        current.SatsSpent += sats; // sats is negative for debits
                        AddCategorySpent(categorySatsSpent, transaction.CategoryId, Math.Abs(sats));
                    }
                    break;

                case TransactionEntityType.Bitcoin:
                    if (transaction.FromSatAmount is not { } satAmount)
                        break;

                    if (satAmount > 0)
                        current.SatsEarned += satAmount;
                    else if (satAmount < 0)
                    {
                        current.SatsSpent += satAmount;
                        AddCategorySpent(categorySatsSpent, transaction.CategoryId, Math.Abs(satAmount));
                    }
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

        var sortedMonths = monthlyData
            .OrderBy(x => x.Key.Year)
            .ThenBy(x => x.Key.Month)
            .Select(x => new BtcDenominatedMetricsMonthDto
            {
                Month = new DateOnly(x.Key.Year, x.Key.Month, 1),
                SatsEarned = x.Value.SatsEarned,
                SatsSpent = x.Value.SatsSpent,
                StackVelocity = x.Value.SatsEarned - x.Value.SatsSpent + x.Value.BtcPurchases - x.Value.BtcSales
            })
            .ToList();

        var spentByCategory = categorySatsSpent
            .Where(x => x.Value > 0)
            .Select(x =>
            {
                provider.Categories.TryGetValue(x.Key, out var category);
                var icon = category is not null ? Icon.RestoreFromId(category.Icon ?? string.Empty) : null;
                return new SatsSpentByCategoryDto
                {
                    CategoryId = x.Key.ToString(),
                    CategoryName = category?.Name ?? string.Empty,
                    SatsTotal = x.Value,
                    IconUnicode = icon is not null && icon.Unicode != char.MinValue ? ((int)icon.Unicode).ToString("X4") : null,
                    IconColor = icon?.Color.ToArgb().ToString("X8")
                };
            })
            .OrderByDescending(x => x.SatsTotal)
            .ToList();

        return new BtcDenominatedMetricsDataDto
        {
            Months = sortedMonths,
            SpentByCategory = spentByCategory,
            PrimaryCurrency = currency.Code
        };
    }

    private static void AddCategorySpent(Dictionary<ObjectId, long> categorySatsSpent, ObjectId categoryId, long sats)
    {
        categorySatsSpent[categoryId] = categorySatsSpent.GetValueOrDefault(categoryId) + sats;
    }

    private static long ConvertFiatToSats(decimal fiatAmount, DateOnly transactionDate, AccountEntity account, IReportDataProvider provider)
    {
        var accountCurrency = FiatCurrency.GetFromCode(account.Currency!);
        var fiatRateToUsd = provider.GetFiatRateAt(transactionDate, accountCurrency);
        var btcPriceUsd = provider.GetUsdBitcoinPriceAt(transactionDate);

        var usdAmount = fiatAmount / fiatRateToUsd;
        var btcAmount = usdAmount / btcPriceUsd;
        return (long)(btcAmount * SatoshisPerBitcoin);
    }

    private record MonthSatsAggregation
    {
        public long SatsEarned { get; set; }
        public long SatsSpent { get; set; }
        public long BtcPurchases { get; set; }
        public long BtcSales { get; set; }
    }
}
