using Avalonia.Controls;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.Goals.Commands.CopyGoalsFromLastMonth;
using Valt.App.Modules.Goals.Commands.DeleteGoal;
using Valt.App.Modules.Goals.Commands.RecalculateGoal;
using Valt.App.Modules.Goals.DTOs;
using Valt.App.Modules.Goals.Queries.GetGoals;
using Valt.App.Modules.Goals.Queries.GetGoalContributingTransactions;
using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Goals;
using Valt.Infra.Kernel;
using Valt.Infra.Settings;
using Valt.Infra.Modules.Goals.Services;
using Valt.UI.Base;
using Valt.UI.Services;
using Valt.UI.State;
using Valt.UI.Views;
using Valt.UI.Views.Main.Modals.GoalSummary;
using Valt.UI.Views.Main.Tabs.Transactions;
using Valt.UI.Views.Main.Tabs.Transactions.Models;

namespace Valt.Tests.UI.Screens;

[TestFixture]
public class GoalsPanelViewModelTests : DatabaseTest
{
    private ICommandDispatcher _commandDispatcher = null!;
    private IQueryDispatcher _queryDispatcher = null!;
    private IModalFactory _modalFactory = null!;
    private GoalProgressState _goalProgressState = null!;
    private CurrencySettings _currencySettings = null!;
    private FilterState _filterState = null!;
    private ILogger<GoalsPanelViewModel> _logger = null!;
    private SecureModeState _secureModeState = null!;
    private List<GoalDTO> _goals = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        IdGenerator.Configure(new LiteDbIdProvider());
    }

    [SetUp]
    public new void SetUp()
    {
        // Reset messenger to avoid interference between tests
        WeakReferenceMessenger.Default.Reset();

        _commandDispatcher = Substitute.For<ICommandDispatcher>();
        _queryDispatcher = Substitute.For<IQueryDispatcher>();
        _modalFactory = Substitute.For<IModalFactory>();
        _goalProgressState = new GoalProgressState();
        _currencySettings = new CurrencySettings(_localDatabase, null!);
        _filterState = new FilterState { MainDate = new DateTime(2025, 1, 15) };
        _logger = Substitute.For<ILogger<GoalsPanelViewModel>>();
        _secureModeState = new SecureModeState();
        _goals = new List<GoalDTO>();

        // Default mock for query dispatcher
        _queryDispatcher.DispatchAsync(Arg.Any<GetGoalsQuery>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult<IReadOnlyList<GoalDTO>>(_goals.ToList()));
    }

    [TearDown]
    public void TearDown()
    {
        WeakReferenceMessenger.Default.Reset();
    }

    private GoalsPanelViewModel CreateViewModel()
    {
        var vm = new GoalsPanelViewModel(
            _commandDispatcher,
            _queryDispatcher,
            _modalFactory,
            _goalProgressState,
            _currencySettings,
            _filterState,
            _logger,
            _secureModeState);
        vm.GetUserControlOwnerWindow = () => null!;
        return vm;
    }

    private GoalDTO CreateStackBitcoinGoalDTO(string id, GoalPeriods period, DateOnly refDate, GoalStates state = GoalStates.Open, decimal progress = 0m)
    {
        return new GoalDTO
        {
            Id = id,
            RefDate = refDate,
            Period = (int)period,
            StartDate = null,
            Progress = progress,
            State = (int)state,
            IsUpToDate = true,
            LastUpdatedAt = DateTime.Now,
            GoalType = new StackBitcoinGoalTypeOutputDTO
            {
                TargetSats = 1_000_000,
                CalculatedSats = (long)(progress * 10000)
            }
        };
    }

    private GoalDTO CreateSpendingLimitGoalDTO(string id, GoalPeriods period, DateOnly refDate, GoalStates state = GoalStates.Open, decimal progress = 0m)
    {
        return new GoalDTO
        {
            Id = id,
            RefDate = refDate,
            Period = (int)period,
            StartDate = null,
            Progress = progress,
            State = (int)state,
            IsUpToDate = true,
            LastUpdatedAt = DateTime.Now,
            GoalType = new SpendingLimitGoalTypeOutputDTO
            {
                TargetAmount = 5000m,
                CalculatedSpending = progress * 50
            }
        };
    }

    private GoalDTO CreateDcaGoalDTO(string id, GoalPeriods period, DateOnly refDate, GoalStates state = GoalStates.Open, decimal progress = 0m)
    {
        return new GoalDTO
        {
            Id = id,
            RefDate = refDate,
            Period = (int)period,
            StartDate = null,
            Progress = progress,
            State = (int)state,
            IsUpToDate = true,
            LastUpdatedAt = DateTime.Now,
            GoalType = new DcaGoalTypeOutputDTO
            {
                TargetPurchaseCount = 12,
                CalculatedPurchaseCount = (int)(progress / 10)
            }
        };
    }

    private GoalDTO CreateNetWorthBtcGoalDTO(string id, GoalPeriods period, DateOnly refDate, GoalStates state = GoalStates.Open, decimal progress = 0m)
    {
        return new GoalDTO
        {
            Id = id,
            RefDate = refDate,
            Period = (int)period,
            StartDate = null,
            Progress = progress,
            State = (int)state,
            IsUpToDate = true,
            LastUpdatedAt = DateTime.Now,
            GoalType = new NetWorthBtcGoalTypeOutputDTO
            {
                TargetSats = 1_000_000,
                CalculatedSats = (long)(progress * 10000)
            }
        };
    }

    private GoalDTO CreateSavingsRateGoalDTO(string id, GoalPeriods period, DateOnly refDate, GoalStates state = GoalStates.Open, decimal progress = 0m)
    {
        return new GoalDTO
        {
            Id = id,
            RefDate = refDate,
            Period = (int)period,
            StartDate = null,
            Progress = progress,
            State = (int)state,
            IsUpToDate = true,
            LastUpdatedAt = DateTime.Now,
            GoalType = new SavingsRateGoalTypeOutputDTO
            {
                TargetPercentage = 50m,
                CalculatedPercentage = progress / 2
            }
        };
    }

    /// <summary>
    /// Builds a GoalDTO for any goal-type output DTO, so per-type visibility/unit
    /// tests stay declarative without one factory per DTO shape.
    /// </summary>
    private GoalDTO CreateGoalDTO(GoalTypeOutputDTO goalType, string id = "goal-1")
    {
        return new GoalDTO
        {
            Id = id,
            RefDate = new DateOnly(2025, 1, 1),
            Period = (int)GoalPeriods.Monthly,
            StartDate = null,
            Progress = 0m,
            State = (int)GoalStates.Open,
            IsUpToDate = true,
            LastUpdatedAt = DateTime.Now,
            GoalType = goalType
        };
    }

    #region Initialization Tests

    [Test]
    public async Task Should_Initialize_With_Empty_GoalEntries()
    {
        // Arrange & Act
        var vm = CreateViewModel();
        await Task.Delay(100); // Wait for async initialization

        // Assert
        Assert.That(vm.GoalEntries, Is.Empty);
    }

    [Test]
    public async Task Should_Fetch_Goals_On_Initialization()
    {
        // Arrange
        _goals.Add(CreateStackBitcoinGoalDTO("1", GoalPeriods.Monthly, new DateOnly(2025, 1, 1)));
        _goals.Add(CreateSpendingLimitGoalDTO("2", GoalPeriods.Monthly, new DateOnly(2025, 1, 1)));

        // Act
        var vm = CreateViewModel();
        await Task.Delay(200); // Wait for async initialization

        // Assert
        Assert.That(vm.GoalEntries, Has.Count.EqualTo(2));
    }

    [Test]
    public void Should_Return_Correct_Month_Description()
    {
        // Arrange
        _filterState.MainDate = new DateTime(2025, 3, 15);

        // Act
        var vm = CreateViewModel();

        // Assert
        Assert.That(vm.GoalsCurrentMonthDescription, Is.EqualTo("(03/25)"));
    }

    #endregion

    #region Goal Sorting Tests

    [Test]
    public async Task Should_Sort_Goals_By_State_And_Period()
    {
        // Arrange
        _goals.Add(CreateStackBitcoinGoalDTO("1", GoalPeriods.Monthly, new DateOnly(2025, 1, 1), GoalStates.Completed));
        _goals.Add(CreateSpendingLimitGoalDTO("2", GoalPeriods.Monthly, new DateOnly(2025, 1, 1), GoalStates.Open));
        _goals.Add(CreateDcaGoalDTO("3", GoalPeriods.Yearly, new DateOnly(2025, 1, 1), GoalStates.Open));

        // Act
        var vm = CreateViewModel();
        await Task.Delay(200);

        // Assert - Open monthly goals first, then open yearly, then completed
        Assert.That(vm.GoalEntries, Has.Count.EqualTo(3));
        Assert.That(vm.GoalEntries[0].State, Is.EqualTo(GoalStates.Open));
        Assert.That(vm.GoalEntries[0].Period, Is.EqualTo(GoalPeriods.Monthly));
        Assert.That(vm.GoalEntries[1].State, Is.EqualTo(GoalStates.Open));
        Assert.That(vm.GoalEntries[1].Period, Is.EqualTo(GoalPeriods.Yearly));
        Assert.That(vm.GoalEntries[2].State, Is.EqualTo(GoalStates.Completed));
    }

    #endregion

    #region Secure Mode Tests

    [Test]
    public void IsSecureModeEnabled_Should_Reflect_SecureModeState()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act & Assert
        Assert.That(vm.IsSecureModeEnabled, Is.False);

        _secureModeState.IsEnabled = true;
        Assert.That(vm.IsSecureModeEnabled, Is.True);
    }

    #endregion

    #region Message Communication Tests

    [Test]
    public async Task Should_Refresh_When_FilterDateRangeChanged_Message_Received()
    {
        // Arrange
        var callCount = 0;
        _queryDispatcher.DispatchAsync(Arg.Any<GetGoalsQuery>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                callCount++;
                return Task.FromResult<IReadOnlyList<GoalDTO>>(new List<GoalDTO>());
            });

        var vm = CreateViewModel();
        await Task.Delay(100);
        var initialCallCount = callCount;

        // Act
        WeakReferenceMessenger.Default.Send(new FilterDateRangeChanged());
        await Task.Delay(100);

        // Assert
        Assert.That(callCount, Is.GreaterThan(initialCallCount));
    }

    [Test]
    public async Task Should_Refresh_When_GoalListChanged_Message_Received()
    {
        // Arrange
        var callCount = 0;
        _queryDispatcher.DispatchAsync(Arg.Any<GetGoalsQuery>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                callCount++;
                return Task.FromResult<IReadOnlyList<GoalDTO>>(new List<GoalDTO>());
            });

        var vm = CreateViewModel();
        await Task.Delay(100);
        var initialCallCount = callCount;

        // Act
        WeakReferenceMessenger.Default.Send(new GoalListChanged());
        await Task.Delay(100);

        // Assert
        Assert.That(callCount, Is.GreaterThan(initialCallCount));
    }

    [Test]
    public async Task Should_Update_Progress_When_GoalProgressUpdated_Message_Received()
    {
        // Arrange
        var goalDto = CreateStackBitcoinGoalDTO("1", GoalPeriods.Monthly, new DateOnly(2025, 1, 1), GoalStates.Open, 25m);
        _goals.Add(goalDto);

        var vm = CreateViewModel();
        await Task.Delay(200);

        Assert.That(vm.GoalEntries, Has.Count.EqualTo(1));
        Assert.That(vm.GoalEntries[0].Progress, Is.EqualTo(25m));

        // Update the goal progress in the mock
        _goals[0] = CreateStackBitcoinGoalDTO("1", GoalPeriods.Monthly, new DateOnly(2025, 1, 1), GoalStates.Open, 50m);

        // Act
        WeakReferenceMessenger.Default.Send(new GoalProgressUpdated());
        await Task.Delay(100);

        // Assert - progress should be updated
        Assert.That(vm.GoalEntries[0].Progress, Is.EqualTo(50m));
    }

    #endregion

    #region Command Tests

    [Test]
    public async Task RecalculateGoalCommand_Should_Dispatch_Command_And_Mark_Stale()
    {
        // Arrange
        var goalDto = CreateStackBitcoinGoalDTO("goal-1", GoalPeriods.Monthly, new DateOnly(2025, 1, 1), GoalStates.Completed, 100m);
        _goals.Add(goalDto);

        _commandDispatcher.DispatchAsync(Arg.Any<RecalculateGoalCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<RecalculateGoalResult>.Success(new RecalculateGoalResult()));

        var vm = CreateViewModel();
        await Task.Delay(200);

        var entry = new GoalEntryViewModel(goalDto, _currencySettings.MainFiatCurrency);

        // Act
        await vm.RecalculateGoalCommand.ExecuteAsync(entry);

        // Assert
        await _commandDispatcher.Received(1).DispatchAsync(
            Arg.Is<RecalculateGoalCommand>(c => c.GoalId == "goal-1"),
            Arg.Any<CancellationToken>());
        Assert.That(_goalProgressState.HasStaleGoals, Is.True);
    }

    [Test]
    public async Task DeleteGoalCommand_Should_Not_Dispatch_Without_Entry()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        await vm.DeleteGoalCommand.ExecuteAsync(null);

        // Assert
        await _commandDispatcher.DidNotReceive().DispatchAsync(Arg.Any<DeleteGoalCommand>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Dispose Tests

    [Test]
    public void Dispose_Should_Unregister_Messages()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.Dispose();

        // Assert - sending messages should not cause exceptions
        WeakReferenceMessenger.Default.Send(new FilterDateRangeChanged());
        WeakReferenceMessenger.Default.Send(new GoalListChanged());
        WeakReferenceMessenger.Default.Send(new GoalProgressUpdated());
        Assert.Pass("No exception thrown after dispose");
    }

    #endregion

    #region CopyFromLastMonth Tests

    [Test]
    public async Task CopyFromLastMonth_Should_Dispatch_Command()
    {
        // Arrange
        _commandDispatcher.DispatchAsync(Arg.Any<CopyGoalsFromLastMonthCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<CopyGoalsFromLastMonthResult>.Success(new CopyGoalsFromLastMonthResult { CopiedCount = 1 }));

        _filterState.MainDate = new DateTime(2025, 1, 15);

        var vm = CreateViewModel();
        await Task.Delay(200);

        // Act
        await vm.CopyFromLastMonthCommand.ExecuteAsync(null);

        // Assert - the ViewModel passes the current date from _filterState.MainDate
        await _commandDispatcher.Received(1).DispatchAsync(
            Arg.Is<CopyGoalsFromLastMonthCommand>(c => c.CurrentDate == new DateOnly(2025, 1, 15)),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CopyFromLastMonth_Should_Mark_Progress_State_As_Stale_When_Goals_Copied()
    {
        // Arrange
        _commandDispatcher.DispatchAsync(Arg.Any<CopyGoalsFromLastMonthCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<CopyGoalsFromLastMonthResult>.Success(new CopyGoalsFromLastMonthResult { CopiedCount = 2 }));

        _filterState.MainDate = new DateTime(2025, 1, 15);

        var vm = CreateViewModel();
        await Task.Delay(200);

        // Act
        await vm.CopyFromLastMonthCommand.ExecuteAsync(null);

        // Assert
        Assert.That(_goalProgressState.HasStaleGoals, Is.True);
    }

    [Test]
    public async Task CopyFromLastMonth_Should_Not_Mark_Stale_When_No_Goals_Copied()
    {
        // Arrange
        _commandDispatcher.DispatchAsync(Arg.Any<CopyGoalsFromLastMonthCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<CopyGoalsFromLastMonthResult>.Success(new CopyGoalsFromLastMonthResult { CopiedCount = 0 }));

        _filterState.MainDate = new DateTime(2025, 1, 15);

        var vm = CreateViewModel();
        await Task.Delay(200);

        // Act
        await vm.CopyFromLastMonthCommand.ExecuteAsync(null);

        // Assert
        Assert.That(_goalProgressState.HasStaleGoals, Is.False);
    }

    #endregion

    #region ViewSummary Tests

    [Test]
    public async Task ViewSummary_NullEntry_DoesNothing()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        await vm.ViewSummaryCommand.ExecuteAsync(null);

        // Assert
        await _queryDispatcher.DidNotReceive().DispatchAsync(
            Arg.Any<GetGoalContributingTransactionsQuery>(),
            Arg.Any<CancellationToken>());
        _ = _modalFactory.DidNotReceive().CreateAsync(
            Arg.Is(ApplicationModalNames.GoalSummary),
            Arg.Any<Window?>(),
            Arg.Any<object>());
    }

    [Test]
    public async Task ViewSummary_QueryFailure_DoesNotOpenModal()
    {
        // Arrange
        _queryDispatcher.DispatchAsync(Arg.Any<GetGoalContributingTransactionsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<GoalContributingTransactionsResult>.Failure(
                new Error("GOAL_NOT_FOUND", "Goal not found"))));

        var vm = CreateViewModel();
        var entry = new GoalEntryViewModel(
            CreateStackBitcoinGoalDTO("goal-1", GoalPeriods.Monthly, new DateOnly(2025, 1, 1)),
            _currencySettings.MainFiatCurrency);

        // Act
        try
        {
            await vm.ViewSummaryCommand.ExecuteAsync(entry);
        }
        catch (Exception ex) when (ex is NullReferenceException or InvalidOperationException)
        {
            // Expected: ShowErrorAsync builds a ValtMessageBox (a Window), which cannot
            // be constructed/shown outside a real Avalonia app (null owner window /
            // missing IWindowingPlatform in the test environment).
        }

        // Assert - the modal factory never receives the GoalSummary name on query failure
        _ = _modalFactory.DidNotReceive().CreateAsync(
            Arg.Is(ApplicationModalNames.GoalSummary),
            Arg.Any<Window?>(),
            Arg.Any<object>());
    }

    [Test]
    public async Task ViewSummary_NotSupportedResult_DoesNotOpenModal()
    {
        // Arrange - goal types without contributing-transaction semantics (e.g. NetWorthBtc)
        // return NotSupported from the Phase 49 query; the modal must not open for them
        _queryDispatcher.DispatchAsync(Arg.Any<GetGoalContributingTransactionsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<GoalContributingTransactionsResult>.Success(
                new GoalContributingTransactionsResult.NotSupported(GoalTypeNames.NetWorthBtc))));

        var vm = CreateViewModel();
        var entry = new GoalEntryViewModel(
            CreateNetWorthBtcGoalDTO("goal-1", GoalPeriods.Monthly, new DateOnly(2025, 1, 1)),
            _currencySettings.MainFiatCurrency);

        // Act
        await vm.ViewSummaryCommand.ExecuteAsync(entry);

        // Assert - the modal factory never receives the GoalSummary name on NotSupported
        _ = _modalFactory.DidNotReceive().CreateAsync(
            Arg.Is(ApplicationModalNames.GoalSummary),
            Arg.Any<Window?>(),
            Arg.Any<object>());
    }

    [Test]
    public async Task ViewSummary_SupportedResult_OpensModalWithRequest()
    {
        // Arrange
        var supportedResult = new GoalContributingTransactionsResult.Supported(
            new List<ContributingTransactionRow>
            {
                new()
                {
                    Date = new DateOnly(2025, 1, 1),
                    Description = "Monthly buy",
                    AccountName = "Checking",
                    CategoryName = null,
                    FiatAmount = FiatValue.New(100m),
                    FiatCurrencyCode = "USD",
                    SatsAmount = BtcValue.ParseSats(5000m),
                    RunningTotal = 100m
                }
            });

        _queryDispatcher.DispatchAsync(Arg.Any<GetGoalContributingTransactionsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<GoalContributingTransactionsResult>.Success(supportedResult)));

        _modalFactory.CreateAsync(
                Arg.Is(ApplicationModalNames.GoalSummary),
                Arg.Any<Window?>(),
                Arg.Any<object>())
            .Returns(Task.FromResult<ValtBaseWindow>(null!));

        var vm = CreateViewModel();
        var entry = new GoalEntryViewModel(
            CreateStackBitcoinGoalDTO("goal-1", GoalPeriods.Monthly, new DateOnly(2025, 1, 1)),
            _currencySettings.MainFiatCurrency);

        // Act
        try
        {
            await vm.ViewSummaryCommand.ExecuteAsync(entry);
        }
        catch (NullReferenceException)
        {
            // Expected: the substitute factory returns null and ShowDialogSafeAsync
            // is invoked on a null view reference outside a real Avalonia app
            // (LoanStateHistoryViewModelTests precedent).
        }

        // Assert
        _ = _modalFactory.Received(1).CreateAsync(
            Arg.Is(ApplicationModalNames.GoalSummary),
            Arg.Any<Window?>(),
            Arg.Is<GoalSummaryViewModel.Request>(r =>
                r.GoalName == entry.FriendlyName &&
                r.PeriodLabel == "(01/25)" &&
                r.MainCurrencyCode == _currencySettings.MainFiatCurrency &&
                ReferenceEquals(r.Result, supportedResult) &&
                r.StrategyUnit == GoalStrategyUnit.Sats));
    }

    [Test]
    public void CanViewSummary_FalseForNetWorthBtc()
    {
        // Arrange
        var entry = new GoalEntryViewModel(
            CreateNetWorthBtcGoalDTO("goal-1", GoalPeriods.Monthly, new DateOnly(2025, 1, 1)),
            _currencySettings.MainFiatCurrency);

        // Act & Assert
        Assert.That(entry.CanViewSummary, Is.False);
    }

    [Test]
    public void CanViewSummary_TrueForTransactionBasedTypes()
    {
        // Arrange - one entry per transaction-based goal type
        var entries = new[]
        {
            new GoalEntryViewModel(CreateStackBitcoinGoalDTO("1", GoalPeriods.Monthly, new DateOnly(2025, 1, 1)), _currencySettings.MainFiatCurrency),
            new GoalEntryViewModel(CreateSpendingLimitGoalDTO("2", GoalPeriods.Monthly, new DateOnly(2025, 1, 1)), _currencySettings.MainFiatCurrency),
            new GoalEntryViewModel(CreateDcaGoalDTO("3", GoalPeriods.Monthly, new DateOnly(2025, 1, 1)), _currencySettings.MainFiatCurrency),
            new GoalEntryViewModel(CreateSavingsRateGoalDTO("4", GoalPeriods.Monthly, new DateOnly(2025, 1, 1)), _currencySettings.MainFiatCurrency),
            new GoalEntryViewModel(CreateGoalDTO(new IncomeFiatGoalTypeOutputDTO { TargetAmount = 1000m, CalculatedIncome = 0m }), _currencySettings.MainFiatCurrency),
            new GoalEntryViewModel(CreateGoalDTO(new IncomeBtcGoalTypeOutputDTO { TargetSats = 100_000, CalculatedSats = 0 }), _currencySettings.MainFiatCurrency),
            new GoalEntryViewModel(CreateGoalDTO(new ReduceExpenseCategoryGoalTypeOutputDTO { TargetAmount = 500m, CategoryId = "cat-1", CategoryName = "Food", CalculatedSpending = 0m }), _currencySettings.MainFiatCurrency),
            new GoalEntryViewModel(CreateGoalDTO(new BitcoinHodlGoalTypeOutputDTO { MaxSellableSats = 100_000, CalculatedSoldSats = 0 }), _currencySettings.MainFiatCurrency),
            new GoalEntryViewModel(CreateGoalDTO(new SaveFiatGoalTypeOutputDTO { TargetAmount = 1000m, CalculatedSavings = 0m }), _currencySettings.MainFiatCurrency)
        };

        // Act & Assert
        Assert.That(entries.Select(e => e.CanViewSummary), Is.All.True);
    }

    [Test]
    public void SummaryStrategyUnit_MapsGoalTypes()
    {
        // Arrange
        var stackBitcoin = new GoalEntryViewModel(CreateStackBitcoinGoalDTO("1", GoalPeriods.Monthly, new DateOnly(2025, 1, 1)), _currencySettings.MainFiatCurrency);
        var incomeBtc = new GoalEntryViewModel(CreateGoalDTO(new IncomeBtcGoalTypeOutputDTO { TargetSats = 100_000, CalculatedSats = 0 }), _currencySettings.MainFiatCurrency);
        var bitcoinHodl = new GoalEntryViewModel(CreateGoalDTO(new BitcoinHodlGoalTypeOutputDTO { MaxSellableSats = 100_000, CalculatedSoldSats = 0 }), _currencySettings.MainFiatCurrency);
        var spendingLimit = new GoalEntryViewModel(CreateSpendingLimitGoalDTO("2", GoalPeriods.Monthly, new DateOnly(2025, 1, 1)), _currencySettings.MainFiatCurrency);
        var dca = new GoalEntryViewModel(CreateDcaGoalDTO("3", GoalPeriods.Monthly, new DateOnly(2025, 1, 1)), _currencySettings.MainFiatCurrency);
        var savingsRate = new GoalEntryViewModel(CreateSavingsRateGoalDTO("4", GoalPeriods.Monthly, new DateOnly(2025, 1, 1)), _currencySettings.MainFiatCurrency);

        // Act & Assert
        Assert.That(stackBitcoin.SummaryStrategyUnit, Is.EqualTo(GoalStrategyUnit.Sats));
        Assert.That(incomeBtc.SummaryStrategyUnit, Is.EqualTo(GoalStrategyUnit.Sats));
        Assert.That(bitcoinHodl.SummaryStrategyUnit, Is.EqualTo(GoalStrategyUnit.Sats));
        Assert.That(spendingLimit.SummaryStrategyUnit, Is.EqualTo(GoalStrategyUnit.Fiat));
        Assert.That(dca.SummaryStrategyUnit, Is.EqualTo(GoalStrategyUnit.Count));
        Assert.That(savingsRate.SummaryStrategyUnit, Is.EqualTo(GoalStrategyUnit.Percentage));
    }

    #endregion

    #region Design-Time Constructor Tests

    [Test]
    public void DesignTime_Constructor_Should_Not_Throw_When_Not_In_Design_Mode()
    {
        // Arrange & Act - When not in design mode, constructor should not throw
        var vm = new GoalsPanelViewModel();

        // Assert - GoalEntries should be null or empty since Design.IsDesignMode is false
        // Design-time constructor only populates data when Avalonia designer is running
        Assert.DoesNotThrow(() => _ = vm.GoalEntries);
    }

    #endregion
}
