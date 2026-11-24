using Valt.Core.Common;

namespace Valt.Core.Modules.AvgPrice.CalculationStrategies;

internal class FifoCalculationStrategy : IAvgPriceCalculationStrategy
{
    private readonly AvgPriceProfile _profile;

    public FifoCalculationStrategy(AvgPriceProfile profile)
    {
        _profile = profile;
    }

    public AvgPriceCalculationMethod Method => AvgPriceCalculationMethod.Fifo;

    public void CalculateTotals(IEnumerable<AvgPriceLine> orderedLines)
    {
        // LinkedList of cost lots: (quantity, unitPrice)
        var lots = new LinkedList<CostLot>();
        decimal runningTotalCost = 0m;
        decimal runningQuantity = 0m;

        decimal LotCost(decimal qty, decimal unitPrice) => Math.Round(qty * unitPrice, _profile.Asset.Precision);

        foreach (var line in orderedLines)
        {
            if (line.Type == AvgPriceLineTypes.Buy)
            {
                // Add a new lot at the end of the list
                var lot = new CostLot(line.Quantity, line.UnitPrice.Value);
                lots.AddLast(lot);
                runningTotalCost += LotCost(lot.Quantity, lot.UnitPrice);
                runningQuantity += lot.Quantity;
            }
            else if (line.Type == AvgPriceLineTypes.Sell)
            {
                // Remove lots from the front until the sell amount is fulfilled
                var remainingToSell = line.Quantity;

                while (remainingToSell > 0 && lots.First is { } firstNode)
                {
                    if (firstNode.Value.Quantity <= remainingToSell)
                    {
                        // Consume the entire lot
                        remainingToSell -= firstNode.Value.Quantity;
                        runningTotalCost -= LotCost(firstNode.Value.Quantity, firstNode.Value.UnitPrice);
                        runningQuantity -= firstNode.Value.Quantity;
                        lots.RemoveFirst();
                    }
                    else
                    {
                        // Partially consume the lot in place
                        var newQty = firstNode.Value.Quantity - remainingToSell;
                        var oldCost = LotCost(firstNode.Value.Quantity, firstNode.Value.UnitPrice);
                        var newCost = LotCost(newQty, firstNode.Value.UnitPrice);
                        firstNode.Value = new CostLot(newQty, firstNode.Value.UnitPrice);
                        runningTotalCost += newCost - oldCost;
                        runningQuantity -= remainingToSell;
                        remainingToSell = 0;
                    }
                }

                if (lots.Count == 0)
                {
                    runningTotalCost = 0m;
                    runningQuantity = 0m;
                }
            }
            else
            {
                // Setup: clear the list and add a single lot with the setup values
                // For Setup, Amount represents the avg cost per unit (not total cost)
                lots.Clear();
                var setupLot = new CostLot(line.Quantity, line.Amount.Value);
                lots.AddLast(setupLot);
                runningTotalCost = LotCost(setupLot.Quantity, setupLot.UnitPrice);
                runningQuantity = setupLot.Quantity;
            }

            var avg = runningQuantity > 0
                ? Math.Round(runningTotalCost / runningQuantity, _profile.Asset.Precision)
                : 0m;
            _profile.ChangeLineTotals(line, new LineTotals(FiatValue.New(avg), runningTotalCost, runningQuantity));
        }
    }

    private record CostLot(decimal Quantity, decimal UnitPrice);
}
