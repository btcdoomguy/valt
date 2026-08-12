using System.Collections.Frozen;
using System.Collections.Immutable;
using LiteDB;
using Valt.Core.Common;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.Budget.Categories;
using Valt.Infra.Modules.Budget.Transactions;
using Valt.Infra.Modules.DataSources.Bitcoin;
using Valt.Infra.Modules.DataSources.Fiat;

namespace Valt.Infra.Modules.Reports;

public class AccountFilteredReportDataProvider : IReportDataProvider
{
    private readonly IReportDataProvider _inner;
    private readonly FrozenSet<ObjectId> _accountIds;

    public AccountFilteredReportDataProvider(IReportDataProvider inner, IEnumerable<ObjectId> accountIds)
    {
        _inner = inner;
        _accountIds = accountIds.ToFrozenSet();

        Accounts = _inner.Accounts
            .Where(kv => _accountIds.Contains(kv.Key))
            .ToFrozenDictionary(kv => kv.Key, kv => kv.Value);

        AllTransactions = _inner.AllTransactions
            .Where(IsRelevant)
            .ToImmutableList();

        TransactionsByDate = _inner.TransactionsByDate
            .Select(kv => (kv.Key, Transactions: kv.Value.Where(IsRelevant).ToImmutableList()))
            .Where(x => x.Transactions.Count > 0)
            .ToFrozenDictionary(x => x.Key, x => x.Transactions);

        AccountsByDate = _inner.AccountsByDate
            .Select(kv => (kv.Key, Accounts: kv.Value.Where(id => _accountIds.Contains(id)).ToImmutableList()))
            .Where(x => x.Accounts.Count > 0)
            .ToFrozenDictionary(x => x.Key, x => x.Accounts);
    }

    private bool IsRelevant(TransactionEntity transaction) =>
        _accountIds.Contains(transaction.FromAccountId)
        || (transaction.ToAccountId is { } toAccountId && _accountIds.Contains(toAccountId));

    private DateOnly ComputeMinTransactionDate()
    {
        var minDate = _inner.MinTransactionDate;
        foreach (var tx in AllTransactions)
        {
            var date = DateOnly.FromDateTime(tx.Date.ToUniversalTime());
            if (date < minDate) minDate = date;
        }
        return minDate;
    }

    public FrozenDictionary<ObjectId, AccountEntity> Accounts { get; }
    public FrozenDictionary<ObjectId, CategoryEntity> Categories => _inner.Categories;
    public FrozenDictionary<DateOnly, ImmutableList<TransactionEntity>> TransactionsByDate { get; }
    public FrozenDictionary<DateOnly, ImmutableList<ObjectId>> AccountsByDate { get; }
    public FrozenDictionary<DateOnly, BitcoinDataEntity> BtcRates => _inner.BtcRates;
    public FrozenDictionary<DateOnly, ImmutableList<FiatDataEntity>> FiatRates => _inner.FiatRates;

    public ImmutableList<TransactionEntity> AllTransactions { get; }

    public DateOnly MinTransactionDate => AllTransactions.Count > 0
        ? ComputeMinTransactionDate()
        : _inner.MinTransactionDate;

    public DateOnly MaxTransactionDate => _inner.MaxTransactionDate;

    public decimal GetFiatRateAt(DateOnly date, FiatCurrency currency) => _inner.GetFiatRateAt(date, currency);

    public decimal GetUsdBitcoinPriceAt(DateOnly date) => _inner.GetUsdBitcoinPriceAt(date);
}
