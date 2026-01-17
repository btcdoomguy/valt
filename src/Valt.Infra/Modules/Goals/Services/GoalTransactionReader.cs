using LiteDB;
using Valt.Core.Common;
using Valt.Infra.DataAccess;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.Budget.Transactions;
using Valt.Infra.Modules.DataSources.Bitcoin;
using Valt.Infra.Modules.DataSources.Fiat;
using Valt.Infra.Settings;

namespace Valt.Infra.Modules.Goals.Services;

/// <summary>
/// Provides shared functionality for reading transactions and converting amounts
/// to the main fiat currency. Used by goal progress calculators that need
/// multi-currency support.
/// </summary>
internal interface IGoalTransactionReader
{
    /// <summary>
    /// Calculates total expenses in the main fiat currency for the given date range.
    /// Only includes real expenses: Fiat debits and Bitcoin debits.
    /// Does NOT include transfers (FiatToBitcoin, BitcoinToFiat).
    /// </summary>
    decimal CalculateTotalExpenses(DateOnly from, DateOnly to, ObjectId? categoryId = null);

    /// <summary>
    /// Calculates total fiat income in the main fiat currency for the given date range.
    /// Only includes real fiat income (positive fiat transactions).
    /// Does NOT include BitcoinToFiat (handled by IncomeBtcGoalType).
    /// </summary>
    decimal CalculateTotalIncome(DateOnly from, DateOnly to);
}

internal class GoalTransactionReader : IGoalTransactionReader
{
    private const decimal SatoshisPerBitcoin = 100_000_000m;
    private static readonly DateOnly CutoffDate = new(2010, 1, 1);

    private readonly ILocalDatabase _localDatabase;
    private readonly IPriceDatabase _priceDatabase;
    private readonly CurrencySettings _currencySettings;

    public GoalTransactionReader(
        ILocalDatabase localDatabase,
        IPriceDatabase priceDatabase,
        CurrencySettings currencySettings)
    {
        _localDatabase = localDatabase;
        _priceDatabase = priceDatabase;
        _currencySettings = currencySettings;
    }

    public decimal CalculateTotalExpenses(DateOnly from, DateOnly to, ObjectId? categoryId = null)
    {
        var context = LoadDataContext(from, to, categoryId);
        var totalExpenses = 0m;

        foreach (var tx in context.Transactions)
        {
            var txDate = DateOnly.FromDateTime(tx.Date.ToUniversalTime());

            // Handle fiat expenses (negative FromFiatAmount on Fiat type only)
            if (tx.Type == TransactionEntityType.Fiat && tx.FromFiatAmount < 0)
            {
                var amount = ConvertFiatTransactionAmount(tx, context, txDate, useFromAccount: true, absoluteValue: true);
                totalExpenses += amount;
            }
            // Handle Bitcoin expenses (negative FromSatAmount on Bitcoin type only)
            else if (tx.Type == TransactionEntityType.Bitcoin && tx.FromSatAmount < 0)
            {
                var btcAmount = Math.Abs(tx.FromSatAmount ?? 0) / SatoshisPerBitcoin;
                totalExpenses += ConvertBtcToTarget(btcAmount, context, txDate);
            }
            // NOTE: FiatToBitcoin and BitcoinToFiat are transfers, not expenses
        }

        return totalExpenses;
    }

    public decimal CalculateTotalIncome(DateOnly from, DateOnly to)
    {
        var context = LoadDataContext(from, to, categoryId: null);
        var totalIncome = 0m;

        foreach (var tx in context.Transactions)
        {
            var txDate = DateOnly.FromDateTime(tx.Date.ToUniversalTime());

            // Handle fiat income (positive FromFiatAmount on Fiat type only)
            if (tx.Type == TransactionEntityType.Fiat && tx.FromFiatAmount > 0)
            {
                var amount = ConvertFiatTransactionAmount(tx, context, txDate, useFromAccount: true, absoluteValue: false);
                totalIncome += amount;
            }
            // NOTE: BitcoinToFiat is NOT counted as fiat income - it's a transfer
            // Bitcoin income is handled by IncomeBtcGoalType separately
        }

        return totalIncome;
    }

    private decimal ConvertFiatTransactionAmount(
        TransactionEntity tx,
        DataContext context,
        DateOnly txDate,
        bool useFromAccount,
        bool absoluteValue)
    {
        var accountId = useFromAccount ? tx.FromAccountId : tx.ToAccountId;
        var fiatAmount = useFromAccount ? (tx.FromFiatAmount ?? 0) : (tx.ToFiatAmount ?? 0);

        if (accountId is null || !context.Accounts.TryGetValue(accountId, out var account))
            return 0;

        var amount = absoluteValue ? Math.Abs(fiatAmount) : fiatAmount;
        var accountCurrency = account.Currency ?? context.MainFiatCurrencyCode;

        if (accountCurrency == context.MainFiatCurrencyCode)
            return amount;

        return ConvertFiatToTarget(amount, accountCurrency, context, txDate);
    }

    private DataContext LoadDataContext(DateOnly from, DateOnly to, ObjectId? categoryId)
    {
        var mainFiatCurrencyCode = _currencySettings.MainFiatCurrency;
        var targetCurrency = FiatCurrency.GetFromCode(mainFiatCurrencyCode);

        var fromDate = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toDate = to.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        // Load all accounts for currency lookups
        var accounts = _localDatabase.GetAccounts()
            .FindAll()
            .ToDictionary(x => x.Id);

        // Load price data for the relevant date range (with buffer for closest date lookups)
        var bufferDays = 7;
        var priceFromDate = from.AddDays(-bufferDays).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var priceToDate = to.AddDays(bufferDays).ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var btcRates = _priceDatabase.GetBitcoinData()
            .Find(x => x.Date >= priceFromDate && x.Date <= priceToDate)
            .ToDictionary(x => DateOnly.FromDateTime(x.Date.ToUniversalTime()));
        var sortedBtcDates = btcRates.Keys.Order().ToList();

        var fiatRatesInRange = _priceDatabase.GetFiatData()
            .Find(x => x.Date >= priceFromDate && x.Date <= priceToDate)
            .ToList();
        var fiatRates = fiatRatesInRange
            .GroupBy(x => DateOnly.FromDateTime(x.Date.ToUniversalTime()))
            .ToDictionary(x => x.Key, x => x.ToList());
        var sortedFiatDatesByCurrency = fiatRatesInRange
            .GroupBy(x => x.Currency)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => DateOnly.FromDateTime(x.Date.ToUniversalTime()))
                    .Distinct()
                    .Order()
                    .ToList());

        // Load transactions
        var transactionsQuery = _localDatabase.GetTransactions()
            .Find(x => x.Date >= fromDate && x.Date <= toDate);

        var transactions = categoryId is not null
            ? transactionsQuery.Where(x => x.CategoryId == categoryId).ToList()
            : transactionsQuery.ToList();

        return new DataContext(
            mainFiatCurrencyCode,
            targetCurrency,
            accounts,
            btcRates,
            sortedBtcDates,
            fiatRates,
            sortedFiatDatesByCurrency,
            transactions);
    }

    private decimal ConvertFiatToTarget(
        decimal amount,
        string sourceCurrencyCode,
        DataContext context,
        DateOnly date)
    {
        if (sourceCurrencyCode == context.TargetCurrency.Code)
            return amount;

        // Get source rate to USD (if not USD)
        var sourceRateToUsd = 1m;
        if (sourceCurrencyCode != FiatCurrency.Usd.Code)
        {
            sourceRateToUsd = GetFiatRateAt(date, sourceCurrencyCode, context);
            if (sourceRateToUsd == 0) return 0;
        }

        // Get target rate from USD (if target is not USD)
        var targetRateFromUsd = 1m;
        if (context.TargetCurrency.Code != FiatCurrency.Usd.Code)
        {
            targetRateFromUsd = GetFiatRateAt(date, context.TargetCurrency.Code, context);
            if (targetRateFromUsd == 0) return 0;
        }

        // Convert: source -> USD -> target
        return targetRateFromUsd * (amount / sourceRateToUsd);
    }

    private decimal ConvertBtcToTarget(decimal btcAmount, DataContext context, DateOnly date)
    {
        var usdBitcoinPrice = GetUsdBitcoinPriceAt(date, context);
        if (usdBitcoinPrice == 0) return 0;

        var usdValue = btcAmount * usdBitcoinPrice;

        var targetRateFromUsd = GetFiatRateAt(date, context.TargetCurrency.Code, context);
        if (targetRateFromUsd == 0) return usdValue;

        return targetRateFromUsd * usdValue;
    }

    private decimal GetFiatRateAt(DateOnly date, string currencyCode, DataContext context)
    {
        if (currencyCode == FiatCurrency.Usd.Code)
            return 1m;

        if (!context.SortedFiatDatesByCurrency.TryGetValue(currencyCode, out var sortedDates) || sortedDates.Count == 0)
            return 0m;

        var targetDate = date < CutoffDate ? sortedDates[0] : FindClosestDate(sortedDates, date);

        if (context.FiatRates.TryGetValue(targetDate, out var rates))
        {
            var entry = rates.FirstOrDefault(x => x.Currency == currencyCode);
            if (entry is not null)
                return entry.Price;
        }

        return 0m;
    }

    private decimal GetUsdBitcoinPriceAt(DateOnly date, DataContext context)
    {
        if (context.SortedBtcDates.Count == 0)
            return 0m;

        var targetDate = date < CutoffDate ? context.SortedBtcDates[0] : FindClosestDate(context.SortedBtcDates, date);

        if (context.BtcRates.TryGetValue(targetDate, out var btcRate))
            return btcRate.Price;

        return 0m;
    }

    private static DateOnly FindClosestDate(List<DateOnly> sortedDates, DateOnly targetDate)
    {
        var index = sortedDates.BinarySearch(targetDate);

        if (index >= 0)
            return sortedDates[index];

        var insertionPoint = ~index;

        if (insertionPoint == 0)
            return sortedDates[0];

        return sortedDates[insertionPoint - 1];
    }

    private record DataContext(
        string MainFiatCurrencyCode,
        FiatCurrency TargetCurrency,
        Dictionary<ObjectId, AccountEntity> Accounts,
        Dictionary<DateOnly, BitcoinDataEntity> BtcRates,
        List<DateOnly> SortedBtcDates,
        Dictionary<DateOnly, List<FiatDataEntity>> FiatRates,
        Dictionary<string, List<DateOnly>> SortedFiatDatesByCurrency,
        List<TransactionEntity> Transactions);
}
