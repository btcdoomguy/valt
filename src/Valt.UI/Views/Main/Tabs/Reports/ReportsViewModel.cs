using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Valt.Core.Common;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.Budget.Categories;
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

public partial class ReportsViewModel : ValtTabViewModel, IDisposable
{
    private readonly IAllTimeHighReport _allTimeHighReport;
    private readonly IMonthlyTotalsReport _monthlyTotalsReport;
    private readonly IExpensesByCategoryReport _expensesByCategoryReport;
    private readonly CurrencySettings _currencySettings;
    private readonly ILocalDatabase _localDatabase;
    private readonly IClock _clock;
    private readonly ILogger<ReportsViewModel> _logger;
    private readonly AccountsTotalState _accountsTotalState;

    private const long TotalBtcSupplySats = 21_000_000_00_000_000L; // 21 million BTC in sats

    [ObservableProperty] private DashboardData _allTimeHighData;
    [ObservableProperty] private DashboardData _btcStackData;
    [ObservableProperty] private AvaloniaList<MonthlyReportItemViewModel> _monthlyReportItems = new();
    [ObservableProperty] private MonthlyTotalsChartData _monthlyTotalsChartData = new();
    [ObservableProperty] private ExpensesByCategoryChartData _expensesByCategoryChartData = new();
    [ObservableProperty] private DateTime _filterMainDate;
    [ObservableProperty] private DateRange _filterRange;
    [ObservableProperty] private DateTime _categoryFilterMainDate;
    [ObservableProperty] private DateRange _categoryFilterRange;

    [ObservableProperty] private bool _isAllTimeHighLoading = true;
    [ObservableProperty] private bool _isBtcStackLoading = true;
    [ObservableProperty] private bool _isMonthlyTotalsLoading = true;
    [ObservableProperty] private bool _isSpendingByCategoriesLoading = true;

    // Pie chart filter collections
    [ObservableProperty] private AvaloniaList<SelectItem> _availableAccounts = new();
    [ObservableProperty] private AvaloniaList<SelectItem> _selectedAccounts = new();
    [ObservableProperty] private AvaloniaList<SelectItem> _availableCategories = new();
    [ObservableProperty] private AvaloniaList<SelectItem> _selectedCategories = new();

    private CancellationTokenSource? _filterDebounceTokenSource;
    private const int FilterDebounceDelayMs = 300;

    private bool _ready;

    public ReportsViewModel(IAllTimeHighReport allTimeHighReport,
        IMonthlyTotalsReport monthlyTotalsReport,
        IExpensesByCategoryReport expensesByCategoryReport,
        CurrencySettings currencySettings,
        ILocalDatabase localDatabase,
        IClock clock,
        ILogger<ReportsViewModel> logger,
        AccountsTotalState accountsTotalState)
    {
        _allTimeHighReport = allTimeHighReport;
        _monthlyTotalsReport = monthlyTotalsReport;
        _expensesByCategoryReport = expensesByCategoryReport;
        _currencySettings = currencySettings;
        _localDatabase = localDatabase;
        _clock = clock;
        _logger = logger;
        _accountsTotalState = accountsTotalState;

        FilterMainDate = CategoryFilterMainDate = _clock.GetCurrentDateTimeUtc();
        FilterRange = new DateRange(new DateTime(FilterMainDate.Year, 1, 1), new DateTime(FilterMainDate.Year, 12, 31));
        var currentMonth = new DateTime(CategoryFilterMainDate.Year, CategoryFilterMainDate.Month, 1);
        CategoryFilterRange = new DateRange(currentMonth, currentMonth.AddMonths(1).AddDays(-1));

        PrepareAccountsAndCategoriesList();

        SelectedAccounts.CollectionChanged += OnSelectedFiltersChanged;
        SelectedCategories.CollectionChanged += OnSelectedFiltersChanged;

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

        _accountsTotalState.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(AccountsTotalState.CurrentWealth))
            {
                Dispatcher.UIThread.Post(UpdateBtcStackData);
            }
        };

        _ready = true;
    }

    public void Initialize()
    {
        if (Design.IsDesignMode)
            return;

        // Temporarily disable event handlers to prevent cascading updates during initialization
        _ready = false;
        try
        {
            PrepareAccountsAndCategoriesList();
        }
        finally
        {
            _ready = true;
        }

        _ = FetchMonthlyTotalsAsync();
        _ = FetchExpensesByCategoryAsync();
        _ = FetchAllTimeHighDataAsync();
        UpdateBtcStackData();
    }

    private void PrepareAccountsAndCategoriesList()
    {
        AvailableAccounts.Clear();
        SelectedAccounts.Clear();
        AvailableCategories.Clear();
        SelectedCategories.Clear();
        
        var accounts = _localDatabase.GetAccounts().FindAll().OrderByDescending(x => x.Visible).ThenBy(x => x.DisplayOrder)
            .Select(x => new SelectItem(x.Id.ToString(), x.Name));
        
        AvailableAccounts.AddRange(accounts);
        SelectedAccounts.AddRange(AvailableAccounts);

        var categories = _localDatabase.GetCategories().FindAll().ToDictionary(x => x.Id.ToString());

        var parsedCategories = new List<SelectItem>();
        foreach (var category in categories)
        {
            var name = category.Value.Name;
            if (category.Value.ParentId is not null)
                name = categories[category.Value.ParentId.ToString()].Name + " >> " +  name;
            
            parsedCategories.Add(new SelectItem(category.Key, name));
        }

        AvailableCategories.AddRange(parsedCategories.OrderBy(x => x.Name));
        SelectedCategories.AddRange(AvailableCategories);
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

    private void OnSelectedFiltersChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!_ready) return;

        // Cancel any pending debounced fetch
        _filterDebounceTokenSource?.Cancel();
        _filterDebounceTokenSource = new CancellationTokenSource();

        IsSpendingByCategoriesLoading = true;
        _ = DebouncedFetchExpensesByCategoryAsync(_filterDebounceTokenSource.Token);
    }

    private async Task DebouncedFetchExpensesByCategoryAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(FilterDebounceDelayMs, cancellationToken);
            await FetchExpensesByCategoryAsync();
        }
        catch (TaskCanceledException)
        {
            // Debounce cancelled, ignore
        }
    }

    private async Task FetchAllTimeHighDataAsync()
    {
        try
        {
            var fiatCurrency = FiatCurrency.GetFromCode(_currencySettings.MainFiatCurrency);

            var allTimeHighData = await _allTimeHighReport.GetAsync(fiatCurrency);

            var rows = new ObservableCollection<RowItem>
            {
                new(language.Reports_AllTimeHigh_AllTimeHigh,
                    $"{CurrencyDisplay.FormatFiat(allTimeHighData.Value, fiatCurrency.Code)}"),
                new(language.Reports_AllTimeHigh_Date, allTimeHighData.Date.ToString()),
                new(language.Reports_AllTimeHigh_DeclineFromAth, $"{allTimeHighData.DeclineFromAth}%")
            };

            if (allTimeHighData.MaxDrawdownDate.HasValue && allTimeHighData.MaxDrawdownPercent.HasValue)
            {
                rows.Add(new RowItem(language.Reports_AllTimeHigh_MaxDrawdownPercent,
                    $"{allTimeHighData.MaxDrawdownPercent.Value}%"));
                rows.Add(new RowItem(language.Reports_AllTimeHigh_MaxDrawdownDate,
                    allTimeHighData.MaxDrawdownDate.Value.ToString()));
            }

            AllTimeHighData = new DashboardData(language.Reports_AllTimeHigh_Title, rows);

            IsAllTimeHighLoading = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all time high data");
        }
        finally
        {
            IsAllTimeHighLoading = false;
        }
    }

    private void UpdateBtcStackData()
    {
        try
        {
            var wealth = _accountsTotalState.CurrentWealth;
            if (wealth.WealthInSats == 0)
            {
                BtcStackData = new DashboardData(language.Reports_BtcStack_Title, new ObservableCollection<RowItem>
                {
                    new(language.Reports_BtcStack_CurrentStack, "0 BTC"),
                    new(language.Reports_BtcStack_PercentOfSupply, "0%"),
                    new(language.Reports_BtcStack_PeopleWithSameStack, "∞")
                });
                IsBtcStackLoading = false;
                return;
            }

            var btcFormatted = CurrencyDisplay.FormatSatsAsBitcoin(wealth.WealthInSats);
            var percentOfSupply = (decimal)wealth.WealthInSats / TotalBtcSupplySats * 100m;
            var percentFormatted = percentOfSupply.ToString("0.############") + "%";
            var peopleWithSameStack = Math.Round((decimal)TotalBtcSupplySats / wealth.WealthInSats);
            var peopleFormatted = peopleWithSameStack.ToString("N0", CultureInfo.CurrentCulture);

            var rows = new ObservableCollection<RowItem>
            {
                new(language.Reports_BtcStack_CurrentStack, btcFormatted + " BTC"),
                new(language.Reports_BtcStack_PercentOfSupply, percentFormatted),
                new(language.Reports_BtcStack_PeopleWithSameStack, peopleFormatted)
            };

            BtcStackData = new DashboardData(language.Reports_BtcStack_Title, rows);
            IsBtcStackLoading = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating BTC stack data");
            IsBtcStackLoading = false;
        }
    }

    private async Task FetchExpensesByCategoryAsync()
    {
        try
        {
            var filter = new IExpensesByCategoryReport.Filter(
                SelectedAccounts.Select(x => new AccountId(x.Id.ToString())).ToList(),
                SelectedCategories.Select(x => new CategoryId(x.Id.ToString())).ToList());

            var expensesByCategoryData = await _expensesByCategoryReport.GetAsync(
                DateOnly.FromDateTime(CategoryFilterMainDate),
                new DateOnlyRange(DateOnly.FromDateTime(CategoryFilterRange.Start),
                    DateOnly.FromDateTime(CategoryFilterRange.End)),
                FiatCurrency.GetFromCode(_currencySettings.MainFiatCurrency),
                filter);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ExpensesByCategoryChartData.RefreshChart(expensesByCategoryData);
                IsSpendingByCategoriesLoading = false;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching expenses by category");
            await Dispatcher.UIThread.InvokeAsync(() => { IsSpendingByCategoriesLoading = false; });
        }
    }

    private async Task FetchMonthlyTotalsAsync()
    {
        try
        {
            var monthlyTotalsData = await _monthlyTotalsReport.GetAsync(
                DateOnly.FromDateTime(FilterMainDate),
                new DateOnlyRange(DateOnly.FromDateTime(FilterRange.Start), DateOnly.FromDateTime(FilterRange.End)),
                FiatCurrency.GetFromCode(_currencySettings.MainFiatCurrency));

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                MonthlyTotalsChartData.RefreshChart(monthlyTotalsData);

                var currency = FiatCurrency.GetFromCode(_currencySettings.MainFiatCurrency);

                MonthlyReportItems.Clear();
                MonthlyReportItems.AddRange(monthlyTotalsData.Items.Select(x =>
                    new MonthlyReportItemViewModel(currency, x)));
                
                MonthlyReportItems.Add(new MonthlyReportItemViewModel(currency, monthlyTotalsData.Total));
                
                IsMonthlyTotalsLoading = false;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching monthly totals");
            await Dispatcher.UIThread.InvokeAsync(() => { IsMonthlyTotalsLoading = false; });
        }
    }

    public override MainViewTabNames TabName => MainViewTabNames.ReportsPageContent;

    public record SelectItem(string Id, string Name);

    public void Dispose()
    {
        _filterDebounceTokenSource?.Cancel();
        _filterDebounceTokenSource?.Dispose();

        SelectedAccounts.CollectionChanged -= OnSelectedFiltersChanged;
        SelectedCategories.CollectionChanged -= OnSelectedFiltersChanged;

        WeakReferenceMessenger.Default.Unregister<SettingsChangedMessage>(this);

        MonthlyTotalsChartData.Dispose();
        ExpensesByCategoryChartData.Dispose();
    }
}