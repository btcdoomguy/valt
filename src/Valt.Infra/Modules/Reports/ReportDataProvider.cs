using System.Collections.Frozen;
using System.Collections.Immutable;
using LiteDB;
using Valt.Core.Common;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Infra.DataAccess;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.Budget.Categories;
using Valt.Infra.Modules.Budget.Transactions;
using Valt.Infra.Modules.DataSources.Bitcoin;
using Valt.Infra.Modules.DataSources.Fiat;

namespace Valt.Infra.Modules.Reports;

public interface IReportDataProviderFactory
{
    IReportDataProvider Create();
}

internal class ReportDataProviderFactory : IReportDataProviderFactory
{
    private readonly IPriceDatabase _priceDatabase;
    private readonly ILocalDatabase _localDatabase;
    private readonly IClock _clock;

    public ReportDataProviderFactory(IPriceDatabase priceDatabase, ILocalDatabase localDatabase, IClock clock)
    {
        _priceDatabase = priceDatabase;
        _localDatabase = localDatabase;
        _clock = clock;
    }

    public IReportDataProvider Create()
    {
        return new ReportDataProvider(_priceDatabase, _localDatabase, _clock);
    }
}

internal class ReportDataProvider : IReportDataProvider
{
    private static readonly DateOnly CutoffDate = new(2010, 1, 1);

    public FrozenDictionary<ObjectId, AccountEntity> Accounts { get; }
    public FrozenDictionary<ObjectId, CategoryEntity> Categories { get; }
    public FrozenDictionary<DateOnly, ImmutableList<TransactionEntity>> TransactionsByDate { get; }
    public FrozenDictionary<DateOnly, ImmutableList<ObjectId>> AccountsByDate { get; }
    public FrozenDictionary<DateOnly, BitcoinDataEntity> BtcRates { get; }
    public FrozenDictionary<DateOnly, ImmutableList<FiatDataEntity>> FiatRates { get; }
    public ImmutableList<TransactionEntity> AllTransactions { get; }
    public DateOnly MinTransactionDate { get; }
    public DateOnly MaxTransactionDate { get; }

    private readonly ImmutableArray<DateOnly> _sortedBtcDates;
    private readonly FrozenDictionary<string, ImmutableArray<DateOnly>> _sortedFiatDatesByCurrency;

    public ReportDataProvider(IPriceDatabase priceDatabase, ILocalDatabase localDatabase, IClock clock)
    {
        // Load all accounts
        var accounts = localDatabase.GetAccounts().FindAll().ToImmutableList();
        Accounts = accounts.ToFrozenDictionary(x => x.Id);

        // Load all categories
        var categories = localDatabase.GetCategories().FindAll().ToImmutableList();
        Categories = categories.ToFrozenDictionary(x => x.Id);

        // Load all transactions
        AllTransactions = localDatabase.GetTransactions().FindAll().ToImmutableList();

        if (AllTransactions.Count == 0)
        {
            // No transactions - set empty collections
            MinTransactionDate = clock.GetCurrentLocalDate();
            MaxTransactionDate = clock.GetCurrentLocalDate();
            TransactionsByDate = FrozenDictionary<DateOnly, ImmutableList<TransactionEntity>>.Empty;
            AccountsByDate = FrozenDictionary<DateOnly, ImmutableList<ObjectId>>.Empty;
            BtcRates = FrozenDictionary<DateOnly, BitcoinDataEntity>.Empty;
            FiatRates = FrozenDictionary<DateOnly, ImmutableList<FiatDataEntity>>.Empty;
            _sortedBtcDates = [];
            _sortedFiatDatesByCurrency = FrozenDictionary<string, ImmutableArray<DateOnly>>.Empty;
            return;
        }

        // Determine date range - extract DateOnly from UTC to ensure consistency
        MinTransactionDate = AllTransactions.Min(x => DateOnly.FromDateTime(x.Date.ToUniversalTime()));
        MaxTransactionDate = clock.GetCurrentLocalDate();

        // Build transaction indexes - group by DateOnly extracted from UTC
        TransactionsByDate = AllTransactions
            .GroupBy(x => DateOnly.FromDateTime(x.Date.ToUniversalTime()))
            .ToFrozenDictionary(x => x.Key, x => x.ToImmutableList());

        AccountsByDate = TransactionsByDate.ToFrozenDictionary(
            x => x.Key,
            x => x.Value
                .SelectMany(y => new[] { y.FromAccountId, y.ToAccountId ?? ObjectId.Empty })
                .Where(y => y != ObjectId.Empty)
                .Distinct()
                .ToImmutableList());

        // Load all rates for flexible lookups
        var btcRates = priceDatabase.GetBitcoinData()
            .FindAll()
            .ToImmutableList();
        BtcRates = btcRates.ToFrozenDictionary(x => DateOnly.FromDateTime(x.Date.ToUniversalTime()));
        _sortedBtcDates = BtcRates.Keys.Order().ToImmutableArray();

        var fiatRates = priceDatabase.GetFiatData()
            .FindAll()
            .ToImmutableList();
        FiatRates = fiatRates
            .GroupBy(x => DateOnly.FromDateTime(x.Date.ToUniversalTime()))
            .ToFrozenDictionary(x => x.Key, x => x.ToImmutableList());

        // Build sorted date indexes per currency for efficient lookups
        _sortedFiatDatesByCurrency = fiatRates
            .GroupBy(x => x.Currency)
            .ToFrozenDictionary(
                g => g.Key,
                g => g.Select(x => DateOnly.FromDateTime(x.Date.ToUniversalTime()))
                    .Distinct()
                    .Order()
                    .ToImmutableArray());
    }

    public decimal GetFiatRateAt(DateOnly date, FiatCurrency currency)
    {
        if (currency == FiatCurrency.Usd)
        {
            return 1;
        }

        var currencyCode = currency.Code;

        if (!_sortedFiatDatesByCurrency.TryGetValue(currencyCode, out var sortedDates) || sortedDates.Length == 0)
        {
            throw new ApplicationException($"No fiat rate data available for {currencyCode}");
        }

        var targetDate = date < CutoffDate ? sortedDates[0] : FindClosestDate(sortedDates, date);

        if (FiatRates.TryGetValue(targetDate, out var rates))
        {
            var entry = rates.FirstOrDefault(x => x.Currency == currencyCode);
            if (entry is not null)
            {
                return entry.Price;
            }
        }

        throw new ApplicationException($"Could not find fiat rate for {currencyCode} on {date}");
    }

    public decimal GetUsdBitcoinPriceAt(DateOnly date)
    {
        if (_sortedBtcDates.Length == 0)
        {
            throw new ApplicationException("No BTC rate data available");
        }

        var targetDate = date < CutoffDate ? _sortedBtcDates[0] : FindClosestDate(_sortedBtcDates, date);

        if (BtcRates.TryGetValue(targetDate, out var btcRate))
        {
            return btcRate.Price;
        }

        throw new ApplicationException($"Could not find BTC rate on {date}");
    }

    /// <summary>
    /// Finds the closest date on or before the target date using binary search.
    /// If the target date is before all available dates, returns the first available date.
    /// </summary>
    private static DateOnly FindClosestDate(ImmutableArray<DateOnly> sortedDates, DateOnly targetDate)
    {
        var index = sortedDates.BinarySearch(targetDate);

        if (index >= 0)
        {
            // Exact match found
            return sortedDates[index];
        }

        // BinarySearch returns ~index where index is the first element larger than target
        var insertionPoint = ~index;

        if (insertionPoint == 0)
        {
            // Target is before all dates, return the first available
            return sortedDates[0];
        }

        // Return the date just before the insertion point (last date <= target)
        return sortedDates[insertionPoint - 1];
    }
}
