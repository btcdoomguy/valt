using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Valt.Infra.Kernel;
using Valt.UI.Lang;
using Valt.UI.UserControls;

namespace Valt.UI.Views.Main.Tabs.Reports.Panels;

/// <summary>
/// Base class for report dashboard panel ViewModels.
/// Provides common functionality: DashboardData, loading state, error handling.
/// </summary>
public abstract partial class DashboardPanelViewModel : ObservableObject
{
    private readonly ILogger _logger;

    [ObservableProperty] private DashboardData _data = DashboardData.Empty;
    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private bool _isVisible = true;

    protected DashboardPanelViewModel(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Updates the panel data. Implementations should call SetData() or SetError().
    /// </summary>
    public abstract Task RefreshAsync();

    /// <summary>
    /// Synchronous refresh for simple panels that don't need async operations.
    /// </summary>
    public virtual void Refresh()
    {
        // Override in panels that don't need async
    }

    protected void SetData(string title, ObservableCollection<RowItem> rows, string? icon = null, bool isStale = false)
    {
        Data = new DashboardData(title, rows, Icon: icon, IsStale: isStale);
        IsLoading = false;
    }

    protected void SetError(string title, Exception ex, string? icon = null)
    {
        _logger.LogError(ex, "Error refreshing panel: {Title}", title);
        Data = new DashboardData(title,
            new ObservableCollection<RowItem> { new(language.Error, ex.Message) },
            Icon: icon);
        IsLoading = false;
    }

    protected void SetEmpty(string title, string? icon = null)
    {
        Data = new DashboardData(title, new ObservableCollection<RowItem>(), Icon: icon);
        IsLoading = false;
    }

    protected static string FormatBtc(long sats) =>
        Valt.Infra.Kernel.CurrencyDisplay.FormatSatsAsBitcoin(sats) + " BTC";

    protected static string FormatFiat(decimal amount, string currencyCode) =>
        Valt.Infra.Kernel.CurrencyDisplay.FormatFiat(amount, currencyCode);

    protected static string FormatPercent(decimal value, int decimals = 2) =>
        value.ToString($"F{decimals}") + "%";
}
