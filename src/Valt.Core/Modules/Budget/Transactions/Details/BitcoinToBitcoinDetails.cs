using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;

namespace Valt.Core.Modules.Budget.Transactions.Details;

public record BitcoinToBitcoinDetails : TransactionDetails
{
    public AccountId FromBtcAccountId { get; }
    public AccountId ToBtcAccountId { get; }
    public BtcValue Amount { get; }

    public BitcoinToBitcoinDetails(
        AccountId fromBtcAccountId, AccountId toBtcAccountId, BtcValue amount)
    {
        if (fromBtcAccountId == toBtcAccountId)
            throw new ArgumentException("Cannot create transfer to same account");

        FromBtcAccountId = fromBtcAccountId;
        ToBtcAccountId = toBtcAccountId;
        Amount = amount;
    }

    public override TransactionTypes TransactionType => TransactionTypes.Transfer;
    public override TransactionTransferTypes TransferType => TransactionTransferTypes.BitcoinToBitcoin;
    public override AccountId FromAccountId => FromBtcAccountId;
    public override AccountId? ToAccountId => ToBtcAccountId;

    public override decimal? FromAccountFiatValue => null;
    public override long? FromAccountSatsValue => Amount.Sats * -1;
    public override decimal? ToAccountFiatValue => null;
    public override long? ToAccountSatsValue => Amount.Sats;
    public override bool EligibleToAutoSatAmount => false;
}