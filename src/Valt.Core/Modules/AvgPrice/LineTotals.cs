using Valt.Core.Common;

namespace Valt.Core.Modules.AvgPrice;

public record LineTotals(FiatValue AvgCostOfAcquisition, decimal TotalCost, BtcValue BtcAmount)
{
    public static LineTotals Empty => new LineTotals(FiatValue.Empty, FiatValue.Empty, BtcValue.Empty);
}