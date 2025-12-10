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
using Valt.Infra.Modules.Reports.ExpensesByCategory;
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
    private readonly IExpensesByCategoryReport _expensesByCategoryReport;
    private readonly CurrencySettings _currencySettings;
    private readonly IClock _clock;

    [ObservableProperty] private DashboardData _allTimeHighData;
    [ObservableProperty] private MonthlyTotalsData _monthlyTotalsData;
    [ObservableProperty] private AvaloniaList<MonthlyReportItemViewModel> _monthlyReportItems = new();
    [ObservableProperty] private MonthlyTotalsChartData _monthlyTotalsChartData = new();
    [ObservableProperty] private ExpensesByCategoryChartData _expensesByCategoryChartData = new();
    [ObservableProperty] private DateTime _filterMainDate;
    [ObservableProperty] private DateRange _filterRange;
    [ObservableProperty] private DateTime _categoryFilterMainDate;
    [ObservableProperty] private DateRange _categoryFilterRange;

    [ObservableProperty] private bool _isAllTimeHighLoading = true;
    [ObservableProperty] private bool _isMonthlyTotalsLoading = true;
    [ObservableProperty] private bool _isSpendingByCategoriesLoading = true;

    private bool _ready;

    public ReportsViewModel(IAllTimeHighReport allTimeHighReport,
        IMonthlyTotalsReport monthlyTotalsReport,
        IExpensesByCategoryReport expensesByCategoryReport,
        CurrencySettings currencySettings,
        IClock clock)
    {
        _allTimeHighReport = allTimeHighReport;
        _monthlyTotalsReport = monthlyTotalsReport;
        _expensesByCategoryReport = expensesByCategoryReport;
        _currencySettings = currencySettings;
        _clock = clock;

        FilterMainDate = CategoryFilterMainDate = _clock.GetCurrentDateTimeUtc();
        FilterRange = new DateRange(new DateTime(FilterMainDate.Year, 1, 1), new DateTime(FilterMainDate.Year, 12, 31));
        var currentMonth = new DateTime(CategoryFilterMainDate.Year, CategoryFilterMainDate.Month, 1);
        CategoryFilterRange = new DateRange(currentMonth, currentMonth.AddMonths(1).AddDays(-1));

        WeakReferenceMessenger.Default.Register<SettingsChangedMessage>(this, (recipient, message) =>
        {
            switch (message.Value)
            {
                case nameof(CurrencySettings.MainFiatCurrency):
                    IsAllTimeHighLoading = true;
                    _ = FetchAllTimeHighDataAsync();
                    IsMonthlyTotalsLoading = true;
                    _ = FetchMonthlyTotalsAsync();
                    IsSpendingByCategoriesLoading = true;
                    _ = FetchExpensesByCategoryAsync();
                    break;
            }
        });

        _ready = true;
    }

    public void Initialize()
    {
        _ = FetchAllTimeHighDataAsync();
        _ = FetchMonthlyTotalsAsync();
        _ = FetchExpensesByCategoryAsync();
    }

    partial void OnFilterRangeChanged(DateRange value)
    {
        if (!_ready) return;

        IsMonthlyTotalsLoading = true;
        _ = FetchMonthlyTotalsAsync();
    }

    partial void OnCategoryFilterRangeChanged(DateRange value)
    {
        if (!_ready) return;

        IsSpendingByCategoriesLoading = true;
        _ = FetchExpensesByCategoryAsync();
    }

    private Task FetchAllTimeHighDataAsync()
    {
        return Task.Run(async () =>
        {
            try
            {
                var fiatCurrency = FiatCurrency.GetFromCode(_currencySettings.MainFiatCurrency);

                var allTimeHighData = await _allTimeHighReport.GetAsync(fiatCurrency);

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    AllTimeHighData = new DashboardData(
                        language.Reports_AllTimeHigh_Title,
                        new ObservableCollection<RowItem>
                        {
                            new(language.Reports_AllTimeHigh_AllTimeHigh,
                                $"{CurrencyDisplay.FormatFiat(allTimeHighData.Value, fiatCurrency.Code)}"),
                            new(language.Reports_AllTimeHigh_Date, allTimeHighData.Date.ToString()),
                            new(language.Reports_AllTimeHigh_DeclineFromAth, $"{allTimeHighData.DeclineFromAth}%")
                        });

                    IsAllTimeHighLoading = false;
                });
            }
            catch
            {
                await Dispatcher.UIThread.InvokeAsync(() => IsAllTimeHighLoading = false);
            }
        });
    }

    private Task FetchExpensesByCategoryAsync()
    {
        return Task.Run(async () =>
        {
            try
            {
                var expensesByCategoryData = await _expensesByCategoryReport.GetAsync(
                    DateOnly.FromDateTime(CategoryFilterMainDate),
                    new DateOnlyRange(DateOnly.FromDateTime(CategoryFilterRange.Start),
                        DateOnly.FromDateTime(CategoryFilterRange.End)),
                    FiatCurrency.GetFromCode(_currencySettings.MainFiatCurrency));
                
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ExpensesByCategoryChartData.RefreshChart(expensesByCategoryData);
                    IsSpendingByCategoriesLoading = false;
                });
            }
            catch
            {
                await Dispatcher.UIThread.InvokeAsync(() => IsSpendingByCategoriesLoading = false);
            }
        });
    }

    private Task FetchMonthlyTotalsAsync()
    {
        return Task.Run(async () =>
        {
            try
            {
                var monthlyTotalsData = await _monthlyTotalsReport.GetAsync(
                    DateOnly.FromDateTime(FilterMainDate),
                    new DateOnlyRange(DateOnly.FromDateTime(FilterRange.Start), DateOnly.FromDateTime(FilterRange.End)),
                    FiatCurrency.GetFromCode(_currencySettings.MainFiatCurrency));

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    MonthlyTotalsData = monthlyTotalsData;
                    MonthlyTotalsChartData.RefreshChart(monthlyTotalsData);

                    MonthlyReportItems.Clear();
                    MonthlyReportItems.AddRange(monthlyTotalsData.Items.Select(x =>
                        new MonthlyReportItemViewModel(FiatCurrency.GetFromCode(_currencySettings.MainFiatCurrency),
                            x)));

                    IsMonthlyTotalsLoading = false;
                });
            }
            catch
            {
                await Dispatcher.UIThread.InvokeAsync(() => IsMonthlyTotalsLoading = false);
            }
        });
    }

    public override MainViewTabNames TabName => MainViewTabNames.ReportsPageContent;
}