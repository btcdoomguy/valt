namespace Valt.Core.Common;

public static class BtcPriceCalculator
{
    /// <summary>
    /// Calculates the bitcoin price of a fiat transaction when user converts fiat to bitcoin
    /// </summary>
    /// <param name="fromFiat"></param>
    /// <param name="toBtc"></param>
    /// <returns></returns>
    public static FiatValue CalculateBtcPrice(FiatValue fromFiat, BtcValue toBtc)
    {
        return Math.Round(fromFiat.Value / toBtc.Btc, 2);
    }

    public static long CalculateBtcAmountOfFiat(decimal fiatAmount, decimal fiatRateInUsd, decimal btcPriceInUsd)
    {
        var usdTotal = fiatAmount / fiatRateInUsd;
        var btcPrice = usdTotal / btcPriceInUsd;
        return Convert.ToInt64(btcPrice * 100_000_000);
    }
}