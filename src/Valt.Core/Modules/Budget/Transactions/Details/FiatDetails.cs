using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;

namespace Valt.Core.Modules.Budget.Transactions.Details;

public record FiatDetails : TransactionDetails
{
    public AccountId FiatAccountId { get; }
    public FiatValue Amount { get; }

    public bool Credit { get; }
    /*public bool IsAutoSatAmount { get; }
    public SatAmountState SatAmountState { get; }
    public BtcValue? SatAmount { get; }*/

    public FiatDetails(
        AccountId fiatAccountId, FiatValue amount, bool credit)
    {
        FiatAccountId = fiatAccountId;
        Amount = amount;
        Credit = credit;
    }

    public override TransactionTypes TransactionType => Credit ? TransactionTypes.Credit : TransactionTypes.Debt;
    public override TransactionTransferTypes TransferType => TransactionTransferTypes.Fiat;
    public override AccountId FromAccountId => FiatAccountId;
    public override AccountId? ToAccountId => null;

    public override decimal? FromAccountFiatValue => Credit ? Amount.Value : Amount.Value * -1;
    public override long? FromAccountSatsValue => null;
    public override decimal? ToAccountFiatValue => null;
    public override long? ToAccountSatsValue => null;
    public override bool EligibleToAutoSatAmount => true;
}