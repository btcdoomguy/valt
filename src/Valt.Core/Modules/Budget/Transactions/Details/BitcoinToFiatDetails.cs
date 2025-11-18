using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;

namespace Valt.Core.Modules.Budget.Transactions.Details;

public record BitcoinToFiatDetails : TransactionDetails
{
    public AccountId FromBtcAccountId { get; }
    public AccountId ToFiatAccountId { get; }
    public BtcValue FromAmount { get; }
    public FiatValue ToAmount { get; }
    public FiatValue BtcPrice => BtcPriceCalculator.CalculateBtcPrice(ToAmount, FromAmount);

    public BitcoinToFiatDetails(
        AccountId fromBtcAccountId, AccountId toFiatAccountId, BtcValue fromAmount, FiatValue toAmount)
    {
        FromBtcAccountId = fromBtcAccountId;
        ToFiatAccountId = toFiatAccountId;
        FromAmount = fromAmount;
        ToAmount = toAmount;
    }

    public override TransactionTypes TransactionType => TransactionTypes.Transfer;
    public override TransactionTransferTypes TransferType => TransactionTransferTypes.BitcoinToFiat;
    public override AccountId FromAccountId => FromBtcAccountId;
    public override AccountId? ToAccountId => ToFiatAccountId;

    public override decimal? FromAccountFiatValue => null;
    public override long? FromAccountSatsValue => FromAmount.Sats * -1;
    public override decimal? ToAccountFiatValue => ToAmount.Value;
    public override long? ToAccountSatsValue => null;
    public override bool EligibleToAutoSatAmount => false;
}