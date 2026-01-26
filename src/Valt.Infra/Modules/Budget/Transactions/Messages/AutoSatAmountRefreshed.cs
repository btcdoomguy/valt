using Valt.Core.Common;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Infra.Kernel.Notifications;

namespace Valt.Infra.Modules.Budget.Transactions.Messages;

public record AutoSatAmountRefreshed(IReadOnlyList<AutoSatAmountRefreshedTransaction> Transactions) : INotification;

public record AutoSatAmountRefreshedTransaction(TransactionId TransactionId, BtcValue AutoSatAmount);