using System.Globalization;
using System.Text;
using Valt.Core.Common;

namespace Valt.Infra.Kernel;

public static class CurrencyDisplay
{
    public static string FormatAsBitcoin(decimal bitcoinValue)
    {
        return FormatSats(Convert.ToInt64(bitcoinValue * 100_000_000));
    }

    public static string FormatSatsAsBitcoin(long bitcoinValue)
    {
        return FormatSats(bitcoinValue);
    }

    public static string FormatSatsAsNumber(long sats)
    {
        return FormatWithGroups(sats.ToString());
    }

    public static string FormatFiat(decimal value, string currencyCode)
    {
        var currency = FiatCurrency.GetFromCode(currencyCode);

        var cultureName = currency.CultureName;

        var cultureInfo = new CultureInfo(cultureName);
        var numberFormatInfo = (NumberFormatInfo)cultureInfo.NumberFormat.Clone();
        numberFormatInfo.CurrencySymbol = "";
        numberFormatInfo.CurrencyNegativePattern = 1;

        var formattedValue = value.ToString("C", numberFormatInfo).Trim();

        return currency.SymbolOnRight
            ? $"{formattedValue} {currency.Symbol}"
            : $"{currency.Symbol} {formattedValue}";
    }

    private static string FormatSats(long sats)
    {
        const long SatoshiFactor = 100_000_000L;
        
        var prefix = "";

        if (sats < 0)
        {
            sats *= -1;
            prefix = "-";
        }

        var integerPart = sats / SatoshiFactor;
        var fractionalSats = sats % SatoshiFactor;
        
        if (integerPart > 0)
        {
            var integerStr = integerPart.ToString();
            var formattedInteger = FormatWithGroups(integerStr);

            var fractionalStr = fractionalSats.ToString("D8");
            var formattedFractional = $"{fractionalStr.Substring(0, 2)} {fractionalStr.Substring(2, 3)} {fractionalStr.Substring(5, 3)}";

            return $"{prefix}{formattedInteger}.{formattedFractional}";
        }
        else
        {
            var fractionalStr = fractionalSats.ToString();
            return fractionalStr == "0" ? "0" : $"{prefix}{FormatWithGroups(fractionalStr)}";
        }
    }

    // Existing FormatWithGroups method...
    private static string FormatWithGroups(string number, int groupSize = 3, char separator = ' ')
    {
        if (string.IsNullOrEmpty(number) || number == "0")
        {
            return number;
        }

        var stringBuilder = new StringBuilder();
        var count = 0;

        for (var i = number.Length - 1; i >= 0; i--)
        {
            stringBuilder.Insert(0, number[i]);
            count++;
            if (count % groupSize == 0 && i > 0)
            {
                stringBuilder.Insert(0, separator);
            }
        }

        return stringBuilder.ToString();
    }
}