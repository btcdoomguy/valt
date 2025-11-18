using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;

namespace Valt.Core.Modules.Budget.Transactions.Details;

public record BitcoinDetails : TransactionDetails
{
    public AccountId BtcAccountId { get; }
    public BtcValue Amount { get; }
    public bool Credit { get; }

    public BitcoinDetails(
        AccountId btcAccountId, BtcValue amount, bool credit)
    {
        BtcAccountId = btcAccountId;
        Amount = amount;
        Credit = credit;
    }

    public override TransactionTypes TransactionType => Credit ? TransactionTypes.Credit : TransactionTypes.Debt;
    public override TransactionTransferTypes TransferType => TransactionTransferTypes.Bitcoin;
    public override AccountId FromAccountId => BtcAccountId;
    public override AccountId? ToAccountId => null;

    public override decimal? FromAccountFiatValue => null;
    public override long? FromAccountSatsValue => Credit ? Amount.Sats : Amount.Sats * -1;
    public override decimal? ToAccountFiatValue => null;
    public override long? ToAccountSatsValue => null;
    public override bool EligibleToAutoSatAmount => false;
}