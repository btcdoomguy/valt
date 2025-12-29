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
        // Queue of cost lots: (quantity, unitPrice)
        var lots = new Queue<CostLot>();

        foreach (var line in orderedLines)
        {
            if (line.Type == AvgPriceLineTypes.Buy)
            {
                // Add a new lot at the end of the queue
                lots.Enqueue(new CostLot(line.Quantity, line.UnitPrice.Value));
            }
            else if (line.Type == AvgPriceLineTypes.Sell)
            {
                // Remove lots from the front until the sell amount is fulfilled
                var remainingToSell = line.Quantity;

                while (remainingToSell > 0 && lots.Count > 0)
                {
                    var lot = lots.Peek();

                    if (lot.Quantity <= remainingToSell)
                    {
                        // Consume the entire lot
                        remainingToSell -= lot.Quantity;
                        lots.Dequeue();
                    }
                    else
                    {
                        // Partially consume the lot
                        lots.Dequeue();
                        lots = PrependLot(lots, new CostLot(lot.Quantity - remainingToSell, lot.UnitPrice));
                        remainingToSell = 0;
                    }
                }
            }
            else
            {
                // Setup: clear the queue and add a single lot with the setup values
                lots.Clear();
                lots.Enqueue(new CostLot(line.Quantity, line.UnitPrice.Value));
            }

            // Calculate totals from remaining lots
            var (totalCost, quantity, avg) = CalculateFromLots(lots);
            _profile.ChangeLineTotals(line, new LineTotals(FiatValue.New(avg), totalCost, quantity));
        }
    }

    private (decimal totalCost, decimal quantity, decimal avg) CalculateFromLots(Queue<CostLot> lots)
    {
        if (lots.Count == 0)
            return (0m, 0m, 0m);

        var totalCost = 0m;
        var quantity = 0m;

        foreach (var lot in lots)
        {
            totalCost += Math.Round(lot.Quantity * lot.UnitPrice, _profile.Asset.Precision);
            quantity += lot.Quantity;
        }

        var avg = quantity > 0 ? Math.Round(totalCost / quantity, _profile.Asset.Precision) : 0m;
        return (totalCost, quantity, avg);
    }

    private Queue<CostLot> PrependLot(Queue<CostLot> existingQueue, CostLot newLot)
    {
        var newQueue = new Queue<CostLot>();
        newQueue.Enqueue(newLot);
        foreach (var lot in existingQueue)
        {
            newQueue.Enqueue(lot);
        }
        return newQueue;
    }

    private record CostLot(decimal Quantity, decimal UnitPrice);
}
