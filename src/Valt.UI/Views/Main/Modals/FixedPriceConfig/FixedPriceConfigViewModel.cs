using System;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Valt.UI.Base;
using Valt.UI.Lang;

namespace Valt.UI.Views.Main.Modals.FixedPriceConfig;

public partial class FixedPriceConfigViewModel : ValtModalViewModel
{
    [ObservableProperty] private string _priceText = string.Empty;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private string _currencySymbol = "$";

    public FixedPriceConfigViewModel() { }

    public FixedPriceConfigViewModel(string currencySymbol, decimal? currentPrice)
    {
        _currencySymbol = currencySymbol;
        if (currentPrice.HasValue)
        {
            _priceText = currentPrice.Value.ToString("F2", CultureInfo.InvariantCulture);
        }
    }

    [RelayCommand]
    private void Save()
    {
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(PriceText))
        {
            ErrorMessage = language.Reports_FixedPriceModal_Validation_Required;
            return;
        }

        if (!decimal.TryParse(PriceText, NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
        {
            ErrorMessage = language.Reports_FixedPriceModal_Validation_Required;
            return;
        }

        if (price < 0)
        {
            ErrorMessage = language.Reports_FixedPriceModal_Validation_NonNegative;
            return;
        }

        CloseDialog?.Invoke(new Response(true, price));
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseWindow?.Invoke();
    }

    public record Response(bool Ok, decimal Price);
}