using Valt.Core.Common;

namespace Valt.Core.Modules.Budget.Transactions.Details;

public record AutoSatAmountDetails(
    bool IsAutoSatAmount,
    SatAmountState SatAmountState,
    BtcValue? SatAmount)
{
    public static AutoSatAmountDetails Pending => new AutoSatAmountDetails(true, SatAmountState.Pending, null);
}