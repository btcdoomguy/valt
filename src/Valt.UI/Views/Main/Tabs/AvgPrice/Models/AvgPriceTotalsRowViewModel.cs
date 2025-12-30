using System;
using System.Globalization;
using Valt.Core.Common;
using Valt.Core.Modules.AvgPrice.Calculations;

namespace Valt.UI.Views.Main.Tabs.AvgPrice.Models;

public class AvgPriceTotalsRowViewModel
{
    public string Period { get; }
    public string AmountBought { get; }
    public string AmountSold { get; }
    public string ProfitLoss { get; }
    public string ProfitLossColor { get; }
    public string Volume { get; }
    public bool IsYearlyTotal { get; }

    public AvgPriceTotalsRowViewModel(DateTime month, IAvgPriceTotalizer.ValuesDTO values, FiatCurrency currency, bool isYearlyTotal = false)
    {
        IsYearlyTotal = isYearlyTotal;
        Period = isYearlyTotal
            ? Lang.language.AvgPrice_Totals_YearlyTotal
            : month.ToString("MMMM", CultureInfo.CurrentCulture);

        AmountBought = FormatCurrency(values.AmountBought, currency);
        AmountSold = FormatCurrency(values.AmountSold, currency);
        ProfitLoss = FormatCurrency(values.TotalProfitLoss, currency);
        ProfitLossColor = values.TotalProfitLoss >= 0 ? "Green" : "Red";
        Volume = FormatCurrency(values.Volume, currency);
    }

    private static string FormatCurrency(decimal value, FiatCurrency currency)
    {
        var culture = new CultureInfo(currency.CultureName);
        return value.ToString("N2", culture);
    }
}
