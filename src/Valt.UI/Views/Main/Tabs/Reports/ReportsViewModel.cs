using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Valt.Core.Common;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Infra.Modules.Reports;
using Valt.Infra.Modules.Reports.AllTimeHigh;
using Valt.Infra.Settings;
using Valt.UI.Base;
using Valt.UI.Services.LocalStorage;
using Valt.UI.State;
using Valt.UI.UserControls;
using Valt.UI.Views.Main.Tabs.Transactions;

namespace Valt.UI.Views.Main.Tabs.Reports;

public partial class ReportsViewModel : ValtTabViewModel
{
    private readonly IAllTimeHighReport _allTimeHighReport;
    private readonly IMonthlyTotalsReport _monthlyTotalsReport;
    private readonly CurrencySettings _currencySettings;
    private readonly FilterState _filterState;
    private readonly IClock _clock;
    [ObservableProperty] private DashboardData _allTimeHighData;
    [ObservableProperty] private DashboardData _monthlyTotalsData;
    
    #region Filter proxy

    public DateTime FilterMainDate
    {
        get => _filterState.MainDate;
        set => _filterState.MainDate = value;
    }

    public DateRange FilterRange
    {
        get => _filterState.Range;
        set => _filterState.Range = value;
    }

    #endregion

    public ReportsViewModel()
    {
        if (Design.IsDesignMode)
        {
            AllTimeHighData = new DashboardData("My all-time high", [
                new("ATH (R$):", "R$ 100.000,00"),
                new("Date:", "10/06/2025"),
                new("Decline from ATH:", "-30%")
            ]);

            MonthlyTotalsData = new DashboardData("Monthly totals", [
                new("Total (R$):", "R$ 100.000,00"),
                new("BTC Stack:", "0.10000000")
            ]);
        }
    }

    public ReportsViewModel(IAllTimeHighReport allTimeHighReport,
        IMonthlyTotalsReport monthlyTotalsReport,
        CurrencySettings currencySettings,
        FilterState filterState,
        IClock clock)
    {
        _allTimeHighReport = allTimeHighReport;
        _monthlyTotalsReport = monthlyTotalsReport;
        _currencySettings = currencySettings;
        _filterState = filterState;
        _clock = clock;

        _ = InitializeAsync();
        
        WeakReferenceMessenger.Default.Register<FilterDateRangeChanged>(this, OnFilterDataRangeChanged);
    }
    
    private void OnFilterDataRangeChanged(object recipient, FilterDateRangeChanged message)
    {
        OnPropertyChanged(nameof(FilterMainDate));
        OnPropertyChanged(nameof(FilterRange));
        _ = MonthlyTotalsFetchAsync();
    }

    private async Task InitializeAsync()
    {
        var cultureInfo = CultureInfo.GetCultureInfo(LocalStorageHelper.LoadCulture());

        var allTimeHighData =
            await _allTimeHighReport.GetAsync(FiatCurrency.GetFromCode(_currencySettings.MainFiatCurrency));
        _ = MonthlyTotalsFetchAsync();

        Dispatcher.UIThread.Post(() =>
        {
            AllTimeHighData = new DashboardData("My all-time high", new ObservableCollection<RowItem>()
            {
                new("All-time high", $"R$ {allTimeHighData.Value.Value.ToString(cultureInfo)}"),
                new("Date", allTimeHighData.Date.ToString()),
                new("Decline from ATH", $"{allTimeHighData.DeclineFromAth}%")
            });
        });
    }

    private async Task MonthlyTotalsFetchAsync()
    {
        var monthlyTotalsData = await _monthlyTotalsReport.GetAsync(DateOnly.FromDateTime(FilterMainDate),
            FiatCurrency.GetFromCode(_currencySettings.MainFiatCurrency));
        
        Dispatcher.UIThread.Post(() =>
        {
            MonthlyTotalsData = new DashboardData("Monthly totals", new ObservableCollection<RowItem>()
            {
                new("Total", $"R$ {monthlyTotalsData.Fiat.FiatTotal}"),
                new("From last month", monthlyTotalsData.Fiat.VariationFromPreviousMonth.ToString("0.00%")),
                new("From last year", monthlyTotalsData.Fiat.VariationFromPreviousYear.ToString("0.00%")),
                new("BTC", monthlyTotalsData.Bitcoin.BtcTotal.ToString("0.00000000")),
                new("From last month", monthlyTotalsData.Bitcoin.VariationFromPreviousMonth.ToString("0.00%")),
                new("From last year", monthlyTotalsData.Bitcoin.VariationFromPreviousYear.ToString("0.00%")),
            });
        });
    }

    public override MainViewTabNames TabName => MainViewTabNames.ReportsPageContent;
}