using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.Assets.DTOs;
using Valt.App.Modules.Assets.Queries.GetBtcLoansDashboard;
using Valt.App.Modules.Assets.Queries.GetVisibleAssets;
using Valt.Core.Common;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Infra.Crawlers.Indicators;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel;
using Valt.Infra.Modules.Configuration;
using Valt.Infra.Modules.Reports;
using Valt.Infra.Modules.Reports.AllTimeHigh;
using Valt.Infra.Modules.Reports.MaxBtcStack;
using Valt.Infra.Modules.Reports.ExpensesByCategory;
using Valt.Infra.Modules.Reports.IncomeByCategory;
using Valt.Infra.Modules.Reports.MonthlyTotals;
using Valt.Infra.Modules.Reports.Statistics;
using Valt.Infra.Modules.Reports.WealthOverview;
using Valt.Infra.Settings;
using Valt.UI.Base;

using Valt.UI.Lang;
using Valt.UI.Services;
using Valt.UI.Services.MessageBoxes;
using Valt.UI.State;
using Valt.UI.State.Events;
using static Valt.UI.State.AccountsTotalState;
using Valt.UI.UserControls;
using Valt.UI.Views.Main.Modals.SimulatedPricesConfig;
using Valt.UI.Views.Main.Modals.FixedPriceConfig;
using Valt.UI.Views.Main.Modals.StatisticsConfig;
using Valt.UI.Views.Main.Tabs.Reports.Models;
using Valt.UI.Views.Main.Tabs.Reports.Panels;

namespace Valt.UI.Views.Main.Tabs.Reports;

public partial class ReportsViewModel : ValtTabViewModel, IDisposable
{
    private readonly IAllTimeHighReport _allTimeHighReport = null!;
    private readonly IMaxBtcStackReport _maxBtcStackReport = null!;
    private readonly IMonthlyTotalsReport _monthlyTotalsReport = null!;
    private readonly IExpensesByCategoryReport _expensesByCategoryReport = null!;
    private readonly IIncomeByCategoryReport _incomeByCategoryReport = null!;
    private readonly IStatisticsReport _statisticsReport = null!;
    private readonly IWealthOverviewReport _wealthOverviewReport = null!;
    private readonly IReportDataProviderFactory _reportDataProviderFactory = null!;
    private readonly CurrencySettings _currencySettings = null!;
    private readonly ILocalDatabase _localDatabase = null!;
    private readonly IClock _clock = null!;
    private readonly ILogger<ReportsViewModel> _logger = null!;
    private readonly AccountsTotalState _accountsTotalState = null!;
    private readonly RatesState _ratesState = null!;
    private readonly CustomBtcPriceState _customBtcPriceState = null!;
    private readonly SecureModeState _secureModeState = null!;
    private readonly IConfigurationManager _configurationManager = null!;
    private readonly IModalFactory _modalFactory = null!;
    private readonly IQueryDispatcher _queryDispatcher = null!;
    private readonly IIndicatorCache _indicatorCache = null!;
    private readonly IFireAndForgetTaskRunner _runner = null!;
    private readonly IndicatorsPanelViewModel _indicatorsPanel;
    private readonly WealthPanelViewModel _wealthPanel;
    private readonly BtcStackPanelViewModel _btcStackPanel;
    private readonly SimulatedPricesPanelViewModel _simulatedPricesPanel;

    // Cached provider for the lifetime of the tab being active
    private IReportDataProvider? _cachedProvider;

    // Cached ATH fiat value for reuse by leverage positions panel
    private decimal? _allTimeHighFiatValue;

    [ObservableProperty] private DashboardData _wealthData = DashboardData.Empty;
    [ObservableProperty] private DashboardData _allTimeHighData = DashboardData.Empty;
    [ObservableProperty] private DashboardData _btcStackData = DashboardData.Empty;
    [ObservableProperty] private DashboardData _leveragePositionsData = DashboardData.Empty;
    [ObservableProperty] private DashboardData _btcLoansData = DashboardData.Empty;
    [ObservableProperty] private DashboardData _statisticsData = DashboardData.Empty;
    [ObservableProperty] private DashboardData _simulatedPricesData = DashboardData.Empty;
    [ObservableProperty] private DashboardData _indicatorsData = DashboardData.Empty;
    [ObservableProperty] private AvaloniaList<MonthlyReportItemViewModel> _monthlyReportItems = new();
    [ObservableProperty] private MonthlyTotalsChartData _monthlyTotalsChartData = new();
    [ObservableProperty] private ExpensesByCategoryChartData _expensesByCategoryChartData = new();
    [ObservableProperty] private WealthOverviewChartData _wealthOverviewChartData = new();
    [ObservableProperty] private WealthOverviewPeriod _selectedWealthOverviewPeriod = WealthOverviewPeriod.Monthly;
    [ObservableProperty] private int _selectedWealthOverviewMaxElements = 12;
    [ObservableProperty] private DateTime _filterMainDate;
    [ObservableProperty] private DateRange _filterRange = new(DateTime.MinValue, DateTime.MinValue);
    [ObservableProperty] private DateTime _categoryFilterMainDate;
    [ObservableProperty] private DateRange _categoryFilterRange = new(DateTime.MinValue, DateTime.MinValue);

    [ObservableProperty] private bool _isWealthLoading = true;
    [ObservableProperty] private bool _isAllTimeHighLoading = true;
    [ObservableProperty] private bool _isBtcStackLoading = true;
    [ObservableProperty] private bool _isLeveragePositionsLoading = true;
    [ObservableProperty] private bool _isLeveragePositionsVisible;
    [ObservableProperty] private bool _isBtcLoansLoading = true;
    [ObservableProperty] private bool _isBtcLoansVisible;
    [ObservableProperty] private bool _isStatisticsLoading = true;
    [ObservableProperty] private bool _isSimulatedPricesLoading = true;
    [ObservableProperty] private bool _isIndicatorsLoading = true;
    [ObservableProperty] private bool _isMonthlyTotalsLoading = true;
    [ObservableProperty] private bool _isSpendingByCategoriesLoading = true;
    [ObservableProperty] private bool _isIncomeByCategoriesLoading = true;
    [ObservableProperty] private bool _isWealthOverviewLoading = true;
    
    // Fixed price simulation
    [ObservableProperty] private decimal? _customBtcPrice;
    [ObservableProperty] private string _currentBtcPriceFormatted = string.Empty;
    [ObservableProperty] private bool _isCustomPriceActive;
    [ObservableProperty] private string _simulateButtonText = string.Empty;

    public bool IsSecureModeEnabled => _secureModeState.IsEnabled;

    public IndicatorsPanelViewModel IndicatorsPanel => _indicatorsPanel;
    public WealthPanelViewModel WealthPanel => _wealthPanel;
    public BtcStackPanelViewModel BtcStackPanel => _btcStackPanel;
    public SimulatedPricesPanelViewModel SimulatedPricesPanel => _simulatedPricesPanel;

    // Pie chart filter collections (expenses)
    [ObservableProperty] private AvaloniaList<SelectItem> _availableAccounts = new();
    [ObservableProperty] private AvaloniaList<SelectItem> _selectedAccounts = new();
    [ObservableProperty] private AvaloniaList<SelectItem> _availableCategories = new();
    [ObservableProperty] private AvaloniaList<SelectItem> _selectedCategories = new();

    // Income by category
    [ObservableProperty] private IncomeByCategoryChartData _incomeByCategoryChartData = new();

    private CancellationTokenSource? _filterDebounceTokenSource;
    private const int FilterDebounceDelayMs = 300;

    private bool _ready;

    public ReportsViewModel(IAllTimeHighReport allTimeHighReport,
        IMaxBtcStackReport maxBtcStackReport,
        IMonthlyTotalsReport monthlyTotalsReport,
        IExpensesByCategoryReport expensesByCategoryReport,
        IIncomeByCategoryReport incomeByCategoryReport,
        IStatisticsReport statisticsReport,
        IWealthOverviewReport wealthOverviewReport,
        IReportDataProviderFactory reportDataProviderFactory,
        CurrencySettings currencySettings,
        ILocalDatabase localDatabase,
        IClock clock,
        ILogger<ReportsViewModel> logger,
        AccountsTotalState accountsTotalState,
        RatesState ratesState,
        CustomBtcPriceState customBtcPriceState,
        SecureModeState secureModeState,
        IConfigurationManager configurationManager,
        IModalFactory modalFactory,
        IQueryDispatcher queryDispatcher,
        IIndicatorCache indicatorCache,
        IFireAndForgetTaskRunner runner,
        IndicatorsPanelViewModel indicatorsPanel,
        WealthPanelViewModel wealthPanel,
        BtcStackPanelViewModel btcStackPanel,
        SimulatedPricesPanelViewModel simulatedPricesPanel)
    {
        _allTimeHighReport = allTimeHighReport;
        _maxBtcStackReport = maxBtcStackReport;
        _monthlyTotalsReport = monthlyTotalsReport;
        _expensesByCategoryReport = expensesByCategoryReport;
        _incomeByCategoryReport = incomeByCategoryReport;
        _statisticsReport = statisticsReport;
        _wealthOverviewReport = wealthOverviewReport;
        _reportDataProviderFactory = reportDataProviderFactory;
        _currencySettings = currencySettings;
        _localDatabase = localDatabase;
        _clock = clock;
        _logger = logger;
        _accountsTotalState = accountsTotalState;
        _ratesState = ratesState;
        _customBtcPriceState = customBtcPriceState;
        _secureModeState = secureModeState;
        _configurationManager = configurationManager;
        _modalFactory = modalFactory;
        _queryDispatcher = queryDispatcher;
        _indicatorCache = indicatorCache;
        _runner = runner;
        _indicatorsPanel = indicatorsPanel;
        _wealthPanel = wealthPanel;
        _btcStackPanel = btcStackPanel;
        _simulatedPricesPanel = simulatedPricesPanel;

        _wealthPanel.PropertyChanged += OnWealthPanelPropertyChanged;
        _btcStackPanel.PropertyChanged += OnBtcStackPanelPropertyChanged;
        _simulatedPricesPanel.PropertyChanged += OnSimulatedPricesPanelPropertyChanged;
        _indicatorsPanel.PropertyChanged += OnIndicatorsPanelPropertyChanged;

        SimulateButtonText = language.Reports_SimulateButton;
        UpdateCurrentBtcPriceFormatted();

        _secureModeState.PropertyChanged += OnSecureModeStatePropertyChanged;

        FilterMainDate = CategoryFilterMainDate = _clock.GetCurrentDateTimeUtc();
        FilterRange = new DateRange(new DateTime(FilterMainDate.Year, 1, 1), new DateTime(FilterMainDate.Year, 12, 31));
        var currentMonth = new DateTime(CategoryFilterMainDate.Year, CategoryFilterMainDate.Month, 1);
        CategoryFilterRange = new DateRange(currentMonth, currentMonth.AddMonths(1).AddDays(-1));

        PrepareAccountsAndCategoriesList();

        SelectedAccounts.CollectionChanged += OnSelectedFiltersChanged;
        SelectedCategories.CollectionChanged += OnSelectedFiltersChanged;

        WeakReferenceMessenger.Default.Register<SettingsChangedMessage>(this, (recipient, message) =>
        {
            switch (message.PropertyName)
            {
                case nameof(CurrencySettings.MainFiatCurrency):
                    IsAllTimeHighLoading = true;
                    IsStatisticsLoading = true;
                    IsSimulatedPricesLoading = true;
                    IsMonthlyTotalsLoading = true;
                    IsSpendingByCategoriesLoading = true;
                    IsIncomeByCategoriesLoading = true;
                    IsWealthOverviewLoading = true;
                    IsBtcLoansLoading = true;
                    // Reload data when currency changes
                    ReloadDataAndFetchAllReportsAsync().FireAndForgetSafeAsync(_runner, _logger);
                    _simulatedPricesPanel.Refresh();
                    UpdateBtcLoansDataAsync().FireAndForgetSafeAsync(_runner, _logger);
                    break;
            }
        });

        _accountsTotalState.PropertyChanged += OnAccountsTotalStatePropertyChanged;
        _ratesState.PropertyChanged += OnRatesStatePropertyChanged;

        WeakReferenceMessenger.Default.Register<AssetSummaryUpdatedMessage>(this, (recipient, message) =>
        {
            UpdateLeveragePositionsDataAsync().FireAndForgetSafeAsync(_runner, _logger);
            UpdateBtcLoansDataAsync().FireAndForgetSafeAsync(_runner, _logger);
        });

        WeakReferenceMessenger.Default.Register<IndicatorsUpdatedMessage>(this, (recipient, message) =>
        {
            Dispatcher.UIThread.Post(() => _indicatorsPanel.UpdateIndicatorsData(message.Snapshot));
        });

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

        LoadDataAndFetchAllReportsAsync()
            .ContinueWith(_ => UpdateLeveragePositionsDataAsync(), TaskScheduler.Default)
            .ContinueWith(_ => UpdateBtcLoansDataAsync(), TaskScheduler.Default)
            .FireAndForgetSafeAsync(_runner, _logger);
        _wealthPanel.Refresh();
        _btcStackPanel.Refresh();
        _simulatedPricesPanel.Refresh();
        _indicatorsPanel.RefreshAsync().FireAndForgetSafeAsync(_runner, _logger);
    }

    /// <summary>
    /// Unloads the cached provider to free memory when the tab is no longer active
    /// </summary>
    public void UnloadData()
    {
        _cachedProvider = null;
        _logger.LogDebug("Report data provider unloaded");
    }

    private async Task<IReportDataProvider> GetOrCreateProviderAsync()
    {
        _cachedProvider ??= await _reportDataProviderFactory.CreateAsync();
        return _cachedProvider;
    }

    private async Task LoadDataAndFetchAllReportsAsync()
    {
        // Create and cache the provider with parallel data loading
        _cachedProvider = await _reportDataProviderFactory.CreateAsync();

        // Determine the best default period based on transaction history
        SelectedWealthOverviewPeriod = DetermineDefaultWealthOverviewPeriod(_cachedProvider);

        await FetchAllReportsAsync(_cachedProvider);
    }

    private WealthOverviewPeriod DetermineDefaultWealthOverviewPeriod(IReportDataProvider provider)
    {
        if (provider.AllTransactions.Count == 0)
            return WealthOverviewPeriod.Monthly;

        var today = _clock.GetCurrentLocalDate();
        var minDate = provider.MinTransactionDate;
        var daysDiff = today.DayNumber - minDate.DayNumber;

        // Calculate approximate data points for each period
        var weeksOfData = daysDiff / 7;
        var monthsOfData = ((today.Year - minDate.Year) * 12) + (today.Month - minDate.Month);

        // Start with Daily, upgrade when at least 4 data points available
        // If 4+ months of data, use Monthly
        if (monthsOfData >= 4)
            return WealthOverviewPeriod.Monthly;

        // If 4+ weeks of data, use Weekly
        if (weeksOfData >= 4)
            return WealthOverviewPeriod.Weekly;

        // Default to Daily for new users
        return WealthOverviewPeriod.Daily;
    }

    private async Task ReloadDataAndFetchAllReportsAsync()
    {
        // Force refresh the provider (data might have changed)
        _cachedProvider = await _reportDataProviderFactory.CreateAsync(forceRefresh: true);

        await FetchAllReportsAsync(_cachedProvider);
    }

    private async Task FetchAllReportsAsync(IReportDataProvider provider)
    {
        await Task.WhenAll(
            FetchMonthlyTotalsAsync(provider),
            FetchExpensesByCategoryAsync(provider),
            FetchIncomeByCategoryAsync(provider),
            FetchAllTimeHighDataAsync(provider),
            FetchMaxBtcStackDataAsync(provider),
            FetchStatisticsDataAsync(provider),
            FetchWealthOverviewAsync(provider));
    }

    private void PrepareAccountsAndCategoriesList()
    {
        AvailableAccounts.Clear();
        SelectedAccounts.Clear();
        AvailableCategories.Clear();
        SelectedCategories.Clear();

        var accounts = GetFilterableAccounts();

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

        // Apply saved expense category filter
        var expenseExcluded = _configurationManager.GetExpensesCategoryFilterExcludedIds().ToHashSet();
        if (expenseExcluded.Count > 0)
        {
            var toRemove = SelectedCategories.Where(c => expenseExcluded.Contains(c.Id)).ToList();
            foreach (var item in toRemove)
                SelectedCategories.Remove(item);
        }
    }

    private IEnumerable<SelectItem> GetFilterableAccounts()
    {
        var allAccounts = _localDatabase.GetAccounts().FindAll().ToList();

        var hiddenAccountIds = allAccounts
            .Where(x => !x.Visible)
            .Select(x => x.Id)
            .ToHashSet();

        var hiddenAccountIdsWithTransactions = new HashSet<LiteDB.ObjectId>();
        if (hiddenAccountIds.Count > 0)
        {
            var transactions = _localDatabase.GetTransactions()
                .Find(t => t.Date >= CategoryFilterRange.Start && t.Date <= CategoryFilterRange.End);

            foreach (var t in transactions)
            {
                if (hiddenAccountIds.Contains(t.FromAccountId))
                    hiddenAccountIdsWithTransactions.Add(t.FromAccountId);
                if (t.ToAccountId is not null && hiddenAccountIds.Contains(t.ToAccountId))
                    hiddenAccountIdsWithTransactions.Add(t.ToAccountId);
            }
        }

        return allAccounts
            .Where(x => x.Visible || hiddenAccountIdsWithTransactions.Contains(x.Id))
            .OrderByDescending(x => x.Visible)
            .ThenBy(x => x.DisplayOrder)
            .Select(x => new SelectItem(x.Id.ToString(), x.Name));
    }

    private void RefreshAvailableAccounts()
    {
        var previousSelectedIds = SelectedAccounts.Select(x => x.Id).ToHashSet();

        SelectedAccounts.CollectionChanged -= OnSelectedFiltersChanged;

        try
        {
            var accounts = GetFilterableAccounts().ToList();

            AvailableAccounts.Clear();
            SelectedAccounts.Clear();

            AvailableAccounts.AddRange(accounts);
            SelectedAccounts.AddRange(accounts.Where(a => previousSelectedIds.Contains(a.Id)));
        }
        finally
        {
            SelectedAccounts.CollectionChanged += OnSelectedFiltersChanged;
        }
    }

    partial void OnFilterRangeChanged(DateRange value)
    {
        if (!_ready) return;

        IsMonthlyTotalsLoading = true;
        FetchMonthlyTotalsWithProviderAsync().FireAndForgetSafeAsync(_runner, _logger);
    }

    private async Task FetchMonthlyTotalsWithProviderAsync()
    {
        var provider = await GetOrCreateProviderAsync();
        await FetchMonthlyTotalsAsync(provider);
    }

    partial void OnCategoryFilterRangeChanged(DateRange value)
    {
        if (!_ready) return;

        RefreshAvailableAccounts();
        IsSpendingByCategoriesLoading = true;
        IsIncomeByCategoriesLoading = true;
        FetchCategoriesWithProviderAsync().FireAndForgetSafeAsync(_runner, _logger);
    }

    private async Task FetchCategoriesWithProviderAsync()
    {
        var provider = await GetOrCreateProviderAsync();
        await Task.WhenAll(
            FetchExpensesByCategoryAsync(provider),
            FetchIncomeByCategoryAsync(provider));
    }

    private void OnSelectedFiltersChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!_ready) return;

        // Cancel any pending debounced fetch
        _filterDebounceTokenSource?.Cancel();
        _filterDebounceTokenSource = new CancellationTokenSource();

        IsSpendingByCategoriesLoading = true;
        IsIncomeByCategoriesLoading = true;
        DebouncedFetchCategoriesAsync(_filterDebounceTokenSource.Token).FireAndForgetSafeAsync(_runner, _logger);
    }

    private async Task DebouncedFetchCategoriesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(FilterDebounceDelayMs, cancellationToken);
            var provider = await GetOrCreateProviderAsync();
            await Task.WhenAll(
                FetchExpensesByCategoryAsync(provider),
                FetchIncomeByCategoryAsync(provider));
        }
        catch (TaskCanceledException)
        {
            // Debounce cancelled, ignore
        }
    }

    [RelayCommand]
    private async Task SaveExpensesCategoryFilter()
    {
        var ownerWindow = GetUserControlOwnerWindow?.Invoke();
        if (ownerWindow is null) return;

        var confirmed = await MessageBoxHelper.ShowQuestionAsync(
            language.Reports_CategoryFilter_SaveConfirmTitle,
            language.Reports_CategoryFilter_SaveConfirmMessage,
            ownerWindow);

        if (!confirmed) return;

        var selectedIds = SelectedCategories.Select(c => c.Id).ToHashSet();
        var excludedIds = AvailableCategories.Where(c => !selectedIds.Contains(c.Id)).Select(c => c.Id);
        _configurationManager.SetExpensesCategoryFilterExcludedIds(excludedIds);

        await MessageBoxHelper.ShowAlertAsync(
            language.Reports_CategoryFilter_SaveConfirmTitle,
            language.Reports_CategoryFilter_SaveSuccess,
            ownerWindow);
    }

    [RelayCommand]
    private void LoadExpensesCategoryFilter()
    {
        var excludedIds = _configurationManager.GetExpensesCategoryFilterExcludedIds().ToHashSet();
        if (excludedIds.Count == 0)
        {
            // No saved filter — select all
            SelectedCategories.Clear();
            SelectedCategories.AddRange(AvailableCategories);
            return;
        }

        var newSelection = AvailableCategories.Where(c => !excludedIds.Contains(c.Id)).ToList();
        SelectedCategories.Clear();
        SelectedCategories.AddRange(newSelection);
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
                rows.Add(RowItem.Separator());
                rows.Add(new RowItem(language.Reports_AllTimeHigh_MaxDrawdownPercent,
                    $"{allTimeHighData.MaxDrawdownPercent.Value}%"));
                rows.Add(new RowItem(language.Reports_AllTimeHigh_MaxDrawdownDate,
                    allTimeHighData.MaxDrawdownDate.Value.ToString()));
            }

            // Add BTC price to hit ATH calculation
            var currentWealth = _accountsTotalState.CurrentWealth;
            var currentFiat = currentWealth.WealthInMainFiatCurrency;
            var currentBtcInBtc = currentWealth.WealthInSats / 100_000_000m;

            if (currentBtcInBtc > 0)
            {
                var targetAthValue = allTimeHighData.Value.Value;
                _allTimeHighFiatValue = targetAthValue;
                var requiredBtcPriceValue = targetAthValue - currentFiat;

                if (requiredBtcPriceValue > 0)
                {
                    var requiredBtcPrice = requiredBtcPriceValue / currentBtcInBtc;
                    rows.Add(new RowItem(language.Reports_AllTimeHigh_BtcPriceToHitAth,
                        CurrencyDisplay.FormatFiat(requiredBtcPrice, fiatCurrency.Code)));
                }
            }

            AllTimeHighData = new DashboardData(language.Reports_AllTimeHigh_Title, rows, Icon: "\uE8E5");

            IsAllTimeHighLoading = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all time high data");
            AllTimeHighData = new DashboardData(language.Reports_AllTimeHigh_Title,
                new ObservableCollection<RowItem>
                {
                    new(language.Error, ex.Message)
                },
                Icon: "\uE8E5");
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

            // Get excluded category IDs from configuration
            var excludedCategoryIds = _configurationManager.GetStatisticsExcludedCategoryIds().ToHashSet();

            var statisticsData = await _statisticsReport.GetAsync(
                fiatCurrency,
                currentWealthInFiat,
                provider,
                excludedCategoryIds.Count > 0 ? excludedCategoryIds : null);

            var rows = new ObservableCollection<RowItem>
            {
                new(language.Reports_Statistics_MedianExpenses,
                    $"{CurrencyDisplay.FormatFiat(statisticsData.MedianMonthlyExpenses, fiatCurrency.Code)}")
            };

            // Add previous period median and evolution if available
            if (statisticsData.HasMedianMonthlyExpensesPreviousPeriod && statisticsData.MedianMonthlyExpensesPreviousPeriod is not null)
            {
                rows.Add(new RowItem(language.Reports_Statistics_MedianExpensesPrevious,
                    $"{CurrencyDisplay.FormatFiat(statisticsData.MedianMonthlyExpensesPreviousPeriod, fiatCurrency.Code)}"));

                if (statisticsData.MedianMonthlyExpensesEvolution.HasValue)
                {
                    var evolutionSign = statisticsData.MedianMonthlyExpensesEvolution.Value >= 0 ? "+" : "";
                    var evolutionFormatted = $"{evolutionSign}{statisticsData.MedianMonthlyExpensesEvolution.Value}%";
                    rows.Add(new RowItem(language.Reports_Statistics_MedianExpensesEvolution, evolutionFormatted, TooltipContent.Text(language.Reports_Statistics_MedianExpensesEvolution_Tooltip)));
                }
            }

            // Add sat-based median data if available (from AutoSatAmount calculations)
            if (statisticsData.HasMedianMonthlyExpensesSats && statisticsData.MedianMonthlyExpensesSats.HasValue)
            {
                rows.Add(RowItem.Separator());
                var satMedianFormatted = statisticsData.MedianMonthlyExpensesSats.Value.ToString("N0", CultureInfo.CurrentCulture) + " sats";
                rows.Add(new RowItem(language.Reports_Statistics_MedianExpensesSatsLabel, satMedianFormatted));

                if (statisticsData.MedianMonthlyExpensesPreviousPeriodSats.HasValue)
                {
                    var prevSatMedianFormatted = statisticsData.MedianMonthlyExpensesPreviousPeriodSats.Value.ToString("N0", CultureInfo.CurrentCulture) + " sats";
                    rows.Add(new RowItem(language.Reports_Statistics_MedianExpensesSatsPrevious, prevSatMedianFormatted));

                    if (statisticsData.MedianMonthlyExpensesSatsEvolution.HasValue)
                    {
                        var satEvolutionSign = statisticsData.MedianMonthlyExpensesSatsEvolution.Value >= 0 ? "+" : "";
                        var satEvolutionFormatted = $"{satEvolutionSign}{statisticsData.MedianMonthlyExpensesSatsEvolution.Value}%";
                        rows.Add(new RowItem(language.Reports_Statistics_MedianExpensesSatsEvolution, satEvolutionFormatted, TooltipContent.Text(language.Reports_Statistics_MedianExpensesSatsEvolution_Tooltip)));
                    }
                }
            }

            rows.Add(new RowItem(language.Reports_Statistics_WealthCoverage, statisticsData.WealthCoverageFormatted, TooltipContent.Text(language.Reports_Statistics_WealthCoverage_Tooltip)));

            StatisticsData = new DashboardData(language.Reports_Statistics_Title, rows, OpenStatisticsConfigCommand, "\uE4FC");

            IsStatisticsLoading = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching statistics data");
            StatisticsData = new DashboardData(language.Reports_Statistics_Title,
                new ObservableCollection<RowItem>
                    {
                        new(language.Error, ex.Message)
                    },
                OpenStatisticsConfigCommand,
                "\uE4FC");
        }
        finally
        {
            IsStatisticsLoading = false;
        }
    }

    [RelayCommand]
    private async Task OpenStatisticsConfig()
    {
        var ownerWindow = GetUserControlOwnerWindow?.Invoke();
        if (ownerWindow is null)
            return;

        var modal = (StatisticsConfigView)await _modalFactory.CreateAsync(
            ApplicationModalNames.StatisticsConfig,
            ownerWindow);

        var result = await modal.ShowDialogSafeAsync<StatisticsConfigViewModel.Response?>(ownerWindow);

        // Refresh statistics after config change
        if (result?.Ok == true)
        {
            IsStatisticsLoading = true;
            var provider = await GetOrCreateProviderAsync();
            await FetchStatisticsDataAsync(provider);
        }
    }

    private void UpdateWealthData()
    {
        _wealthPanel.Refresh();
    }

    private void UpdateBtcStackData()
    {
        _btcStackPanel.Refresh();
    }

    private void UpdateSimulatedPricesData()
    {
        _simulatedPricesPanel.Refresh();
    }

    [RelayCommand]
    private async Task OpenSimulatedPricesConfig()
    {
        var ownerWindow = GetUserControlOwnerWindow?.Invoke();
        if (ownerWindow is null)
            return;

        var modal = (SimulatedPricesConfigView)await _modalFactory.CreateAsync(
            ApplicationModalNames.SimulatedPricesConfig,
            ownerWindow);

        var result = await modal.ShowDialogSafeAsync<SimulatedPricesConfigViewModel.Response?>(ownerWindow);

        if (result?.Ok == true)
        {
            _simulatedPricesPanel.Refresh();
        }
    }

    [RelayCommand]
    private async Task OpenFixedPriceConfig()
    {
        var ownerWindow = GetUserControlOwnerWindow?.Invoke();
        if (ownerWindow is null)
            return;

        var modal = (FixedPriceConfigView)await _modalFactory.CreateAsync(
            ApplicationModalNames.FixedPriceConfig,
            ownerWindow);

        var viewModel = (FixedPriceConfigViewModel)modal.DataContext!;
        viewModel.OwnerWindow = ownerWindow;
        viewModel.CurrencySymbol = _currencySettings.MainFiatCurrency;
        var currentPrice = CustomBtcPrice ?? GetCurrentBtcPriceInMainFiat();
        viewModel.PriceText = currentPrice.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);

        var result = await modal.ShowDialogSafeAsync<FixedPriceConfigViewModel.Response?>(ownerWindow);

        if (result?.Ok == true)
        {
            var mainCurrency = _currencySettings.MainFiatCurrency;
            var fiatRate = _ratesState.FiatRates?.GetValueOrDefault(mainCurrency, 1m) ?? 1m;
            var priceInUsd = result.Price / fiatRate;
            
            CustomBtcPrice = result.Price;
            IsCustomPriceActive = true;
            SimulateButtonText = language.Reports_ChangePriceButton;
            _customBtcPriceState.CustomBtcPriceUsd = priceInUsd;
            UpdateCurrentBtcPriceFormatted();
            TriggerCustomPriceRecalculations();
        }
    }

    [RelayCommand]
    private void ResetFixedPrice()
    {
        CustomBtcPrice = null;
        IsCustomPriceActive = false;
        SimulateButtonText = language.Reports_SimulateButton;
        _customBtcPriceState.CustomBtcPriceUsd = null;
        UpdateCurrentBtcPriceFormatted();
        TriggerCustomPriceRecalculations();
    }

    private void TriggerCustomPriceRecalculations()
    {
        _wealthPanel.Refresh();
        _btcStackPanel.Refresh();
        _simulatedPricesPanel.Refresh();
        UpdateLeveragePositionsDataAsync().FireAndForgetSafeAsync(_runner, _logger);
        UpdateBtcLoansDataAsync().FireAndForgetSafeAsync(_runner, _logger);
    }

    private decimal GetCurrentBtcPriceInMainFiat()
    {
        if (_ratesState.BitcoinPrice.HasValue && _ratesState.FiatRates is not null)
        {
            var mainCurrency = _currencySettings.MainFiatCurrency;
            var fiatRate = _ratesState.FiatRates.GetValueOrDefault(mainCurrency, 1m);
            return _ratesState.BitcoinPrice.Value * fiatRate;
        }
        return 0m;
    }

    private void UpdateCurrentBtcPriceFormatted()
    {
        decimal price;
        if (CustomBtcPrice.HasValue)
        {
            price = CustomBtcPrice.Value;
        }
        else if (_ratesState.BitcoinPrice.HasValue && _ratesState.FiatRates is not null)
        {
            var mainCurrency = _currencySettings.MainFiatCurrency;
            var fiatRate = _ratesState.FiatRates.GetValueOrDefault(mainCurrency, 1m);
            price = _ratesState.BitcoinPrice.Value * fiatRate;
        }
        else
        {
            price = 0m;
        }
        
        var currencyCode = _currencySettings.MainFiatCurrency;
        CurrentBtcPriceFormatted = CurrencyDisplay.FormatFiat(price, currencyCode);
    }

    private async Task FetchMaxBtcStackDataAsync(IReportDataProvider provider)
    {
        await _btcStackPanel.FetchMaxBtcStackAsync(provider);
    }

    /// <summary>
    /// Calculates the simulated P&L for a leveraged position when BTC price is simulated.
    /// Only recalculates if the position is a BTC position (Symbol starts with "BTC").
    /// </summary>
    private decimal? CalculateSimulatedLeveragedPnl(AssetDTO position, IReadOnlyDictionary<string, decimal> fiatRates)
    {
        // If no custom price is active, return the stored P&L
        if (!_customBtcPriceState.IsActive || !_customBtcPriceState.CustomBtcPriceUsd.HasValue)
            return position.PnL;

        // Only recalculate for BTC positions
        if (string.IsNullOrWhiteSpace(position.Symbol) ||
            !position.Symbol.StartsWith("BTC", StringComparison.OrdinalIgnoreCase))
            return position.PnL;

        if (!position.Collateral.HasValue || !position.Leverage.HasValue ||
            !position.EntryPrice.HasValue || position.EntryPrice.Value == 0)
            return position.PnL;

        // Get simulated BTC price in position's currency
        var simulatedPriceUsd = _customBtcPriceState.CustomBtcPriceUsd.Value;
        decimal simulatedPrice;
        if (position.CurrencyCode == FiatCurrency.Usd.Code)
        {
            simulatedPrice = simulatedPriceUsd;
        }
        else if (fiatRates.TryGetValue(position.CurrencyCode, out var rate))
        {
            simulatedPrice = simulatedPriceUsd * rate;
        }
        else
        {
            return position.PnL;
        }

        // Calculate P&L using leveraged position formula
        var priceChange = (simulatedPrice - position.EntryPrice.Value) / position.EntryPrice.Value;
        var leveragedChange = priceChange * position.Leverage.Value;

        decimal currentValue;
        if (position.IsLong.HasValue && position.IsLong.Value)
            currentValue = position.Collateral.Value * (1 + leveragedChange);
        else
            currentValue = position.Collateral.Value * (1 - leveragedChange);

        return currentValue - position.Collateral.Value;
    }

    private async Task UpdateLeveragePositionsDataAsync()
    {
        try
        {
            IsLeveragePositionsLoading = true;

            var assets = await _queryDispatcher.DispatchAsync(new GetVisibleAssetsQuery());

            // Filter to leveraged positions that are visible and included in net worth
            var leveragedPositions = assets
                .Where(a => a.IsLong.HasValue && a.IncludeInNetWorth && a.Visible)
                .ToList();

            if (leveragedPositions.Count == 0)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    IsLeveragePositionsVisible = false;
                    IsLeveragePositionsLoading = false;
                });
                return;
            }

            var wealth = _accountsTotalState.CurrentWealth;
            var btcSpotSats = wealth.WealthInSats;

            // Calculate BTC exposure from leveraged positions
            decimal totalBtcExposure = 0;
            foreach (var position in leveragedPositions)
            {
                if (!position.Collateral.HasValue || !position.Leverage.HasValue || !position.EntryPrice.HasValue || position.EntryPrice.Value == 0)
                    continue;

                // Notional value = Collateral * Leverage
                var notionalValue = position.Collateral.Value * position.Leverage.Value;
                // BTC exposure = Notional / Entry Price
                var btcExposure = notionalValue / position.EntryPrice.Value;

                // Apply direction: Long = positive, Short = negative
                if (!position.IsLong!.Value)
                    btcExposure = -btcExposure;

                totalBtcExposure += btcExposure;
            }

            // Convert BTC exposure to sats
            var exposureSats = (long)(totalBtcExposure * 100_000_000m);

            // Leveraged stack = BTC spot + exposure from leverage
            var leveragedStackSats = btcSpotSats + exposureSats;

            // Calculate leverage percentage: |exposure| / |leveraged stack| * 100
            decimal leveragePercentage = 0;
            if (leveragedStackSats != 0)
            {
                leveragePercentage = Math.Abs(totalBtcExposure) / Math.Abs(leveragedStackSats / 100_000_000m) * 100m;
            }

            // Calculate total P&L in main fiat currency
            var fiatRates = _ratesState.FiatRates;
            var btcPrice = _ratesState.BitcoinPrice;
            var mainCurrency = _currencySettings.MainFiatCurrency;
            decimal totalPnlInMainFiat = 0;

            if (fiatRates != null && btcPrice.HasValue)
            {
                foreach (var position in leveragedPositions)
                {
                    var pnl = CalculateSimulatedLeveragedPnl(position, fiatRates);
                    if (!pnl.HasValue) continue;
                    var currency = position.CurrencyCode;

                    if (currency == mainCurrency)
                        totalPnlInMainFiat += pnl.Value;
                    else if (currency == FiatCurrency.Usd.Code)
                        totalPnlInMainFiat += pnl.Value * fiatRates[mainCurrency];
                    else if (fiatRates.ContainsKey(currency))
                        totalPnlInMainFiat += (pnl.Value / fiatRates[currency]) * fiatRates[mainCurrency];
                }
            }
            totalPnlInMainFiat = Math.Round(totalPnlInMainFiat, 2);

            // Calculate P&L in BTC (sats)
            long pnlInSats = 0;
            if (fiatRates != null && btcPrice.HasValue && fiatRates.ContainsKey(mainCurrency))
            {
                var mainFiatRate = fiatRates[mainCurrency];
                pnlInSats = BtcPriceCalculator.CalculateBtcAmountOfFiat(
                    totalPnlInMainFiat, mainFiatRate, btcPrice.Value);
            }

            // Calculate BTC price to hit ATH using leveraged stack
            decimal? requiredBtcPriceLeveraged = null;
            if (_allTimeHighFiatValue.HasValue)
            {
                var currentFiat = wealth.WealthInMainFiatCurrency;
                var leveragedStackBtc = leveragedStackSats / 100_000_000m;
                if (leveragedStackBtc > 0)
                {
                    var requiredFiatDiff = _allTimeHighFiatValue.Value - currentFiat;
                    if (requiredFiatDiff > 0)
                        requiredBtcPriceLeveraged = requiredFiatDiff / leveragedStackBtc;
                }
            }

            var fiatCurrency = FiatCurrency.GetFromCode(mainCurrency);
            var leveragedStackFormatted = CurrencyDisplay.FormatSatsAsBitcoin(leveragedStackSats) + " BTC";
            var exposureFormatted = (totalBtcExposure >= 0 ? "+" : "") + totalBtcExposure.ToString("0.########") + " BTC";
            var leveragePercentFormatted = leveragePercentage.ToString("0.##") + "%";
            var positionCountFormatted = leveragedPositions.Count.ToString();
            var pnlFiatFormatted = (totalPnlInMainFiat >= 0 ? "+" : "")
                + CurrencyDisplay.FormatFiat(totalPnlInMainFiat, fiatCurrency.Code);
            var pnlBtcFormatted = (pnlInSats >= 0 ? "+" : "")
                + CurrencyDisplay.FormatSatsAsBitcoin(pnlInSats) + " BTC";

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var rows = new ObservableCollection<RowItem>
                {
                    new(language.Reports_LeveragePositions_LeveragedStack, leveragedStackFormatted, TooltipContent.Text(language.Reports_LeveragePositions_LeveragedStack_Tooltip)),
                    new(language.Reports_LeveragePositions_LeverageExposure, exposureFormatted),
                    new(language.Reports_LeveragePositions_LeveragePercentage, leveragePercentFormatted, TooltipContent.Text(language.Reports_LeveragePositions_LeveragePercentage_Tooltip)),
                    new(language.Reports_LeveragePositions_PositionCount, positionCountFormatted),
                    new(language.Reports_LeveragePositions_CurrentResult, pnlFiatFormatted),
                    new(language.Reports_LeveragePositions_CurrentResultBtc, pnlBtcFormatted)
                };

                if (requiredBtcPriceLeveraged.HasValue)
                    rows.Add(new RowItem(language.Reports_LeveragePositions_BtcPriceToHitAth,
                        CurrencyDisplay.FormatFiat(requiredBtcPriceLeveraged.Value, fiatCurrency.Code)));

                LeveragePositionsData = new DashboardData(language.Reports_LeveragePositions_Title, rows, Icon: "\uEA0B");
                IsLeveragePositionsVisible = true;
                IsLeveragePositionsLoading = false;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating leverage positions data");
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsLeveragePositionsVisible = false;
                IsLeveragePositionsLoading = false;
            });
        }
    }

    private async Task UpdateBtcLoansDataAsync()
    {
        try
        {
            IsBtcLoansLoading = true;

            var fiatRates = _ratesState.FiatRates;
            var btcPrice = _ratesState.BitcoinPrice;
            var mainCurrency = _currencySettings.MainFiatCurrency;
            var stackSats = _accountsTotalState.CurrentWealth.WealthInSats;

            if (fiatRates is null || !btcPrice.HasValue)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    IsBtcLoansVisible = false;
                    IsBtcLoansLoading = false;
                });
                return;
            }

            var dto = await _queryDispatcher.DispatchAsync(new GetBtcLoansDashboardQuery
            {
                MainCurrencyCode = mainCurrency,
                BtcPriceUsd = btcPrice,
                CustomBtcPriceUsd = _customBtcPriceState.CustomBtcPriceUsd,
                FiatRates = fiatRates,
                TotalBtcStackSats = stackSats
            });

            if (!dto.HasActiveLoans)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    IsBtcLoansVisible = false;
                    IsBtcLoansLoading = false;
                });
                return;
            }

            var fiatCurrency = FiatCurrency.GetFromCode(mainCurrency);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var rows = new ObservableCollection<RowItem>
                {
                    new(language.Reports_BtcLoans_ActiveLoans, dto.ActiveLoansCount.ToString(CultureInfo.InvariantCulture)),
                    new(language.Reports_BtcLoans_TotalDebt, CurrencyDisplay.FormatFiat(dto.TotalDebtInMainCurrency, fiatCurrency.Code)),
                    new(language.Reports_BtcLoans_TotalDebtBtc, CurrencyDisplay.FormatAsBitcoin(dto.TotalDebtInBtc) + " BTC"),
                    new(language.Reports_BtcLoans_AvgLtv, dto.DebtWeightedAvgLtv.ToString("0.##", CultureInfo.InvariantCulture) + "%",
                        TooltipContent.Text(language.Reports_BtcLoans_AvgLtv_Tooltip)),
                    new(language.Reports_BtcLoans_AvgApr, dto.DebtWeightedAvgApr.ToString("0.##", CultureInfo.InvariantCulture) + "%",
                        TooltipContent.Text(language.Reports_BtcLoans_AvgApr_Tooltip)),
                    RowItem.Separator(),
                    new(language.Reports_BtcLoans_CollateralFiat, CurrencyDisplay.FormatFiat(dto.TotalCollateralFiatInMainCurrency, fiatCurrency.Code)),
                    new(language.Reports_BtcLoans_CollateralSats, CurrencyDisplay.FormatSatsAsBitcoin(dto.TotalCollateralSats) + " BTC"),
                    new(language.Reports_BtcLoans_CollateralPercent,
                        dto.CollateralPercentOfStack.ToString("0.##", CultureInfo.InvariantCulture) + "%",
                        TooltipContent.Text(language.Reports_BtcLoans_CollateralPercent_Tooltip)),
                    new(language.Reports_BtcLoans_FreeBtc, CurrencyDisplay.FormatSatsAsBitcoin(dto.FreeBtcSats) + " BTC"),
                    RowItem.Separator(),
                    new(language.Reports_BtcLoans_HealthBreakdown,
                        string.Format(CultureInfo.InvariantCulture, language.Reports_BtcLoans_HealthBreakdown_Format,
                            dto.HealthyCount, dto.WarningCount, dto.DangerCount)),
                    new(language.Reports_BtcLoans_HighestLtv, dto.HighestLtv.ToString("0.##", CultureInfo.InvariantCulture) + "%"),
                    new(language.Reports_BtcLoans_ClosestDistance,
                        dto.ClosestDistanceToLiquidationLtv.ToString("0.##", CultureInfo.InvariantCulture) + "%",
                        TooltipContent.Text(string.Format(CultureInfo.InvariantCulture, language.Reports_BtcLoans_ClosestDistance_Tooltip, dto.ClosestLoanName))),
                    new(language.Reports_BtcLoans_WorstCaseLiquidationPrice,
                        CurrencyDisplay.FormatFiat(dto.WorstCaseLiquidationBtcPriceUsd, FiatCurrency.Usd.Code),
                        TooltipContent.Text(language.Reports_BtcLoans_WorstCaseLiquidationPrice_Tooltip)),
                    RowItem.Separator(),
                    new(language.Reports_BtcLoans_AccruedInterest, CurrencyDisplay.FormatFiat(dto.TotalAccruedInterestInMainCurrency, fiatCurrency.Code)),
                    new(language.Reports_BtcLoans_FeesPaid, CurrencyDisplay.FormatFiat(dto.TotalFeesPaidInMainCurrency, fiatCurrency.Code)),
                    RowItem.Separator(),
                    new(language.Reports_BtcLoans_AvgLoanAge,
                        string.Format(CultureInfo.InvariantCulture, language.Reports_BtcLoans_DaysFormat, (int)Math.Round(dto.AverageLoanAgeDays)))
                };

                if (dto.NextRepaymentDate.HasValue)
                {
                    var daysText = string.Format(CultureInfo.InvariantCulture, language.Reports_BtcLoans_DaysFormat, dto.DaysUntilNextRepayment ?? 0);
                    rows.Add(new RowItem(language.Reports_BtcLoans_NextRepayment,
                        $"{dto.NextRepaymentDate.Value} ({daysText})",
                        TooltipContent.Text(string.Format(CultureInfo.InvariantCulture, language.Reports_BtcLoans_NextRepayment_Tooltip, dto.NextRepaymentLoanName))));
                }

                BtcLoansData = new DashboardData(language.Reports_BtcLoans_Title, rows, Icon: "\uE227");
                IsBtcLoansVisible = true;
                IsBtcLoansLoading = false;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating BTC loans data");
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsBtcLoansVisible = false;
                IsBtcLoansLoading = false;
            });
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

    private async Task FetchIncomeByCategoryAsync(IReportDataProvider provider)
    {
        try
        {
            var filter = new IIncomeByCategoryReport.Filter(
                SelectedAccounts.Select(x => new AccountId(x.Id.ToString())).ToList(),
                SelectedCategories.Select(x => new CategoryId(x.Id.ToString())).ToList());

            var incomeByCategoryData = await _incomeByCategoryReport.GetAsync(
                DateOnly.FromDateTime(CategoryFilterMainDate),
                new DateOnlyRange(DateOnly.FromDateTime(CategoryFilterRange.Start),
                    DateOnly.FromDateTime(CategoryFilterRange.End)),
                FiatCurrency.GetFromCode(_currencySettings.MainFiatCurrency),
                filter,
                provider);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IncomeByCategoryChartData.RefreshChart(incomeByCategoryData);
                IsIncomeByCategoriesLoading = false;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching income by category");
            await Dispatcher.UIThread.InvokeAsync(() => { IsIncomeByCategoriesLoading = false; });
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

    private async Task FetchWealthOverviewAsync(IReportDataProvider provider)
    {
        try
        {
            var wealthOverviewData = await _wealthOverviewReport.GetAsync(
                SelectedWealthOverviewPeriod,
                FiatCurrency.GetFromCode(_currencySettings.MainFiatCurrency),
                provider,
                SelectedWealthOverviewMaxElements);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                WealthOverviewChartData.RefreshChart(wealthOverviewData);
                IsWealthOverviewLoading = false;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching wealth overview");
            await Dispatcher.UIThread.InvokeAsync(() => { IsWealthOverviewLoading = false; });
        }
    }

    partial void OnSelectedWealthOverviewPeriodChanged(WealthOverviewPeriod value)
    {
        if (!_ready) return;

        IsWealthOverviewLoading = true;
        FetchWealthOverviewWithProviderAsync().FireAndForgetSafeAsync(_runner, _logger);
    }

    partial void OnSelectedWealthOverviewMaxElementsChanged(int value)
    {
        if (!_ready) return;

        IsWealthOverviewLoading = true;
        FetchWealthOverviewWithProviderAsync().FireAndForgetSafeAsync(_runner, _logger);
    }

    private async Task FetchWealthOverviewWithProviderAsync()
    {
        var provider = await GetOrCreateProviderAsync();
        await FetchWealthOverviewAsync(provider);
    }

    public override MainViewTabNames TabName => MainViewTabNames.ReportsPageContent;

    public override Task RefreshAsync() => ReloadDataAndFetchAllReportsAsync();

    public record SelectItem(string Id, string Name);

    #region Event Handlers

    private void OnSecureModeStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(IsSecureModeEnabled));
    }

    private void OnAccountsTotalStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AccountsTotalState.CurrentWealth))
        {
            Dispatcher.UIThread.Post(_wealthPanel.Refresh);
            Dispatcher.UIThread.Post(_btcStackPanel.Refresh);
            Dispatcher.UIThread.Post(_simulatedPricesPanel.Refresh);
            UpdateLeveragePositionsDataAsync().FireAndForgetSafeAsync(_runner, _logger);
            UpdateBtcLoansDataAsync().FireAndForgetSafeAsync(_runner, _logger);
        }
    }

    private void OnRatesStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(RatesState.BitcoinPrice) or nameof(RatesState.FiatRates))
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (!IsCustomPriceActive)
                {
                    UpdateCurrentBtcPriceFormatted();
                    _simulatedPricesPanel.Refresh();
                }
            });
        }
    }

    private void OnWealthPanelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DashboardPanelViewModel.Data))
            WealthData = _wealthPanel.Data;
        else if (e.PropertyName == nameof(DashboardPanelViewModel.IsLoading))
            IsWealthLoading = _wealthPanel.IsLoading;
    }

    private void OnBtcStackPanelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DashboardPanelViewModel.Data))
            BtcStackData = _btcStackPanel.Data;
        else if (e.PropertyName == nameof(DashboardPanelViewModel.IsLoading))
            IsBtcStackLoading = _btcStackPanel.IsLoading;
    }

    private void OnSimulatedPricesPanelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DashboardPanelViewModel.Data))
            SimulatedPricesData = _simulatedPricesPanel.Data with { ConfigureCommand = OpenSimulatedPricesConfigCommand };
        else if (e.PropertyName == nameof(DashboardPanelViewModel.IsLoading))
            IsSimulatedPricesLoading = _simulatedPricesPanel.IsLoading;
    }

    private void OnIndicatorsPanelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DashboardPanelViewModel.Data))
            IndicatorsData = _indicatorsPanel.Data;
        else if (e.PropertyName == nameof(DashboardPanelViewModel.IsLoading))
            IsIndicatorsLoading = _indicatorsPanel.IsLoading;
    }

    #endregion

    private void UpdateIndicatorsData(IndicatorSnapshot snapshot)
    {
        _indicatorsPanel.UpdateIndicatorsData(snapshot);
    }

    public void Dispose()
    {
        _filterDebounceTokenSource?.Cancel();
        _filterDebounceTokenSource?.Dispose();

        SelectedAccounts.CollectionChanged -= OnSelectedFiltersChanged;
        SelectedCategories.CollectionChanged -= OnSelectedFiltersChanged;

        // Unsubscribe from PropertyChanged events
        _secureModeState.PropertyChanged -= OnSecureModeStatePropertyChanged;
        _accountsTotalState.PropertyChanged -= OnAccountsTotalStatePropertyChanged;
        _ratesState.PropertyChanged -= OnRatesStatePropertyChanged;
        _wealthPanel.PropertyChanged -= OnWealthPanelPropertyChanged;
        _btcStackPanel.PropertyChanged -= OnBtcStackPanelPropertyChanged;
        _simulatedPricesPanel.PropertyChanged -= OnSimulatedPricesPanelPropertyChanged;
        _indicatorsPanel.PropertyChanged -= OnIndicatorsPanelPropertyChanged;

        WeakReferenceMessenger.Default.Unregister<SettingsChangedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<AssetSummaryUpdatedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<IndicatorsUpdatedMessage>(this);

        MonthlyTotalsChartData.Dispose();
        ExpensesByCategoryChartData.Dispose();
        IncomeByCategoryChartData.Dispose();
        WealthOverviewChartData.Dispose();

        // Clear the provider on dispose
        _cachedProvider = null;
    }
}
