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
    public FrozenDictionary<DateOnly, ImmutableList<TransactionEntity>> TransactionsByDate { get; }
    public FrozenDictionary<DateOnly, ImmutableList<ObjectId>> AccountsByDate { get; }
    public FrozenDictionary<DateOnly, BitcoinDataEntity> BtcRates { get; }
    public FrozenDictionary<DateOnly, ImmutableList<FiatDataEntity>> FiatRates { get; }
    public ImmutableList<TransactionEntity> AllTransactions { get; }
    public DateOnly MinTransactionDate { get; }
    public DateOnly MaxTransactionDate { get; }

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

        // Load rates with buffer for lookups
        var rateMinDate = MinTransactionDate.AddDays(-MaxDaysToScanForRate).ToValtDateTime();
        var rateMaxDate = MaxTransactionDate.ToValtDateTime();
        var btcRates = priceDatabase.GetBitcoinData()
            .Find(x => x.Date >= rateMinDate && x.Date <= rateMaxDate)
            .ToImmutableList();
        BtcRates = btcRates.ToFrozenDictionary(x => DateOnly.FromDateTime(x.Date.ToUniversalTime()));

        var fiatRates = priceDatabase.GetFiatData()
            .Find(x => x.Date >= rateMinDate && x.Date <= rateMaxDate)
            .ToImmutableList();
        FiatRates = fiatRates
            .GroupBy(x => DateOnly.FromDateTime(x.Date.ToUniversalTime()))
            .ToFrozenDictionary(x => x.Key, x => x.ToImmutableList());
    }

    public decimal GetFiatRateAt(DateOnly date, FiatCurrency currency)
    {
        if (currency == FiatCurrency.Usd)
        {
            return 1;
        }

        var scanDate = date;
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

    public decimal GetUsdBitcoinPriceAt(DateOnly date)
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
