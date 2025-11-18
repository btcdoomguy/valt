using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;

namespace Valt.Core.Modules.Budget.Transactions.Details;

public record FiatToFiatDetails : TransactionDetails
{
    public AccountId FromFiatAccountId { get; }
    public AccountId ToFiatAccountId { get; }
    public FiatValue FromAmount { get; }

    public FiatValue ToAmount { get; }

    public FiatToFiatDetails(
        AccountId fromFiatAccountId, AccountId toFiatAccountId, FiatValue fromAmount, FiatValue toAmount)
    {
        if (fromFiatAccountId == toFiatAccountId)
            throw new ArgumentException("Cannot create transfer to same account");

        FromFiatAccountId = fromFiatAccountId;
        ToFiatAccountId = toFiatAccountId;
        FromAmount = fromAmount;
        ToAmount = toAmount;
    }

    public override TransactionTypes TransactionType => TransactionTypes.Transfer;
    public override TransactionTransferTypes TransferType => TransactionTransferTypes.FiatToFiat;
    public override AccountId FromAccountId => FromFiatAccountId;
    public override AccountId? ToAccountId => ToFiatAccountId;

    public override decimal? FromAccountFiatValue => FromAmount.Value * -1;
    public override long? FromAccountSatsValue => null;
    public override decimal? ToAccountFiatValue => ToAmount.Value;
    public override long? ToAccountSatsValue => null;
    public override bool EligibleToAutoSatAmount => true;
}