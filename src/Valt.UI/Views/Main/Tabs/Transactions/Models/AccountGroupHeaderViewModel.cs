using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Valt.Infra.Kernel;

namespace Valt.UI.Views.Main.Tabs.Transactions.Models;

/// <summary>
/// Represents a group header in the accounts list.
/// </summary>
public partial class AccountGroupHeaderViewModel : ObservableObject, IAccountListItem
{
    public string Id { get; }
    public string Name { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattedTotal))]
    private bool _secureModeEnabled;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattedTotal))]
    [NotifyPropertyChangedFor(nameof(HasTotal))]
    private decimal? _totalValue;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattedTotal))]
    private string? _totalCurrency;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattedTotal))]
    private bool _isBtcTotal;

    public bool HasTotal => TotalValue.HasValue && TotalValue.Value != 0;

    public string FormattedTotal
    {
        get
        {
            if (SecureModeEnabled)
                return "---";

            if (!HasTotal)
                return string.Empty;

            if (IsBtcTotal)
            {
                return CurrencyDisplay.FormatSatsAsBitcoin(Convert.ToInt64(TotalValue!.Value));
            }

            return TotalCurrency is not null
                ? CurrencyDisplay.FormatFiat(TotalValue!.Value, TotalCurrency)
                : string.Empty;
        }
    }

    public AccountGroupHeaderViewModel(string id, string name)
    {
        Id = id;
        Name = name;
    }

    public AccountGroupHeaderViewModel(string id, string name, decimal? totalValue, string? totalCurrency, bool isBtcTotal, bool secureModeEnabled = false)
    {
        Id = id;
        Name = name;
        _totalValue = totalValue;
        _totalCurrency = totalCurrency;
        _isBtcTotal = isBtcTotal;
        _secureModeEnabled = secureModeEnabled;
    }
}
