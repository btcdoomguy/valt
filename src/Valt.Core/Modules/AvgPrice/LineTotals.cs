using Valt.Core.Common;

namespace Valt.Core.Modules.AvgPrice;

public record LineTotals(FiatValue AvgCostOfAcquisition, decimal TotalCost, decimal Quantity)
{
    public static LineTotals Empty => new LineTotals(FiatValue.Empty, FiatValue.Empty, 0);
}