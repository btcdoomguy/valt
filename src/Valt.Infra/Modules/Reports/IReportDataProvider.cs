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

public interface IReportDataProvider
{
    FrozenDictionary<ObjectId, AccountEntity> Accounts { get; }
    FrozenDictionary<ObjectId, CategoryEntity> Categories { get; }
    FrozenDictionary<DateOnly, ImmutableList<TransactionEntity>> TransactionsByDate { get; }
    FrozenDictionary<DateOnly, ImmutableList<ObjectId>> AccountsByDate { get; }
    FrozenDictionary<DateOnly, BitcoinDataEntity> BtcRates { get; }
    FrozenDictionary<DateOnly, ImmutableList<FiatDataEntity>> FiatRates { get; }

    ImmutableList<TransactionEntity> AllTransactions { get; }
    DateOnly MinTransactionDate { get; }
    DateOnly MaxTransactionDate { get; }

    decimal GetFiatRateAt(DateOnly date, FiatCurrency currency);
    decimal GetUsdBitcoinPriceAt(DateOnly date);
}
