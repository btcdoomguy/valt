namespace Valt.Core.Modules.AvgPrice.CalculationStrategies;

public interface IAvgPriceCalculationStrategy
{
    public AvgPriceCalculationMethod Method { get; }
    void CalculateTotals(IEnumerable<AvgPriceLine> orderedLines);
}