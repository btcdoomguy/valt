using System.Reflection;
using CommunityToolkit.Mvvm.Messaging;
using LiteDB;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.SpendingAnalytics.DTOs;
using Valt.App.Modules.SpendingAnalytics.Queries;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Core.Kernel.Factories;
using Valt.Infra.Crawlers.Indicators;
using Valt.Infra.Kernel;
using Valt.Infra.DataAccess;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.Budget.Categories;
using Valt.Infra.Modules.Budget.Transactions;
using Valt.App.Kernel.Notifications;
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
        _queryDispatcher.DispatchAsync(Arg.Any<GetSavingsRateQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<SavingsRateDataDto>(new InvalidOperationException("Savings rate fetch triggered")));

        _queryDispatcher.DispatchAsync(Arg.Any<GetFixedVsVariableQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<FixedVsVariableDataDto>(new InvalidOperationException("Fixed vs variable fetch triggered")));
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
    }

    private void ConfigureDefaultClockBehavior()
    {
        var now = new DateTime(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        _clock.GetCurrentDateTimeUtc().Returns(now);
        _clock.GetCurrentLocalDate().Returns(new DateOnly(2025, 1, 15));
    }

    private ReportsViewModel CreateViewModel()
    {
        return new ReportsViewModel(
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

    [Test]
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
    public void Constructor_Should_Initialize_SavingsRate_And_FixedVsVariable_Loading_Flags()
    {
        // Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.That(viewModel.IsSavingsRateLoading, Is.True, "Savings rate loading flag should start true");
        Assert.That(viewModel.IsFixedVsVariableLoading, Is.True, "Fixed vs variable loading flag should start true");
    }

    [Test]
    public void Constructor_Should_Expose_SavingsRate_Chart_Data()
    {
        // Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.That(viewModel.SavingsRateChartData, Is.Not.Null);
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
}
