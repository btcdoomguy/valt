using Valt.Core.Modules.Budget.Accounts;

namespace Valt.Core.Modules.Budget.Transactions.Details;

public abstract record TransactionDetails
{
    public abstract TransactionTypes TransactionType { get; }
    public abstract TransactionTransferTypes TransferType { get; }
    public abstract AccountId FromAccountId { get; }
    public abstract AccountId? ToAccountId { get; }
    public abstract decimal? FromAccountFiatValue { get; }
    public abstract long? FromAccountSatsValue { get; }
    public abstract decimal? ToAccountFiatValue { get; }
    public abstract long? ToAccountSatsValue { get; }
    public abstract bool EligibleToAutoSatAmount { get; }
}