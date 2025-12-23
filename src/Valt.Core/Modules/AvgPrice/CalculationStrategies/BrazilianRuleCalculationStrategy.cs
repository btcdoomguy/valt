using Valt.Core.Common;

namespace Valt.Core.Modules.AvgPrice.CalculationStrategies;

internal class BrazilianRuleCalculationStrategy : IAvgPriceCalculationStrategy
{
    private readonly AvgPriceProfile _profile;

    public BrazilianRuleCalculationStrategy(AvgPriceProfile profile)
    {
        _profile = profile;
    }
    
    public void CalculateTotals(IEnumerable<AvgPriceLine> orderedLines)
    {
        var totalCost = 0m;
        var quantity = 0m;
        var avg = 0m;

        foreach (var line in orderedLines)
        {
            if (line.Type == AvgPriceLineTypes.Buy)
            {
                totalCost += Math.Round(line.Quantity * line.UnitPrice.Value, _profile.Asset.Precision);
                quantity += line.Quantity;
                avg = quantity > 0 ? Math.Round(totalCost / quantity, _profile.Asset.Precision) : 0m;
            }
            else if (line.Type == AvgPriceLineTypes.Sell)
            {
                //Reduce total proportionally
                var proportionSold = line.Quantity / quantity;
                totalCost -= Math.Round(totalCost * proportionSold, _profile.Asset.Precision);
                quantity -= line.Quantity;
                avg = quantity > 0 ? Math.Round(totalCost / quantity, _profile.Asset.Precision) : 0m;
            }
            else
            {
                //Setup just overrides the current quantity and avg price
                quantity = line.Quantity;
                avg = line.UnitPrice.Value;
                totalCost = Math.Round(quantity * avg, _profile.Asset.Precision);
            }

            _profile.ChangeLineTotals(line, new LineTotals(FiatValue.New(avg), totalCost, quantity));
        }
    }
}