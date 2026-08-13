using System.Reflection;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using LiteDB;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.Assets.DTOs;
using Valt.App.Modules.Assets.Queries.GetBtcLoansDashboard;
using Valt.App.Modules.BtcDenominatedMetrics.DTOs;
using Valt.App.Modules.BtcDenominatedMetrics.Queries;
using Valt.App.Modules.LoanReports.DTOs;
using Valt.App.Modules.LoanReports.Queries;
using Valt.App.Modules.SpendingAnalytics.DTOs;
using Valt.App.Modules.SpendingAnalytics.Queries;
using Valt.Core.Common;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Core.Kernel.Factories;
using Valt.Infra.Crawlers.Indicators;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel;
using Valt.App.Kernel.Notifications;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.Budget.Categories;
using Valt.Infra.Modules.Budget.Transactions;
using Valt.Infra.Modules.Configuration;
using Valt.Infra.Modules.Reports;
using Valt.Infra.Modules.Reports.AllTimeHigh;
using Valt.Infra.Modules.Reports.ExpensesByCategory;
using Valt.Infra.Modules.Reports.IncomeByCategory;
using Valt.Infra.Modules.Reports.MaxBtcStack;
using Valt.Infra.Modules.Reports.MonthlyTotals;
using Valt.Infra.Modules.Reports.Statistics;
using Valt.Infra.Modules.Reports.WealthOverview;
using Valt.Infra.Settings;
using Valt.UI.Services;
using Valt.UI.Base;
using Valt.UI.State;
using Valt.UI.UserControls;
using Valt.UI.Views.Main.Tabs.Reports;
using Valt.UI.Views.Main.Tabs.Reports.Panels;
using Valt.UI.Views.Main.Tabs.Transactions.Models;

namespace Valt.Tests.UI.Screens;

[TestFixture]
public class ReportsViewModelTests
{
    private IFireAndForgetTaskRunner _runner = null!;
    private IndicatorsPanelViewModel _indicatorsPanel = null!;
    private IIndicatorCache _indicatorCache = null!;
    private ILogger<IndicatorsPanelViewModel> _indicatorsLogger = null!;
    private ILocalDatabase _localDatabase = null!;
    private INotificationPublisher _notificationPublisher = null!;
    private CurrencySettings _currencySettings = null!;
    private RatesState _ratesState = null!;
    private CustomBtcPriceState _customBtcPriceState = null!;
    private AccountsTotalState _accountsTotalState = null!;
    private IQueryDispatcher _queryDispatcher = null!;
    private ILogger<AccountsTotalState> _accountsTotalLogger = null!;
    private SecureModeState _secureModeState = null!;
    private IConfigurationManager _configurationManager = null!;
    private IModalFactory _modalFactory = null!;
    private WealthPanelViewModel _wealthPanel = null!;
    private BtcStackPanelViewModel _btcStackPanel = null!;
    private SimulatedPricesPanelViewModel _simulatedPricesPanel = null!;
    private ILogger<ReportsViewModel> _logger = null!;
    private ILeveragePositionsPanelViewModel _leveragePanel = null!;
    private IBtcLoansPanelViewModel _btcLoansPanel = null!;
    private BurnRatePanelViewModel _burnRatePanel = null!;
    private IAllTimeHighReport _allTimeHighReport = null!;
    private IMaxBtcStackReport _maxBtcStackReport = null!;
    private IMonthlyTotalsReport _monthlyTotalsReport = null!;
    private IExpensesByCategoryReport _expensesByCategoryReport = null!;
    private IIncomeByCategoryReport _incomeByCategoryReport = null!;
    private IStatisticsReport _statisticsReport = null!;
    private IWealthOverviewReport _wealthOverviewReport = null!;
    private IReportDataProviderFactory _reportDataProviderFactory = null!;
    private IClock _clock = null!;
    private ILogger<WealthPanelViewModel> _wealthLogger = null!;
    private ILogger<BtcStackPanelViewModel> _btcStackLogger = null!;
    private ILogger<SimulatedPricesPanelViewModel> _simulatedPricesLogger = null!;
    private ILogger<BurnRatePanelViewModel> _burnRateLogger = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        IdGenerator.Configure(new LiteDbIdProvider());
    }

    [SetUp]
    public void SetUp()
    {
        TransactionGridResources.InitializeForTesting();

        WeakReferenceMessenger.Default.Reset();

        _runner = Substitute.For<IFireAndForgetTaskRunner>();
        _indicatorCache = Substitute.For<IIndicatorCache>();
        _indicatorsLogger = Substitute.For<ILogger<IndicatorsPanelViewModel>>();
        _indicatorsPanel = Substitute.For<IndicatorsPanelViewModel>(_indicatorCache, _indicatorsLogger);

        _localDatabase = Substitute.For<ILocalDatabase>();
        _notificationPublisher = Substitute.For<INotificationPublisher>();
        _currencySettings = new CurrencySettings(_localDatabase, _notificationPublisher);
        _ratesState = new RatesState();
        _customBtcPriceState = new CustomBtcPriceState();
        _queryDispatcher = Substitute.For<IQueryDispatcher>();
        _accountsTotalLogger = Substitute.For<ILogger<AccountsTotalState>>();
        _accountsTotalState = new AccountsTotalState(
            _currencySettings, _ratesState, _customBtcPriceState, _queryDispatcher, _accountsTotalLogger);
        _secureModeState = new SecureModeState();
        _configurationManager = Substitute.For<IConfigurationManager>();
        _modalFactory = Substitute.For<IModalFactory>();

        _wealthLogger = Substitute.For<ILogger<WealthPanelViewModel>>();
        _wealthPanel = Substitute.For<WealthPanelViewModel>(
            _accountsTotalState, _customBtcPriceState, _ratesState, _currencySettings, _wealthLogger);

        _btcStackLogger = Substitute.For<ILogger<BtcStackPanelViewModel>>();
        _maxBtcStackReport = Substitute.For<IMaxBtcStackReport>();
        _clock = Substitute.For<IClock>();
        _btcStackPanel = Substitute.For<BtcStackPanelViewModel>(
            _accountsTotalState, _maxBtcStackReport, _clock, _btcStackLogger);

        _simulatedPricesLogger = Substitute.For<ILogger<SimulatedPricesPanelViewModel>>();
        _simulatedPricesPanel = Substitute.For<SimulatedPricesPanelViewModel>(
            _configurationManager, _accountsTotalState, _ratesState, _currencySettings, _simulatedPricesLogger);

        _leveragePanel = Substitute.For<ILeveragePositionsPanelViewModel>();
        _btcLoansPanel = Substitute.For<IBtcLoansPanelViewModel>();
        _burnRateLogger = Substitute.For<ILogger<BurnRatePanelViewModel>>();
        _burnRatePanel = new BurnRatePanelViewModel(
            _queryDispatcher, _accountsTotalState, _currencySettings, _burnRateLogger);

        _logger = Substitute.For<ILogger<ReportsViewModel>>();
        _allTimeHighReport = Substitute.For<IAllTimeHighReport>();
        _monthlyTotalsReport = Substitute.For<IMonthlyTotalsReport>();
        _expensesByCategoryReport = Substitute.For<IExpensesByCategoryReport>();
        _incomeByCategoryReport = Substitute.For<IIncomeByCategoryReport>();
        _statisticsReport = Substitute.For<IStatisticsReport>();
        _wealthOverviewReport = Substitute.For<IWealthOverviewReport>();
        _reportDataProviderFactory = Substitute.For<IReportDataProviderFactory>();

        ConfigureDefaultLocalDatabaseBehavior();
        ConfigureDefaultConfigurationManagerBehavior();
        ConfigureDefaultClockBehavior();
        ConfigureDefaultQueryDispatcherBehavior();
    }

    private void ConfigureDefaultQueryDispatcherBehavior()
    {
        _queryDispatcher.DispatchAsync(Arg.Any<GetFixedVsVariableQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<FixedVsVariableDataDto>(new InvalidOperationException("Fixed vs variable fetch triggered")));

        _queryDispatcher.DispatchAsync(Arg.Any<GetLoanReportsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new LoanReportsDataDto
            {
                CostMonths = new List<LoanCostMonthDto>(),
                DistanceMonths = new List<LiquidationDistanceMonthDto>(),
                HasActiveLoans = false,
                PrimaryCurrency = "USD"
            }));
    }

    [TearDown]
    public void TearDown()
    {
        WeakReferenceMessenger.Default.Reset();
        _localDatabase?.Dispose();
        _ratesState?.Dispose();
        _accountsTotalState?.Dispose();
    }

    private void ConfigureDefaultLocalDatabaseBehavior()
    {
        var accounts = Substitute.For<ILiteCollection<AccountEntity>>();
        accounts.FindAll().Returns(new List<AccountEntity>());
        _localDatabase.GetAccounts().Returns(accounts);

        var categories = Substitute.For<ILiteCollection<CategoryEntity>>();
        categories.FindAll().Returns(new List<CategoryEntity>());
        _localDatabase.GetCategories().Returns(categories);

        var transactions = Substitute.For<ILiteCollection<TransactionEntity>>();
        transactions.FindAll().Returns(new List<TransactionEntity>());
        _localDatabase.GetTransactions().Returns(transactions);
    }

    private void ConfigureDefaultConfigurationManagerBehavior()
    {
        _configurationManager.GetExpensesCategoryFilterExcludedIds()
            .Returns(new List<string>());
        _configurationManager.GetStatisticsExcludedCategoryIds()
            .Returns(new List<string>());
        _configurationManager.GetReportsAnalyticsCategoryFilterExcludedIds()
            .Returns(new List<string>());
    }

    private void ConfigureDefaultClockBehavior()
    {
        var now = new DateTime(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        _clock.GetCurrentDateTimeUtc().Returns(now);
        _clock.GetCurrentLocalDate().Returns(new DateOnly(2025, 1, 15));
    }

    private ReportsViewModel CreateViewModel()
    {
        return new TestableReportsViewModel(
            _allTimeHighReport,
            _maxBtcStackReport,
            _monthlyTotalsReport,
            _expensesByCategoryReport,
            _incomeByCategoryReport,
            _statisticsReport,
            _wealthOverviewReport,
            _reportDataProviderFactory,
            _currencySettings,
            _localDatabase,
            _clock,
            _logger,
            _accountsTotalState,
            _ratesState,
            _customBtcPriceState,
            _secureModeState,
            _configurationManager,
            _modalFactory,
            _queryDispatcher,
            _indicatorCache,
            _runner,
            _indicatorsPanel,
            _wealthPanel,
            _btcStackPanel,
            _simulatedPricesPanel,
            _leveragePanel,
            _btcLoansPanel,
            _burnRatePanel);
    }

    private class TestableReportsViewModel : ReportsViewModel
    {
        public TestableReportsViewModel(
            IAllTimeHighReport allTimeHighReport,
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
            SimulatedPricesPanelViewModel simulatedPricesPanel,
            ILeveragePositionsPanelViewModel leveragePanel,
            IBtcLoansPanelViewModel btcLoansPanel,
            BurnRatePanelViewModel burnRatePanel)
            : base(allTimeHighReport, maxBtcStackReport, monthlyTotalsReport, expensesByCategoryReport,
                incomeByCategoryReport, statisticsReport, wealthOverviewReport, reportDataProviderFactory,
                currencySettings, localDatabase, clock, logger, accountsTotalState, ratesState,
                customBtcPriceState, secureModeState, configurationManager, modalFactory, queryDispatcher,
                indicatorCache, runner, indicatorsPanel, wealthPanel, btcStackPanel, simulatedPricesPanel,
                leveragePanel, btcLoansPanel, burnRatePanel)
        {
        }

        protected override Task RunOnUiThread(Action action)
        {
            action();
            return Task.CompletedTask;
        }
    }

    [Test]
    public void Initialize_Should_Run_IndicatorsRefresh_Through_Runner()
    {
        // Arrange
        var tcs = new TaskCompletionSource();
        tcs.SetResult();
        var expectedTask = tcs.Task;
        _indicatorsPanel.RefreshAsync().Returns(expectedTask);

        var viewModel = CreateViewModel();

        // Act
        viewModel.Initialize();

        // Assert
        _runner.Received(1).RunAsync(expectedTask, _logger, "Initialize");
    }

    [Test]
    public async Task Initialize_Should_Refresh_LeveragePanel()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.Initialize();
        await Task.Delay(100);

        // Assert
        _ = _leveragePanel.Received(1).RefreshAsync();
    }

    [Test, CancelAfter(10000)]
    public async Task Initialize_Should_Refresh_BtcLoansPanel()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.Initialize();
        await Task.Delay(100);

        // Assert
        _ = _btcLoansPanel.Received(1).RefreshAsync();
    }

    [Test]
    public void Panel_IsVisible_Changes_Are_Forwarded_To_ReportsViewModel()
    {
        // Arrange
        var leverageLogger = Substitute.For<ILogger<LeveragePositionsPanelViewModel>>();
        var btcLoansLogger = Substitute.For<ILogger<BtcLoansPanelViewModel>>();
        var burnRateLogger = Substitute.For<ILogger<BurnRatePanelViewModel>>();

        var realLeveragePanel = new LeveragePositionsPanelViewModel(
            _queryDispatcher, _accountsTotalState, _ratesState, _customBtcPriceState, _currencySettings, leverageLogger);
        var realBtcLoansPanel = new BtcLoansPanelViewModel(
            _queryDispatcher, _accountsTotalState, _ratesState, _customBtcPriceState, _currencySettings, btcLoansLogger);
        var realBurnRatePanel = new BurnRatePanelViewModel(
            _queryDispatcher, _accountsTotalState, _currencySettings, burnRateLogger);

        var viewModel = new ReportsViewModel(
            _allTimeHighReport,
            _maxBtcStackReport,
            _monthlyTotalsReport,
            _expensesByCategoryReport,
            _incomeByCategoryReport,
            _statisticsReport,
            _wealthOverviewReport,
            _reportDataProviderFactory,
            _currencySettings,
            _localDatabase,
            _clock,
            _logger,
            _accountsTotalState,
            _ratesState,
            _customBtcPriceState,
            _secureModeState,
            _configurationManager,
            _modalFactory,
            _queryDispatcher,
            _indicatorCache,
            _runner,
            _indicatorsPanel,
            _wealthPanel,
            _btcStackPanel,
            _simulatedPricesPanel,
            realLeveragePanel,
            realBtcLoansPanel,
            realBurnRatePanel);

        // Act
        var leverageEventCount = 0;
        realLeveragePanel.PropertyChanged += (s, e) =>
        {
            leverageEventCount++;
        };

        Assert.That(viewModel.IsLeveragePositionsVisible, Is.False, "Initial leverage visibility should be false");
        Assert.That(realLeveragePanel.IsVisible, Is.False, "Panel initial IsVisible should be false");

        realLeveragePanel.IsVisible = true;
        Assert.That(viewModel.IsLeveragePositionsVisible, Is.True, "After panel shown");

        realLeveragePanel.IsVisible = false;
        Assert.That(viewModel.IsLeveragePositionsVisible, Is.False, "After panel hidden");

        realBtcLoansPanel.IsVisible = true;
        Assert.That(viewModel.IsBtcLoansVisible, Is.True, "After BTC loans panel shown");

        realBtcLoansPanel.IsVisible = false;
        Assert.That(viewModel.IsBtcLoansVisible, Is.False, "After BTC loans panel hidden");

        realBurnRatePanel.IsVisible = true;
        Assert.That(viewModel.IsBurnRateVisible, Is.True, "After burn rate panel shown");

        realBurnRatePanel.IsVisible = false;
        Assert.That(viewModel.IsBurnRateVisible, Is.False, "After burn rate panel hidden");

        // Assert
        Assert.That(leverageEventCount, Is.GreaterThan(0), "Leverage panel should raise PropertyChanged");
    }

    [Test]
    public void Panel_Initial_Visibility_Is_Forwarded_To_ReportsViewModel()
    {
        // Arrange
        var leverageLogger = Substitute.For<ILogger<LeveragePositionsPanelViewModel>>();
        var btcLoansLogger = Substitute.For<ILogger<BtcLoansPanelViewModel>>();
        var burnRateLogger = Substitute.For<ILogger<BurnRatePanelViewModel>>();

        var realLeveragePanel = new LeveragePositionsPanelViewModel(
            _queryDispatcher, _accountsTotalState, _ratesState, _customBtcPriceState, _currencySettings, leverageLogger);
        var realBtcLoansPanel = new BtcLoansPanelViewModel(
            _queryDispatcher, _accountsTotalState, _ratesState, _customBtcPriceState, _currencySettings, btcLoansLogger);
        var realBurnRatePanel = new BurnRatePanelViewModel(
            _queryDispatcher, _accountsTotalState, _currencySettings, burnRateLogger);

        // Act
        var viewModel = new ReportsViewModel(
            _allTimeHighReport,
            _maxBtcStackReport,
            _monthlyTotalsReport,
            _expensesByCategoryReport,
            _incomeByCategoryReport,
            _statisticsReport,
            _wealthOverviewReport,
            _reportDataProviderFactory,
            _currencySettings,
            _localDatabase,
            _clock,
            _logger,
            _accountsTotalState,
            _ratesState,
            _customBtcPriceState,
            _secureModeState,
            _configurationManager,
            _modalFactory,
            _queryDispatcher,
            _indicatorCache,
            _runner,
            _indicatorsPanel,
            _wealthPanel,
            _btcStackPanel,
            _simulatedPricesPanel,
            realLeveragePanel,
            realBtcLoansPanel,
            realBurnRatePanel);

        // Assert
        Assert.That(viewModel.IsLeveragePositionsVisible, Is.EqualTo(realLeveragePanel.IsVisible),
            "ReportsViewModel should reflect the panel's initial visibility");
        Assert.That(viewModel.IsBtcLoansVisible, Is.EqualTo(realBtcLoansPanel.IsVisible),
            "ReportsViewModel should reflect the panel's initial visibility");
        Assert.That(viewModel.IsBurnRateVisible, Is.EqualTo(realBurnRatePanel.IsVisible),
            "ReportsViewModel should reflect the panel's initial visibility");
    }

    [Test]
    public void Constructor_Should_Expose_FixedVsVariable_Chart_Data()
    {
        // Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.That(viewModel.FixedVsVariableChartData, Is.Not.Null);
    }

    [Test]
    public void HasNoFixedExpenses_Flag_Exposes_Hint_Box_Path()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var propertyChanged = false;
        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ReportsViewModel.HasNoFixedExpenses))
                propertyChanged = true;
        };

        // Act
        viewModel.HasNoFixedExpenses = true;

        // Assert
        Assert.That(viewModel.HasNoFixedExpenses, Is.True);
        Assert.That(propertyChanged, Is.True, "PropertyChanged should be raised for HasNoFixedExpenses");
    }

    [Test]
    public void OpenReportsCategoryFilterConfigCommand_Exists_After_Construction()
    {
        // Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.That(viewModel.OpenReportsCategoryFilterConfigCommand, Is.Not.Null,
            "OpenReportsCategoryFilterConfigCommand should be created by the constructor");
    }

    [Test]
    public void GetSelectedAnalyticsCategoryIds_Returns_Available_Minus_Excluded()
    {
        // Arrange
        var excludedId = "507f1f77bcf86cd799439012";
        _configurationManager.GetReportsAnalyticsCategoryFilterExcludedIds()
            .Returns(new List<string> { excludedId });

        var categories = Substitute.For<ILiteCollection<CategoryEntity>>();
        categories.FindAll().Returns(new List<CategoryEntity>
        {
            new() { Id = new ObjectId("507f1f77bcf86cd799439011"), Name = "Category 1" },
            new() { Id = new ObjectId(excludedId), Name = "Category 2" },
            new() { Id = new ObjectId("507f1f77bcf86cd799439013"), Name = "Category 3" }
        });
        _localDatabase.GetCategories().Returns(categories);

        var viewModel = CreateViewModel();

        // Act - use reflection to access private helper
        var method = typeof(ReportsViewModel).GetMethod(
            "GetSelectedAnalyticsCategoryIds",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var result = (string[])method!.Invoke(viewModel, null)!;

        // Assert
        Assert.That(result, Is.EquivalentTo(new[] { "507f1f77bcf86cd799439011", "507f1f77bcf86cd799439013" }),
            "Selected analytics category IDs should exclude the configured excluded ID");
    }

    [Test]
    public void BurnRatePanelViewModel_SetCategoryFilter_Stores_Ids_And_RefreshAsync_Passes_Them_To_Query()
    {
        // Arrange
        var categoryIds = new[] { "cat-1", "cat-3" };
        GetBurnRateQuery? capturedQuery = null;
        _queryDispatcher.DispatchAsync(Arg.Do<GetBurnRateQuery>(q => capturedQuery = q), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new BurnRateDataDto
            {
                HasData = false,
                SpentSoFar = 0m,
                AvgDailySpend = 0m,
                MedianMonthlyExpenses = 0m,
                DayOfMonth = 15,
                PrimaryCurrency = "USD"
            }));

        _burnRatePanel.SetCategoryFilter(categoryIds);

        // Act
        _burnRatePanel.RefreshAsync();

        // Assert
        Assert.That(capturedQuery, Is.Not.Null, "RefreshAsync should dispatch GetBurnRateQuery");
        Assert.That(capturedQuery!.CategoryIds, Is.EquivalentTo(categoryIds),
            "GetBurnRateQuery.CategoryIds should contain the IDs set by SetCategoryFilter");
    }

    [Test]
    public void StatisticsData_Does_Not_Expose_Configuration_Command()
    {
        // Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.That(viewModel.StatisticsData.HasConfiguration, Is.False,
            "Statistics dashboard should not expose a configuration command");
    }

    private static void SetFilterRange(ReportsViewModel viewModel, DateRange range)
    {
        typeof(ReportsViewModel)
            .GetField("_filterRange", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(viewModel, range);
    }

    [Test, CancelAfter(10000)]
    public async Task FetchLoanReportsAsync_Dispatches_GetLoanReportsQuery()
    {
        // Arrange
        var viewModel = CreateViewModel();
        SetFilterRange(viewModel, new DateRange(new DateTime(2025, 1, 1), new DateTime(2025, 1, 31)));

        var method = typeof(ReportsViewModel).GetMethod(
            "FetchLoanReportsAsync",
            BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        await (Task)method!.Invoke(viewModel, null)!;

        // Assert
        await _queryDispatcher.Received(1).DispatchAsync(
            Arg.Any<GetLoanReportsQuery>(),
            Arg.Any<CancellationToken>());
    }

    [Test, CancelAfter(10000)]
    public async Task FetchLoanReportsAsync_Sets_IsLoanReportsVisible_From_HasActiveLoans_True()
    {
        // Arrange
        var viewModel = CreateViewModel();
        SetFilterRange(viewModel, new DateRange(new DateTime(2025, 1, 1), new DateTime(2025, 1, 31)));

        _queryDispatcher.DispatchAsync(Arg.Any<GetLoanReportsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new LoanReportsDataDto
            {
                CostMonths = new List<LoanCostMonthDto>
                {
                    new() { Month = new DateOnly(2025, 1, 1), CombinedCost = 100m, Interest = 100m, Fees = 0m }
                },
                DistanceMonths = new List<LiquidationDistanceMonthDto>
                {
                    new() { Month = new DateOnly(2025, 1, 1), DistanceToLiquidation = 50m, ClosestLoanName = "Test" }
                },
                HasActiveLoans = true,
                PrimaryCurrency = "USD"
            }));

        var method = typeof(ReportsViewModel).GetMethod(
            "FetchLoanReportsAsync",
            BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        await (Task)method!.Invoke(viewModel, null)!;

        // Assert
        Assert.That(viewModel.IsLoanReportsVisible, Is.True);
        Assert.That(viewModel.IsLoanReportsLoading, Is.False);
        Assert.That(viewModel.IsLoanReportsError, Is.False);
    }

    [Test, CancelAfter(10000)]
    public async Task FetchLoanReportsAsync_Sets_IsLoanReportsVisible_From_HasActiveLoans_False()
    {
        // Arrange
        var viewModel = CreateViewModel();
        SetFilterRange(viewModel, new DateRange(new DateTime(2025, 1, 1), new DateTime(2025, 1, 31)));

        _queryDispatcher.DispatchAsync(Arg.Any<GetLoanReportsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new LoanReportsDataDto
            {
                CostMonths = new List<LoanCostMonthDto>(),
                DistanceMonths = new List<LiquidationDistanceMonthDto>(),
                HasActiveLoans = false,
                PrimaryCurrency = "USD"
            }));

        var method = typeof(ReportsViewModel).GetMethod(
            "FetchLoanReportsAsync",
            BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        await (Task)method!.Invoke(viewModel, null)!;

        // Assert
        Assert.That(viewModel.IsLoanReportsVisible, Is.False);
        Assert.That(viewModel.IsLoanReportsEmpty, Is.True);
        Assert.That(viewModel.IsLoanReportsLoading, Is.False);
    }

    [Test, CancelAfter(10000)]
    public async Task FetchLoanReportsAsync_Sets_IsLoanReportsEmpty_When_No_Months()
    {
        // Arrange
        var viewModel = CreateViewModel();
        SetFilterRange(viewModel, new DateRange(new DateTime(2025, 1, 1), new DateTime(2025, 1, 31)));

        _queryDispatcher.DispatchAsync(Arg.Any<GetLoanReportsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new LoanReportsDataDto
            {
                CostMonths = new List<LoanCostMonthDto>(),
                DistanceMonths = new List<LiquidationDistanceMonthDto>(),
                HasActiveLoans = true,
                PrimaryCurrency = "USD"
            }));

        var method = typeof(ReportsViewModel).GetMethod(
            "FetchLoanReportsAsync",
            BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        await (Task)method!.Invoke(viewModel, null)!;

        // Assert
        Assert.That(viewModel.IsLoanReportsEmpty, Is.True);
        Assert.That(viewModel.IsLoanReportsVisible, Is.True);
    }

    [Test, CancelAfter(10000)]
    public async Task FetchLoanReportsAsync_Resets_IsLoanReportsLoading_After_Completion()
    {
        // Arrange
        var viewModel = CreateViewModel();
        SetFilterRange(viewModel, new DateRange(new DateTime(2025, 1, 1), new DateTime(2025, 1, 31)));

        var method = typeof(ReportsViewModel).GetMethod(
            "FetchLoanReportsAsync",
            BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        await (Task)method!.Invoke(viewModel, null)!;

        // Assert
        Assert.That(viewModel.IsLoanReportsLoading, Is.False);
    }

    [Test, CancelAfter(10000)]
    public async Task FetchLoanReportsAsync_Sets_IsLoanReportsError_On_Exception()
    {
        // Arrange
        var viewModel = CreateViewModel();
        SetFilterRange(viewModel, new DateRange(new DateTime(2025, 1, 1), new DateTime(2025, 1, 31)));

        _queryDispatcher.DispatchAsync(Arg.Any<GetLoanReportsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<LoanReportsDataDto>(new InvalidOperationException("boom")));

        var method = typeof(ReportsViewModel).GetMethod(
            "FetchLoanReportsAsync",
            BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        await (Task)method!.Invoke(viewModel, null)!;

        // Assert
        Assert.That(viewModel.IsLoanReportsError, Is.True);
        Assert.That(viewModel.IsLoanReportsVisible, Is.False);
        Assert.That(viewModel.IsLoanReportsLoading, Is.False);
    }
}
