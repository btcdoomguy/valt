using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;

namespace Valt.Core.Modules.Budget.Transactions.Details;

public record FiatToBitcoinDetails : TransactionDetails
{
    public AccountId FromFiatAccountId { get; }
    public AccountId ToBtcAccountId { get; }
    public FiatValue FromAmount { get; }
    public BtcValue ToAmount { get; }

    public FiatValue BtcPrice => BtcPriceCalculator.CalculateBtcPrice(FromAmount, ToAmount);

    public FiatToBitcoinDetails(
        AccountId fromFiatAccountId, AccountId toBtcAccountId, FiatValue fromAmount, BtcValue toAmount)
    {
        FromFiatAccountId = fromFiatAccountId;
        ToBtcAccountId = toBtcAccountId;
        FromAmount = fromAmount;
        ToAmount = toAmount;
    }

    public override TransactionTypes TransactionType => TransactionTypes.Transfer;
    public override TransactionTransferTypes TransferType => TransactionTransferTypes.FiatToBitcoin;
    public override AccountId FromAccountId => FromFiatAccountId;
    public override AccountId? ToAccountId => ToBtcAccountId;

    public override decimal? FromAccountFiatValue => FromAmount.Value * -1;
    public override long? FromAccountSatsValue => null;
    public override decimal? ToAccountFiatValue => null;
    public override long? ToAccountSatsValue => ToAmount.Sats;
    public override bool EligibleToAutoSatAmount => false;
}