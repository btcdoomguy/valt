namespace Valt.Core.Modules.AvgPrice.CalculationStrategies;

public interface IAvgPriceCalculationStrategy
{
    void CalculateTotals(IEnumerable<AvgPriceLine> orderedLines);
}