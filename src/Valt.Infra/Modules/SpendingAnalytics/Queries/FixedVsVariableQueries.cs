using LiteDB;
using Valt.App.Modules.SpendingAnalytics.Contracts;
using Valt.App.Modules.SpendingAnalytics.DTOs;
using Valt.App.Modules.SpendingAnalytics.Queries;
using Valt.Core.Common;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Core.Modules.Budget.FixedExpenses;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.Budget.Transactions;
using Valt.Infra.Modules.Currency.Services;
using Valt.Infra.Settings;

namespace Valt.Infra.Modules.SpendingAnalytics.Queries;

public class FixedVsVariableQueries : IFixedVsVariableQueries
{
    private readonly ILocalDatabase _localDatabase;
    private readonly IPriceDatabase _priceDatabase;
    private readonly ICurrencyConversionService _currencyConversionService;
    private readonly CurrencySettings _currencySettings;
    private readonly IClock _clock;

    public FixedVsVariableQueries(
        ILocalDatabase localDatabase,
        IPriceDatabase priceDatabase,
        ICurrencyConversionService currencyConversionService,
        CurrencySettings currencySettings,
        IClock clock)
    {
        _localDatabase = localDatabase;
        _priceDatabase = priceDatabase;
        _currencyConversionService = currencyConversionService;
        _currencySettings = currencySettings;
        _clock = clock;
    }

    public Task<FixedVsVariableDataDto> GetFixedVsVariableAsync(GetFixedVsVariableQuery query)
    {
        var primaryCurrency = _currencySettings.MainFiatCurrency;

        // D-10: when the user has no fixed expenses registered, return the empty-state flag
        if (_localDatabase.GetFixedExpenses().Count() == 0)
        {
            return Task.FromResult(new FixedVsVariableDataDto
            {
                Months = new List<FixedVsVariableMonthDto>(),
                HasNoFixedExpenses = true,
                PrimaryCurrency = primaryCurrency
            });
        }

        // D-06: collect only Paid records that are bound to an actual transaction
        var paidTransactionIds = _localDatabase.GetFixedExpenseRecords()
            .Query()
            .Where(r => r.FixedExpenseRecordStateId == (int)FixedExpenseRecordState.Paid && r.Transaction != null)
            .ToList()
            .Select(r => r.Transaction!.Id)
            .ToHashSet();

        // Load accounts and apply optional account filter
        var allAccountsList = _localDatabase.GetAccounts().FindAll().ToList();
        var selectedAccountIds = query.AccountIds.Length > 0
            ? query.AccountIds.Select(id => new ObjectId(id)).ToList()
            : allAccountsList.Select(a => a.Id).ToList();
        var accountDict = allAccountsList.Where(a => selectedAccountIds.Contains(a.Id)).ToDictionary(a => a.Id);

        // Get latest rates for currency conversion
        var (bitcoinPriceUsd, fiatRates) = GetLatestRates();

        // Build transaction query with LiteDB-side filtering (mirrors SpendingEvolutionQueries)
        var transactionQuery = _localDatabase.GetTransactions().Query();

        var fromDate = query.From.ToValtDateTime();
        var toDate = query.To.ToValtDateTime();
        transactionQuery = transactionQuery.Where(x => x.Date >= fromDate && x.Date <= toDate);

        if (query.CategoryIds.Length > 0)
        {
            var categoryObjectIds = query.CategoryIds.Select(id => new ObjectId(id)).ToList();
            transactionQuery = transactionQuery.Where(x => categoryObjectIds.Contains(x.CategoryId));
        }

        if (selectedAccountIds.Count < allAccountsList.Count)
        {
            transactionQuery = transactionQuery.Where(x => selectedAccountIds.Contains(x.FromAccountId));
        }

        transactionQuery = transactionQuery.Where(x =>
            (x.FromFiatAmount.HasValue && x.FromFiatAmount.Value < 0) ||
            (x.SatAmount.HasValue && x.FromSatAmount.HasValue && x.FromSatAmount.Value < 0));

        var transactions = transactionQuery.ToList();

        // Aggregate per month, splitting fixed vs variable
        var today = _clock.GetCurrentLocalDate();
        var firstOfCurrentMonth = new DateOnly(today.Year, today.Month, 1);

        var monthlyData = AggregateByMonth(transactions, accountDict, paidTransactionIds, primaryCurrency, bitcoinPriceUsd, fiatRates);

        var sortedMonths = monthlyData
            .Where(x => new DateOnly(x.Key.Year, x.Key.Month, 1) < firstOfCurrentMonth)
            .OrderBy(x => x.Key.Year)
            .ThenBy(x => x.Key.Month)
            .Select(x => new FixedVsVariableMonthDto
            {
                Month = new DateOnly(x.Key.Year, x.Key.Month, 1),
                FixedTotal = x.Value.FixedTotal,
                VariableTotal = x.Value.VariableTotal
            })
            .ToList();

        return Task.FromResult(new FixedVsVariableDataDto
        {
            Months = sortedMonths,
            HasNoFixedExpenses = false,
            PrimaryCurrency = primaryCurrency
        });
    }

    private Dictionary<(int Year, int Month), (decimal FixedTotal, decimal VariableTotal)> AggregateByMonth(
        List<TransactionEntity> transactions,
        Dictionary<ObjectId, AccountEntity> accountDict,
        HashSet<ObjectId> paidTransactionIds,
        string primaryCurrency,
        decimal? bitcoinPriceUsd,
        IReadOnlyDictionary<string, decimal>? fiatRates)
    {
        var monthlyData = new Dictionary<(int Year, int Month), (decimal FixedTotal, decimal VariableTotal)>();

        foreach (var transaction in transactions)
        {
            if (!accountDict.TryGetValue(transaction.FromAccountId, out var account))
                continue;

            var yearMonth = (transaction.Date.Year, transaction.Date.Month);

            if (!monthlyData.ContainsKey(yearMonth))
            {
                monthlyData[yearMonth] = (0m, 0m);
            }

            var current = monthlyData[yearMonth];

            decimal amount;
            string? sourceCurrency;

            if (transaction.FromFiatAmount.HasValue)
            {
                amount = Math.Abs(transaction.FromFiatAmount.Value);
                sourceCurrency = account.Currency;
            }
            else if (transaction.FromSatAmount.HasValue)
            {
                amount = Math.Abs((decimal)transaction.FromSatAmount.Value);
                sourceCurrency = "SATS";
            }
            else
            {
                continue;
            }

            var converted = ConvertToPrimaryCurrency(amount, sourceCurrency, primaryCurrency, bitcoinPriceUsd, fiatRates);

            if (paidTransactionIds.Contains(transaction.Id))
            {
                current.FixedTotal += converted;
            }
            else
            {
                current.VariableTotal += converted;
            }

            monthlyData[yearMonth] = current;
        }

        return monthlyData;
    }

    private (decimal? BitcoinPriceUsd, IReadOnlyDictionary<string, decimal>? FiatRates) GetLatestRates()
    {
        try
        {
            var latestBtc = _priceDatabase.GetBitcoinData()
                .Query()
                .OrderByDescending(x => x.Date)
                .FirstOrDefault();

            decimal? bitcoinPriceUsd = latestBtc?.Price;

            var latestFiatDate = _priceDatabase.GetFiatData()
                .Query()
                .OrderByDescending(x => x.Date)
                .Select(x => x.Date)
                .FirstOrDefault();

            IReadOnlyDictionary<string, decimal>? fiatRates = null;

            if (latestFiatDate != default)
            {
                var startDate = latestFiatDate.AddDays(-5);
                var fiatEntries = _priceDatabase.GetFiatData()
                    .Find(x => x.Date >= startDate && x.Date <= latestFiatDate)
                    .GroupBy(x => x.Currency)
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderByDescending(x => x.Date).First().Price);

                fiatRates = fiatEntries;
            }

            return (bitcoinPriceUsd, fiatRates);
        }
        catch
        {
            return (null, null);
        }
    }

    private decimal ConvertToPrimaryCurrency(decimal amount, string? sourceCurrency, string targetCurrency, decimal? bitcoinPriceUsd, IReadOnlyDictionary<string, decimal>? fiatRates)
    {
        if (string.IsNullOrEmpty(sourceCurrency) || sourceCurrency == targetCurrency)
            return amount;

        try
        {
            return _currencyConversionService.Convert(amount, sourceCurrency, targetCurrency, bitcoinPriceUsd, fiatRates);
        }
        catch
        {
            return amount;
        }
    }
}
