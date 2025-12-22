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
        var btcAmount = 0m; // in BTC (not satoshis)
        var avg = 0m;

        foreach (var line in orderedLines)
        {
            if (line.Type == AvgPriceLineTypes.Buy)
            {
                totalCost += Math.Round(line.BtcAmount.Btc * line.BitcoinUnitPrice.Value, 2);
                btcAmount += line.BtcAmount.Btc;
                avg = btcAmount > 0 ? Math.Round(totalCost / btcAmount, 2) : 0m;
            }
            else if (line.Type == AvgPriceLineTypes.Sell)
            {
                //Reduce total proportionally
                var proportionSold = line.BtcAmount.Btc / btcAmount;
                totalCost -= Math.Round(totalCost * proportionSold, 2);
                btcAmount -= line.BtcAmount.Btc;
                avg = btcAmount > 0 ? Math.Round(totalCost / btcAmount, 2) : 0m;
            }
            else
            {
                //Setup just overrides the current btc amount and avg price
                btcAmount = line.BtcAmount.Btc;
                avg = line.BitcoinUnitPrice.Value;
                totalCost = Math.Round(btcAmount * avg, 2);
            }

            _profile.ChangeLineTotals(line, new LineTotals(FiatValue.New(avg), totalCost, BtcValue.ParseBitcoin(btcAmount)));
        }
    }
}