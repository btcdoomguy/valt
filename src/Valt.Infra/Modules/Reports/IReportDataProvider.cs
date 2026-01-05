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
    FrozenDictionary<DateTime, ImmutableList<TransactionEntity>> TransactionsByDate { get; }
    FrozenDictionary<DateTime, ImmutableList<ObjectId>> AccountsByDate { get; }
    FrozenDictionary<DateTime, BitcoinDataEntity> BtcRates { get; }
    FrozenDictionary<DateTime, ImmutableList<FiatDataEntity>> FiatRates { get; }

    ImmutableList<TransactionEntity> AllTransactions { get; }
    DateTime MinTransactionDate { get; }
    DateTime MaxTransactionDate { get; }

    decimal GetFiatRateAt(DateTime date, FiatCurrency currency);
    decimal GetUsdBitcoinPriceAt(DateTime date);
}
