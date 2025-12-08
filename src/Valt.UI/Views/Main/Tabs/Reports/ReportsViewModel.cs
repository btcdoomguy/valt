using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Valt.Core.Common;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Infra.Kernel;
using Valt.Infra.Modules.Reports;
using Valt.Infra.Modules.Reports.AllTimeHigh;
using Valt.Infra.Modules.Reports.MonthlyTotals;
using Valt.Infra.Settings;
using Valt.UI.Base;
using Valt.UI.Lang;
using Valt.UI.Services.LocalStorage;
using Valt.UI.State;
using Valt.UI.UserControls;
using Valt.UI.Views.Main.Tabs.Reports.Models;
using Valt.UI.Views.Main.Tabs.Transactions;

namespace Valt.UI.Views.Main.Tabs.Reports;

public partial class ReportsViewModel : ValtTabViewModel
{
    private readonly IAllTimeHighReport _allTimeHighReport;
    private readonly IMonthlyTotalsReport _monthlyTotalsReport;
    private readonly CurrencySettings _currencySettings;
    private readonly IClock _clock;

    [ObservableProperty] private DashboardData _allTimeHighData;
    [ObservableProperty] private MonthlyTotalsData _monthlyTotalsData;
    [ObservableProperty] private AvaloniaList<MonthlyReportItemViewModel> _monthlyReportItems = new();
    [ObservableProperty] private MonthlyTotalsChartData _monthlyTotalsChartData = new();
    [ObservableProperty] private DateTime _filterMainDate;
    [ObservableProperty] private DateRange _filterRange;

    public ReportsViewModel(IAllTimeHighReport allTimeHighReport,
        IMonthlyTotalsReport monthlyTotalsReport,
        CurrencySettings currencySettings,
        IClock clock)
    {
        _allTimeHighReport = allTimeHighReport;
        _monthlyTotalsReport = monthlyTotalsReport;
        _currencySettings = currencySettings;
        _clock = clock;

        FilterMainDate = _clock.GetCurrentDateTimeUtc();
        FilterRange = new DateRange(new DateTime(FilterMainDate.Year, 1, 1), new DateTime(FilterMainDate.Year, 12, 31));

        _ = InitializeAsync();
        
        WeakReferenceMessenger.Default.Register<SettingsChangedMessage>(this, (recipient, message) =>
        {
            switch (message.Value)
            {
                case nameof(CurrencySettings.MainFiatCurrency):
                    _ = FetchAllTimeHighDataAsync();
                    _ = FetchMonthlyTotalsAsync();
                    break;
            }
        });
    }

    private async Task InitializeAsync()
    {
        _ = FetchAllTimeHighDataAsync();
        _ = FetchMonthlyTotalsAsync();
    }

    partial void OnFilterRangeChanged(DateRange value)
    {
        _ = FetchMonthlyTotalsAsync();
    }

    private async Task FetchAllTimeHighDataAsync()
    {
        var fiatCurrency = FiatCurrency.GetFromCode(_currencySettings.MainFiatCurrency);

        var allTimeHighData =
            await _allTimeHighReport.GetAsync(FiatCurrency.GetFromCode(_currencySettings.MainFiatCurrency));

        Dispatcher.UIThread.Post(() =>
        {
            AllTimeHighData = new DashboardData(language.Reports_AllTimeHigh_Title, new ObservableCollection<RowItem>()
            {
                new(language.Reports_AllTimeHigh_AllTimeHigh, $"{CurrencyDisplay.FormatFiat(allTimeHighData.Value, fiatCurrency.Code)}"),
                new(language.Reports_AllTimeHigh_Date, allTimeHighData.Date.ToString()),
                new(language.Reports_AllTimeHigh_DeclineFromAth, $"{allTimeHighData.DeclineFromAth}%")
            });
        });
    }

    private async Task FetchMonthlyTotalsAsync()
    {
        var monthlyTotalsData = await _monthlyTotalsReport.GetAsync(DateOnly.FromDateTime(FilterMainDate),
            new DateOnlyRange(DateOnly.FromDateTime(FilterRange.Start), DateOnly.FromDateTime(FilterRange.End)),
            FiatCurrency.GetFromCode(_currencySettings.MainFiatCurrency));

        MonthlyTotalsData = monthlyTotalsData;

        MonthlyTotalsChartData.RefreshChart(monthlyTotalsData);

        MonthlyReportItems.Clear();

        MonthlyReportItems.AddRange(monthlyTotalsData.Items.Select(x =>
            new MonthlyReportItemViewModel(FiatCurrency.GetFromCode(_currencySettings.MainFiatCurrency), x)));
    }

    public override MainViewTabNames TabName => MainViewTabNames.ReportsPageContent;
}