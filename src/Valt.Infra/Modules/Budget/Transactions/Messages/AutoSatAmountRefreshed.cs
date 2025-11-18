using Valt.Core.Common;
using Valt.Core.Modules.Budget.Transactions;

namespace Valt.Infra.Modules.Budget.Transactions.Messages;

public record AutoSatAmountRefreshed(IReadOnlyList<AutoSatAmountRefreshedTransaction> Transactions);

public record AutoSatAmountRefreshedTransaction(TransactionId TransactionId, BtcValue AutoSatAmount);