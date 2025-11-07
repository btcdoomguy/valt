using System.Globalization;
using Valt.Core.Common;

namespace Valt.Infra.Kernel;

public static class CurrencyDisplay
{
    public static NumberFormatInfo NumberFormat { get; }

    static CurrencyDisplay()
    {
        var culture = CultureInfo.CurrentUICulture;
        var numberFormat = (NumberFormatInfo)culture.NumberFormat.Clone();
        numberFormat.CurrencySymbol = "";
        NumberFormat = numberFormat;
    }

    public static string FormatAsBitcoin(decimal bitcoinValue)
    {
        return (Convert.ToInt64(bitcoinValue * 100_000_000) / 100_000_000m).ToString(CultureInfo.InvariantCulture);
    }

    public static string FormatSatsAsBitcoin(long bitcoinValue)
    {
        return (bitcoinValue / 100_000_000m).ToString(CultureInfo.InvariantCulture);
    }

    public static string FormatFiat(decimal value, string currencyCode)
    {
        var currency = FiatCurrency.GetFromCode(currencyCode);

        return currency.SymbolOnRight
            ? $"{value.ToString("C", NumberFormat)} {currency.Symbol}"
            : $"{currency.Symbol} {value.ToString("C", NumberFormat)}";
    }
}