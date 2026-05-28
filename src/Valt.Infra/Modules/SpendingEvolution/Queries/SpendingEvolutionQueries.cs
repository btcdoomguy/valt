using LiteDB;
using Valt.App.Modules.SpendingEvolution.Contracts;
using Valt.App.Modules.SpendingEvolution.DTOs;
using Valt.App.Modules.SpendingEvolution.Queries;
using Valt.Core.Common;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.Budget.Transactions;
using Valt.Infra.Modules.Currency.Services;
using Valt.Infra.Settings;

namespace Valt.Infra.Modules.SpendingEvolution.Queries;

public class SpendingEvolutionQueries : ISpendingEvolutionQueries
{
    private readonly ILocalDatabase _localDatabase;
    private readonly IPriceDatabase _priceDatabase;
    private readonly ICurrencyConversionService _currencyConversionService;
    private readonly CurrencySettings _currencySettings;

    public SpendingEvolutionQueries(
        ILocalDatabase localDatabase,
        IPriceDatabase priceDatabase,
        ICurrencyConversionService currencyConversionService,
        CurrencySettings currencySettings)
    {
        _localDatabase = localDatabase;
        _priceDatabase = priceDatabase;
        _currencyConversionService = currencyConversionService;
        _currencySettings = currencySettings;
    }

    public Task<SpendingEvolutionDataDto> GetSpendingEvolutionAsync(GetSpendingEvolutionQuery query)
    {
        // Load accounts
        var allAccountsList = _localDatabase.GetAccounts().FindAll().ToList();
        var selectedAccountIds = query.AccountIds.Length > 0
            ? query.AccountIds.Select(id => new ObjectId(id)).ToList()
            : allAccountsList.Select(a => a.Id).ToList();

        var accountDict = allAccountsList.Where(a => selectedAccountIds.Contains(a.Id)).ToDictionary(a => a.Id);

        // Get primary currency
        var primaryCurrency = _currencySettings.MainFiatCurrency;

        // Get latest rates for currency conversion
        var (bitcoinPriceUsd, fiatRates) = GetLatestRates();

        // Build transaction query with LiteDB-side filtering
        var transactionQuery = _localDatabase.GetTransactions().Query();

        // Filter by date range
        var fromDate = query.From.ToValtDateTime();
        var toDate = query.To.ToValtDateTime();
        transactionQuery = transactionQuery.Where(x => x.Date >= fromDate && x.Date <= toDate);

        // Filter by category IDs if provided
        if (query.CategoryIds.Length > 0)
        {
            var categoryObjectIds = query.CategoryIds.Select(id => new ObjectId(id)).ToList();
            transactionQuery = transactionQuery.Where(x => categoryObjectIds.Contains(x.CategoryId));
        }

        // Filter by selected accounts
        if (selectedAccountIds.Count < allAccountsList.Count)
        {
            transactionQuery = transactionQuery.Where(x => selectedAccountIds.Contains(x.FromAccountId));
        }

        // Include all debit transactions (spending): fiat expenses OR bitcoin expenses with auto-calculated sats
        transactionQuery = transactionQuery.Where(x =>
            (x.FromFiatAmount.HasValue && x.FromFiatAmount.Value < 0) ||
            (x.SatAmount.HasValue && x.FromSatAmount.HasValue && x.FromSatAmount.Value < 0));

        // Execute query - load filtered results into memory
        var transactions = transactionQuery.ToList();

        // Aggregate by month
        var monthlyData = new Dictionary<(int Year, int Month), (decimal FiatTotal, long SatsTotal, int TransactionCount)>();

        foreach (var transaction in transactions)
        {
            if (!accountDict.TryGetValue(transaction.FromAccountId, out var account))
                continue;

            var yearMonth = (transaction.Date.Year, transaction.Date.Month);

            if (!monthlyData.ContainsKey(yearMonth))
            {
                monthlyData[yearMonth] = (0m, 0L, 0);
            }

            var current = monthlyData[yearMonth];

            // Process fiat amount
            if (transaction.FromFiatAmount.HasValue && transaction.FromFiatAmount.Value < 0)
            {
                var absoluteFiat = Math.Abs(transaction.FromFiatAmount.Value);
                var convertedFiat = ConvertToPrimaryCurrency(absoluteFiat, account.Currency, primaryCurrency, bitcoinPriceUsd, fiatRates);
                current.FiatTotal += convertedFiat;
            }

            // Process auto-calculated sat amount (from AutoSatAmountDetails) only if available
            if (transaction.SatAmount.HasValue)
            {
                var absoluteSats = Math.Abs(transaction.SatAmount.Value);
                current.SatsTotal += absoluteSats;
            }

            current.TransactionCount++;
            monthlyData[yearMonth] = current;
        }

        // Sort by year and month ascending
        var sortedMonths = monthlyData
            .OrderBy(x => x.Key.Year)
            .ThenBy(x => x.Key.Month)
            .Select(x => new SpendingEvolutionMonthDto
            {
                Month = new DateOnly(x.Key.Year, x.Key.Month, 1),
                FiatTotal = x.Value.FiatTotal,
                SatsTotal = x.Value.SatsTotal,
                TransactionCount = x.Value.TransactionCount
            })
            .ToList();

        return Task.FromResult(new SpendingEvolutionDataDto
        {
            Months = sortedMonths,
            HasMissingPriceInSats = false,
            PrimaryCurrency = primaryCurrency
        });
    }

    private (decimal? BitcoinPriceUsd, IReadOnlyDictionary<string, decimal>? FiatRates) GetLatestRates()
    {
        try
        {
            // Get latest Bitcoin price in USD
            var latestBtc = _priceDatabase.GetBitcoinData()
                .Query()
                .OrderByDescending(x => x.Date)
                .FirstOrDefault();

            decimal? bitcoinPriceUsd = latestBtc?.Price;

            // Get latest fiat rates (most recent entry per currency)
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
            // If rate lookup fails, return nulls - conversion will return original amount
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
            // If conversion fails, return original amount
            return amount;
        }
    }
}
