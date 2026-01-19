using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Valt.UI.Views.Main.Modals.ConversionCalculator;

/// <summary>
/// Represents a currency item in the conversion list.
/// </summary>
public partial class CurrencyConversionItem : ObservableObject
{
    public string CurrencyCode { get; init; } = string.Empty;
    public string CurrencySymbol { get; init; } = string.Empty;
    public int Decimals { get; init; }
    public bool IsBitcoin { get; init; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattedValue))]
    private decimal _convertedValue;

    [ObservableProperty] private bool _isSelected;

    public string FormattedValue
    {
        get
        {
            if (IsBitcoin)
            {
                // Format BTC with 8 decimals
                return ConvertedValue.ToString("N8", CultureInfo.CurrentUICulture);
            }

            // Format fiat with appropriate decimals
            return ConvertedValue.ToString($"N{Decimals}", CultureInfo.CurrentUICulture);
        }
    }
}
