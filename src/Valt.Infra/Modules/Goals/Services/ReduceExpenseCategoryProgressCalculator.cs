using LiteDB;
using JsonSerializer = System.Text.Json.JsonSerializer;
using Valt.Core.Common;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.GoalTypes;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.Budget.Transactions;
using Valt.Infra.Modules.DataSources.Bitcoin;
using Valt.Infra.Modules.DataSources.Fiat;
using Valt.Infra.Modules.Goals.Queries.DTOs;
using Valt.Infra.Settings;

namespace Valt.Infra.Modules.Goals.Services;

internal class ReduceExpenseCategoryProgressCalculator : IGoalProgressCalculator
{
    private const decimal SatoshisPerBitcoin = 100_000_000m;
    private static readonly DateOnly CutoffDate = new(2010, 1, 1);

    private readonly ILocalDatabase _localDatabase;
    private readonly IPriceDatabase _priceDatabase;
    private readonly CurrencySettings _currencySettings;

    public GoalTypeNames SupportedType => GoalTypeNames.ReduceExpenseCategory;

    public ReduceExpenseCategoryProgressCalculator(ILocalDatabase localDatabase, IPriceDatabase priceDatabase, CurrencySettings currencySettings)
    {
        _localDatabase = localDatabase;
        _priceDatabase = priceDatabase;
        _currencySettings = currencySettings;
    }

    public Task<GoalProgressResult> CalculateProgressAsync(GoalProgressInput input)
    {
        var dto = JsonSerializer.Deserialize<ReduceExpenseCategoryGoalTypeDto>(input.GoalTypeJson)
                  ?? throw new InvalidOperationException("Failed to deserialize ReduceExpenseCategoryGoalType");
        var config = new ReduceExpenseCategoryGoalType(dto.TargetAmount, dto.CategoryId, dto.CategoryName, dto.CalculatedSpending);

        var fromDate = input.From.ToValtDateTime();
        var toDate = input.To.ToValtDateTime().AddDays(1).AddTicks(-1);

        var mainFiatCurrencyCode = _currencySettings.MainFiatCurrency;
        var targetCurrency = FiatCurrency.GetFromCode(mainFiatCurrencyCode);
        var targetCategoryId = new ObjectId(config.CategoryId);

        // Load all accounts for currency lookups
        var accounts = _localDatabase.GetAccounts()
            .FindAll()
            .ToDictionary(x => x.Id);

        // Load price data for the relevant date range (with buffer for closest date lookups)
        var bufferDays = 7;
        var priceFromDate = input.From.AddDays(-bufferDays).ToValtDateTime();
        var priceToDate = input.To.AddDays(bufferDays).ToValtDateTime();

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

        var transactions = _localDatabase.GetTransactions()
            .Find(x => x.Date >= fromDate && x.Date <= toDate && x.CategoryId == targetCategoryId)
            .ToList();

        var totalSpending = 0m;

        foreach (var tx in transactions)
        {
            var txDate = DateOnly.FromDateTime(tx.Date.ToUniversalTime());

            // Handle fiat expenses (negative FromFiatAmount)
            if (tx.Type == TransactionEntityType.Fiat && tx.FromFiatAmount < 0)
            {
                if (!accounts.TryGetValue(tx.FromAccountId, out var account))
                    continue;

                var expenseAmount = Math.Abs(tx.FromFiatAmount ?? 0);
                var accountCurrency = account.Currency ?? mainFiatCurrencyCode;

                if (accountCurrency == mainFiatCurrencyCode)
                {
                    // Same currency, no conversion needed
                    totalSpending += expenseAmount;
                }
                else
                {
                    // Convert from account currency to target currency
                    var convertedAmount = ConvertFiatToTarget(
                        expenseAmount, accountCurrency, targetCurrency, txDate,
                        fiatRates, sortedFiatDatesByCurrency);
                    totalSpending += convertedAmount;
                }
            }
            // Handle Bitcoin expenses (negative FromSatAmount from Bitcoin type transactions)
            else if (tx.Type == TransactionEntityType.Bitcoin && tx.FromSatAmount < 0)
            {
                var btcAmount = Math.Abs(tx.FromSatAmount ?? 0) / SatoshisPerBitcoin;
                var convertedAmount = ConvertBtcToTarget(
                    btcAmount, targetCurrency, txDate,
                    btcRates, sortedBtcDates, fiatRates, sortedFiatDatesByCurrency);
                totalSpending += convertedAmount;
            }
            // Handle FiatToBitcoin expenses (negative FromFiatAmount = spending fiat to buy BTC)
            else if (tx.Type == TransactionEntityType.FiatToBitcoin && tx.FromFiatAmount < 0)
            {
                if (!accounts.TryGetValue(tx.FromAccountId, out var account))
                    continue;

                var expenseAmount = Math.Abs(tx.FromFiatAmount ?? 0);
                var accountCurrency = account.Currency ?? mainFiatCurrencyCode;

                if (accountCurrency == mainFiatCurrencyCode)
                {
                    totalSpending += expenseAmount;
                }
                else
                {
                    var convertedAmount = ConvertFiatToTarget(
                        expenseAmount, accountCurrency, targetCurrency, txDate,
                        fiatRates, sortedFiatDatesByCurrency);
                    totalSpending += convertedAmount;
                }
            }
            // Handle BitcoinToFiat - the BTC was spent, so count the fiat received as the expense value
            else if (tx.Type == TransactionEntityType.BitcoinToFiat && tx.FromSatAmount < 0)
            {
                // Use the fiat amount received as the expense value (what the BTC was worth)
                if (tx.ToFiatAmount.HasValue && tx.ToAccountId is not null)
                {
                    if (!accounts.TryGetValue(tx.ToAccountId, out var toAccount))
                        continue;

                    var fiatAmount = Math.Abs(tx.ToFiatAmount.Value);
                    var accountCurrency = toAccount.Currency ?? mainFiatCurrencyCode;

                    if (accountCurrency == mainFiatCurrencyCode)
                    {
                        totalSpending += fiatAmount;
                    }
                    else
                    {
                        var convertedAmount = ConvertFiatToTarget(
                            fiatAmount, accountCurrency, targetCurrency, txDate,
                            fiatRates, sortedFiatDatesByCurrency);
                        totalSpending += convertedAmount;
                    }
                }
            }
        }

        // Progress calculation: inverse percentage (100% = nothing spent, 0% = at or over limit)
        // Progress = 100 - ((spent / limit) * 100), capped between 0-100
        var progress = config.TargetAmount > 0
            ? Math.Max(0m, Math.Min(100m, 100m - ((totalSpending * 100m) / config.TargetAmount)))
            : (totalSpending == 0 ? 100m : 0m);

        // Create updated goal type with calculated values
        var updatedGoalType = config.WithCalculatedSpending(Math.Round(totalSpending, 2));

        return Task.FromResult(new GoalProgressResult(progress, updatedGoalType));
    }

    private decimal ConvertFiatToTarget(
        decimal amount,
        string sourceCurrencyCode,
        FiatCurrency targetCurrency,
        DateOnly date,
        Dictionary<DateOnly, List<FiatDataEntity>> fiatRates,
        Dictionary<string, List<DateOnly>> sortedFiatDatesByCurrency)
    {
        // If source and target are the same currency, no conversion needed
        if (sourceCurrencyCode == targetCurrency.Code)
            return amount;

        // Get source rate to USD (if not USD)
        var sourceRateToUsd = 1m;
        if (sourceCurrencyCode != FiatCurrency.Usd.Code)
        {
            sourceRateToUsd = GetFiatRateAt(date, sourceCurrencyCode, fiatRates, sortedFiatDatesByCurrency);
            if (sourceRateToUsd == 0) return 0; // Skip if no rate available to avoid mixing currencies
        }

        // Get target rate from USD (if target is not USD)
        var targetRateFromUsd = 1m;
        if (targetCurrency.Code != FiatCurrency.Usd.Code)
        {
            targetRateFromUsd = GetFiatRateAt(date, targetCurrency.Code, fiatRates, sortedFiatDatesByCurrency);
            if (targetRateFromUsd == 0) return 0; // Skip if no rate available to avoid mixing currencies
        }

        // Convert: source -> USD -> target
        // amount / sourceRateToUsd gives us USD value
        // multiply by targetRateFromUsd to get target currency
        return targetRateFromUsd * (amount / sourceRateToUsd);
    }

    private decimal ConvertBtcToTarget(
        decimal btcAmount,
        FiatCurrency targetCurrency,
        DateOnly date,
        Dictionary<DateOnly, BitcoinDataEntity> btcRates,
        List<DateOnly> sortedBtcDates,
        Dictionary<DateOnly, List<FiatDataEntity>> fiatRates,
        Dictionary<string, List<DateOnly>> sortedFiatDatesByCurrency)
    {
        // Get BTC price in USD
        var usdBitcoinPrice = GetUsdBitcoinPriceAt(date, btcRates, sortedBtcDates);
        if (usdBitcoinPrice == 0) return 0;

        // Convert BTC to USD
        var usdValue = btcAmount * usdBitcoinPrice;

        // Get target currency rate from USD
        var targetRateFromUsd = GetFiatRateAt(date, targetCurrency.Code, fiatRates, sortedFiatDatesByCurrency);
        if (targetRateFromUsd == 0) return usdValue; // Fallback to USD if no rate

        return targetRateFromUsd * usdValue;
    }

    private decimal GetFiatRateAt(
        DateOnly date,
        string currencyCode,
        Dictionary<DateOnly, List<FiatDataEntity>> fiatRates,
        Dictionary<string, List<DateOnly>> sortedFiatDatesByCurrency)
    {
        if (currencyCode == FiatCurrency.Usd.Code)
            return 1m;

        if (!sortedFiatDatesByCurrency.TryGetValue(currencyCode, out var sortedDates) || sortedDates.Count == 0)
            return 0m;

        var targetDate = date < CutoffDate ? sortedDates[0] : FindClosestDate(sortedDates, date);

        if (fiatRates.TryGetValue(targetDate, out var rates))
        {
            var entry = rates.FirstOrDefault(x => x.Currency == currencyCode);
            if (entry is not null)
                return entry.Price;
        }

        return 0m;
    }

    private decimal GetUsdBitcoinPriceAt(
        DateOnly date,
        Dictionary<DateOnly, BitcoinDataEntity> btcRates,
        List<DateOnly> sortedBtcDates)
    {
        if (sortedBtcDates.Count == 0)
            return 0m;

        var targetDate = date < CutoffDate ? sortedBtcDates[0] : FindClosestDate(sortedBtcDates, date);

        if (btcRates.TryGetValue(targetDate, out var btcRate))
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
}
