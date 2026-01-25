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
    Task<IReportDataProvider> CreateAsync(bool forceRefresh = false, CancellationToken ct = default);
}

internal class ReportDataProviderFactory : IReportDataProviderFactory
{
    private readonly IPriceDatabase _priceDatabase;
    private readonly ILocalDatabase _localDatabase;
    private readonly IClock _clock;
    private IReportDataProvider? _cachedProvider;
    private int _lastTransactionCount;
    private readonly Lock _lock = new();

    public ReportDataProviderFactory(IPriceDatabase priceDatabase, ILocalDatabase localDatabase, IClock clock)
    {
        _priceDatabase = priceDatabase;
        _localDatabase = localDatabase;
        _clock = clock;
    }

    public async Task<IReportDataProvider> CreateAsync(bool forceRefresh = false, CancellationToken ct = default)
    {
        // Quick cache check (lightweight query)
        if (!forceRefresh && _cachedProvider != null)
        {
            var currentCount = _localDatabase.GetTransactions().Count();
            if (currentCount == _lastTransactionCount)
                return _cachedProvider;
        }

        // Load all database collections in parallel
        var accountsTask = Task.Run(() => _localDatabase.GetAccounts().FindAll().ToImmutableList(), ct);
        var categoriesTask = Task.Run(() => _localDatabase.GetCategories().FindAll().ToImmutableList(), ct);
        var transactionsTask = Task.Run(() => _localDatabase.GetTransactions().FindAll().ToImmutableList(), ct);
        var btcRatesTask = Task.Run(() => _priceDatabase.GetBitcoinData().FindAll().ToImmutableList(), ct);
        var fiatRatesTask = Task.Run(() => _priceDatabase.GetFiatData().FindAll().ToImmutableList(), ct);

        await Task.WhenAll(accountsTask, categoriesTask, transactionsTask, btcRatesTask, fiatRatesTask);

        var provider = new ReportDataProvider(
            await accountsTask, await categoriesTask, await transactionsTask,
            await btcRatesTask, await fiatRatesTask, _clock);

        lock (_lock)
        {
            _cachedProvider = provider;
            _lastTransactionCount = provider.AllTransactions.Count;
        }

        return provider;
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

    public ReportDataProvider(
        ImmutableList<AccountEntity> accounts,
        ImmutableList<CategoryEntity> categories,
        ImmutableList<TransactionEntity> transactions,
        ImmutableList<BitcoinDataEntity> btcRates,
        ImmutableList<FiatDataEntity> fiatRates,
        IClock clock)
    {
        Accounts = accounts.ToFrozenDictionary(x => x.Id);
        Categories = categories.ToFrozenDictionary(x => x.Id);
        AllTransactions = transactions;

        if (transactions.Count == 0)
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

        // Single-pass index building for transactions
        var transactionsByDate = new Dictionary<DateOnly, List<TransactionEntity>>();
        var accountsByDate = new Dictionary<DateOnly, HashSet<ObjectId>>();
        var minDate = DateOnly.MaxValue;

        foreach (var tx in transactions)
        {
            var date = DateOnly.FromDateTime(tx.Date.ToUniversalTime());
            if (date < minDate) minDate = date;

            if (!transactionsByDate.TryGetValue(date, out var txList))
            {
                txList = new List<TransactionEntity>();
                transactionsByDate[date] = txList;
                accountsByDate[date] = new HashSet<ObjectId>();
            }
            txList.Add(tx);
            accountsByDate[date].Add(tx.FromAccountId);
            if (tx.ToAccountId is { } toAccountId)
                accountsByDate[date].Add(toAccountId);
        }

        MinTransactionDate = minDate;
        MaxTransactionDate = clock.GetCurrentLocalDate();
        TransactionsByDate = transactionsByDate.ToFrozenDictionary(k => k.Key, k => k.Value.ToImmutableList());
        AccountsByDate = accountsByDate.ToFrozenDictionary(k => k.Key,
            k => k.Value.Where(id => id != ObjectId.Empty).ToImmutableList());

        // BTC and fiat rates indexing
        BtcRates = btcRates.ToFrozenDictionary(x => DateOnly.FromDateTime(x.Date.ToUniversalTime()));
        _sortedBtcDates = BtcRates.Keys.Order().ToImmutableArray();

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

    /// <summary>
    /// Convenience constructor that loads data synchronously from databases.
    /// Primarily used for testing. Production code should use the factory with pre-loaded data.
    /// </summary>
    public ReportDataProvider(IPriceDatabase priceDatabase, ILocalDatabase localDatabase, IClock clock)
        : this(
            localDatabase.GetAccounts().FindAll().ToImmutableList(),
            localDatabase.GetCategories().FindAll().ToImmutableList(),
            localDatabase.GetTransactions().FindAll().ToImmutableList(),
            priceDatabase.GetBitcoinData().FindAll().ToImmutableList(),
            priceDatabase.GetFiatData().FindAll().ToImmutableList(),
            clock)
    {
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
