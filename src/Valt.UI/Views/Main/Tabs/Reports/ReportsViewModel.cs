using System;
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
using Valt.Infra.Modules.Reports;
using Valt.Infra.Modules.Reports.AllTimeHigh;
using Valt.Infra.Modules.Reports.ExpensesByCategory;
using Valt.Infra.Modules.Reports.MonthlyTotals;
using Valt.Infra.Modules.Reports.Statistics;
using Valt.Infra.Settings;
using Valt.UI.Base;
using Valt.UI.Lang;
using Valt.UI.State;
using Valt.UI.UserControls;
using Valt.UI.Views.Main.Tabs.Reports.Models;

namespace Valt.UI.Views.Main.Tabs.Reports;

public partial class ReportsViewModel : ValtTabViewModel, IDisposable
{
    private readonly IAllTimeHighReport _allTimeHighReport;
    private readonly IMonthlyTotalsReport _monthlyTotalsReport;
    private readonly IExpensesByCategoryReport _expensesByCategoryReport;
    private readonly IStatisticsReport _statisticsReport;
    private readonly IReportDataProviderFactory _reportDataProviderFactory;
    private readonly CurrencySettings _currencySettings;
    private readonly ILocalDatabase _localDatabase;
    private readonly IClock _clock;
    private readonly ILogger<ReportsViewModel> _logger;
    private readonly AccountsTotalState _accountsTotalState;
    private readonly RatesState _ratesState;

    private const long TotalBtcSupplySats = 21_000_000_00_000_000L; // 21 million BTC in sats

    // Cached provider for the lifetime of the tab being active
    private IReportDataProvider? _cachedProvider;

    [ObservableProperty] private DashboardData _allTimeHighData = DashboardData.Empty;
    [ObservableProperty] private DashboardData _btcStackData = DashboardData.Empty;
    [ObservableProperty] private DashboardData _statisticsData = DashboardData.Empty;
    [ObservableProperty] private AvaloniaList<MonthlyReportItemViewModel> _monthlyReportItems = new();
    [ObservableProperty] private MonthlyTotalsChartData _monthlyTotalsChartData = new();
    [ObservableProperty] private ExpensesByCategoryChartData _expensesByCategoryChartData = new();
    [ObservableProperty] private DateTime _filterMainDate;
    [ObservableProperty] private DateRange _filterRange;
    [ObservableProperty] private DateTime _categoryFilterMainDate;
    [ObservableProperty] private DateRange _categoryFilterRange;

    [ObservableProperty] private bool _isAllTimeHighLoading = true;
    [ObservableProperty] private bool _isBtcStackLoading = true;
    [ObservableProperty] private bool _isStatisticsLoading = true;
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
        IStatisticsReport statisticsReport,
        IReportDataProviderFactory reportDataProviderFactory,
        CurrencySettings currencySettings,
        ILocalDatabase localDatabase,
        IClock clock,
        ILogger<ReportsViewModel> logger,
        AccountsTotalState accountsTotalState,
        RatesState ratesState)
    {
        _allTimeHighReport = allTimeHighReport;
        _monthlyTotalsReport = monthlyTotalsReport;
        _expensesByCategoryReport = expensesByCategoryReport;
        _statisticsReport = statisticsReport;
        _reportDataProviderFactory = reportDataProviderFactory;
        _currencySettings = currencySettings;
        _localDatabase = localDatabase;
        _clock = clock;
        _logger = logger;
        _accountsTotalState = accountsTotalState;
        _ratesState = ratesState;

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
                    IsStatisticsLoading = true;
                    IsMonthlyTotalsLoading = true;
                    IsSpendingByCategoriesLoading = true;
                    // Reload data when currency changes
                    _ = ReloadDataAndFetchAllReportsAsync();
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

        _ = LoadDataAndFetchAllReportsAsync();
        UpdateBtcStackData();
    }

    /// <summary>
    /// Unloads the cached provider to free memory when the tab is no longer active
    /// </summary>
    public void UnloadData()
    {
        _cachedProvider = null;
        _logger.LogDebug("Report data provider unloaded");
    }

    private IReportDataProvider GetOrCreateProvider()
    {
        _cachedProvider ??= _reportDataProviderFactory.Create();
        return _cachedProvider;
    }

    private async Task LoadDataAndFetchAllReportsAsync()
    {
        // Create and cache the provider
        _cachedProvider = _reportDataProviderFactory.Create();

        await FetchAllReportsAsync(_cachedProvider);
    }

    private async Task ReloadDataAndFetchAllReportsAsync()
    {
        // Recreate the provider (data might have changed)
        _cachedProvider = _reportDataProviderFactory.Create();

        await FetchAllReportsAsync(_cachedProvider);
    }

    private async Task FetchAllReportsAsync(IReportDataProvider provider)
    {
        await Task.WhenAll(
            FetchMonthlyTotalsAsync(provider),
            FetchExpensesByCategoryAsync(provider),
            FetchAllTimeHighDataAsync(provider),
            FetchStatisticsDataAsync(provider));
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
        var provider = GetOrCreateProvider();
        _ = FetchMonthlyTotalsAsync(provider);
    }

    partial void OnCategoryFilterRangeChanged(DateRange value)
    {
        if (!_ready) return;

        IsSpendingByCategoriesLoading = true;
        var provider = GetOrCreateProvider();
        _ = FetchExpensesByCategoryAsync(provider);
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
            var provider = GetOrCreateProvider();
            await FetchExpensesByCategoryAsync(provider);
        }
        catch (TaskCanceledException)
        {
            // Debounce cancelled, ignore
        }
    }

    private async Task FetchAllTimeHighDataAsync(IReportDataProvider provider)
    {
        try
        {
            var fiatCurrency = FiatCurrency.GetFromCode(_currencySettings.MainFiatCurrency);

            var allTimeHighData = await _allTimeHighReport.GetAsync(fiatCurrency, provider);

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
            AllTimeHighData = new DashboardData(language.Reports_AllTimeHigh_Title,
                new ObservableCollection<RowItem>
                {
                    new(language.Error, ex.Message)
                });
        }
        finally
        {
            IsAllTimeHighLoading = false;
        }
    }

    private async Task FetchStatisticsDataAsync(IReportDataProvider provider)
    {
        try
        {
            var fiatCurrency = FiatCurrency.GetFromCode(_currencySettings.MainFiatCurrency);
            var currentWealth = _accountsTotalState.CurrentWealth;

            // Get current wealth in main fiat currency
            var currentWealthInFiat = currentWealth.AllWealthInMainFiatCurrency;

            var statisticsData = await _statisticsReport.GetAsync(fiatCurrency, currentWealthInFiat, provider);

            var rows = new ObservableCollection<RowItem>
            {
                new(language.Reports_Statistics_MedianExpenses,
                    $"{CurrencyDisplay.FormatFiat(statisticsData.MedianMonthlyExpenses, fiatCurrency.Code)}")
            };

            // Add expenses in sats if we have valid rate data
            var expensesInSats = ConvertFiatToSats(statisticsData.MedianMonthlyExpenses, fiatCurrency);
            if (expensesInSats.HasValue)
            {
                var satsFormatted = expensesInSats.Value.ToString("N0", CultureInfo.CurrentCulture) + " sats";
                rows.Add(new RowItem(language.Reports_Statistics_MedianExpensesSats, satsFormatted));
            }

            rows.Add(new RowItem(language.Reports_Statistics_WealthCoverage, statisticsData.WealthCoverageFormatted));

            StatisticsData = new DashboardData(language.Reports_Statistics_Title, rows);

            IsStatisticsLoading = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching statistics data");
            StatisticsData = new DashboardData(language.Reports_Statistics_Title,
                new ObservableCollection<RowItem>
                {
                    new(language.Error, ex.Message)
                });
        }
        finally
        {
            IsStatisticsLoading = false;
        }
    }

    private long? ConvertFiatToSats(decimal fiatValue, FiatCurrency fiatCurrency)
    {
        var bitcoinPriceUsd = _ratesState.BitcoinPrice;
        var fiatRates = _ratesState.FiatRates;

        if (bitcoinPriceUsd is null or 0 || fiatRates is null)
            return null;

        // Convert fiat value to USD
        decimal valueInUsd;
        if (fiatCurrency.Code == FiatCurrency.Usd.Code)
        {
            valueInUsd = fiatValue;
        }
        else if (fiatRates.TryGetValue(fiatCurrency.Code, out var rate) && rate != 0)
        {
            // fiatRates contains the rate of each currency relative to USD (e.g., BRL rate = 5.0 means 1 USD = 5 BRL)
            valueInUsd = fiatValue / rate;
        }
        else
        {
            return null;
        }

        // Convert USD to BTC then to sats
        var btcValue = valueInUsd / bitcoinPriceUsd.Value;
        var sats = (long)(btcValue * 100_000_000m);

        return sats;
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

    private async Task FetchExpensesByCategoryAsync(IReportDataProvider provider)
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
                filter,
                provider);

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

    private async Task FetchMonthlyTotalsAsync(IReportDataProvider provider)
    {
        try
        {
            var monthlyTotalsData = await _monthlyTotalsReport.GetAsync(
                DateOnly.FromDateTime(FilterMainDate),
                new DateOnlyRange(DateOnly.FromDateTime(FilterRange.Start), DateOnly.FromDateTime(FilterRange.End)),
                FiatCurrency.GetFromCode(_currencySettings.MainFiatCurrency),
                provider);

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

        // Clear the provider on dispose
        _cachedProvider = null;
    }
}
