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
    private const int MaxDaysToScanForRate = 10;

    public FrozenDictionary<ObjectId, AccountEntity> Accounts { get; }
    public FrozenDictionary<ObjectId, CategoryEntity> Categories { get; }
    public FrozenDictionary<DateTime, ImmutableList<TransactionEntity>> TransactionsByDate { get; }
    public FrozenDictionary<DateTime, ImmutableList<ObjectId>> AccountsByDate { get; }
    public FrozenDictionary<DateTime, BitcoinDataEntity> BtcRates { get; }
    public FrozenDictionary<DateTime, ImmutableList<FiatDataEntity>> FiatRates { get; }
    public ImmutableList<TransactionEntity> AllTransactions { get; }
    public DateTime MinTransactionDate { get; }
    public DateTime MaxTransactionDate { get; }

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
            MinTransactionDate = clock.GetCurrentLocalDate().ToValtDateTime();
            MaxTransactionDate = clock.GetCurrentLocalDate().ToValtDateTime();
            TransactionsByDate = FrozenDictionary<DateTime, ImmutableList<TransactionEntity>>.Empty;
            AccountsByDate = FrozenDictionary<DateTime, ImmutableList<ObjectId>>.Empty;
            BtcRates = FrozenDictionary<DateTime, BitcoinDataEntity>.Empty;
            FiatRates = FrozenDictionary<DateTime, ImmutableList<FiatDataEntity>>.Empty;
            return;
        }

        // Determine date range
        MinTransactionDate = AllTransactions.Min(x => x.Date);
        var currentDate = clock.GetCurrentLocalDate().ToValtDateTime();
        MaxTransactionDate = currentDate;

        // Build transaction indexes
        TransactionsByDate = AllTransactions
            .GroupBy(x => x.Date)
            .ToFrozenDictionary(x => x.Key, x => x.ToImmutableList());

        AccountsByDate = TransactionsByDate.ToFrozenDictionary(
            x => x.Key,
            x => x.Value
                .SelectMany(y => new[] { y.FromAccountId, y.ToAccountId ?? ObjectId.Empty })
                .Where(y => y != ObjectId.Empty)
                .Distinct()
                .ToImmutableList());

        // Load rates with buffer for lookups
        var rateMinDate = MinTransactionDate.AddDays(-MaxDaysToScanForRate);
        var btcRates = priceDatabase.GetBitcoinData()
            .Find(x => x.Date >= rateMinDate && x.Date <= MaxTransactionDate)
            .ToImmutableList();
        BtcRates = btcRates.ToFrozenDictionary(x => x.Date);

        var fiatRates = priceDatabase.GetFiatData()
            .Find(x => x.Date >= rateMinDate && x.Date <= MaxTransactionDate)
            .ToImmutableList();
        FiatRates = fiatRates
            .GroupBy(x => x.Date)
            .ToFrozenDictionary(x => x.Key, x => x.ToImmutableList());
    }

    public decimal GetFiatRateAt(DateTime date, FiatCurrency currency)
    {
        if (currency == FiatCurrency.Usd)
        {
            return 1;
        }

        var scanDate = date.Date;
        var currencyCode = currency.Code;

        for (var i = 0; i < MaxDaysToScanForRate; i++)
        {
            if (FiatRates.TryGetValue(scanDate, out var rates))
            {
                var entry = rates.FirstOrDefault(x => x.Currency == currencyCode);
                if (entry is not null)
                {
                    return entry.Price;
                }
            }

            scanDate = scanDate.AddDays(-1);
        }

        throw new ApplicationException($"Could not find fiat rate for {currencyCode} on {date}");
    }

    public decimal GetUsdBitcoinPriceAt(DateTime date)
    {
        var scanDate = date;

        for (var i = 0; i < MaxDaysToScanForRate; i++)
        {
            if (BtcRates.TryGetValue(scanDate, out var btcRate))
            {
                return btcRate.Price;
            }

            scanDate = scanDate.AddDays(-1);
        }

        throw new ApplicationException($"Could not find BTC rate on {date}");
    }
}
