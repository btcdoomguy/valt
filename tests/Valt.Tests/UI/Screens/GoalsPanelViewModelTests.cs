using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.Contracts;
using Valt.Core.Modules.Goals.GoalTypes;
using Valt.Infra.Kernel;
using Valt.Infra.Modules.Goals.Services;
using Valt.Infra.Settings;
using Valt.Tests.Builders;
using Valt.UI.Services;
using Valt.UI.State;
using Valt.UI.Views.Main.Tabs.Transactions;
using Valt.UI.Views.Main.Tabs.Transactions.Models;

namespace Valt.Tests.UI.Screens;

[TestFixture]
public class GoalsPanelViewModelTests : DatabaseTest
{
    private IModalFactory _modalFactory = null!;
    private IGoalRepository _goalRepository = null!;
    private GoalProgressState _goalProgressState = null!;
    private CurrencySettings _currencySettings = null!;
    private FilterState _filterState = null!;
    private ILogger<GoalsPanelViewModel> _logger = null!;
    private SecureModeState _secureModeState = null!;

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

        _modalFactory = Substitute.For<IModalFactory>();
        _goalRepository = Substitute.For<IGoalRepository>();
        _goalProgressState = new GoalProgressState();
        _currencySettings = new CurrencySettings(_localDatabase);
        _filterState = new FilterState { MainDate = new DateTime(2025, 1, 15) };
        _logger = Substitute.For<ILogger<GoalsPanelViewModel>>();
        _secureModeState = new SecureModeState();

        // Default mock for goal repository
        _goalRepository.GetAllAsync()
            .Returns(Task.FromResult<IEnumerable<Goal>>(new List<Goal>()));
    }

    [TearDown]
    public void TearDown()
    {
        WeakReferenceMessenger.Default.Reset();
    }

    private GoalsPanelViewModel CreateViewModel()
    {
        var vm = new GoalsPanelViewModel(
            _modalFactory,
            _goalRepository,
            _goalProgressState,
            _currencySettings,
            _filterState,
            _logger,
            _secureModeState);
        vm.GetUserControlOwnerWindow = () => null!;
        return vm;
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
        var currentDate = new DateOnly(2025, 1, 15);
        var goals = new List<Goal>
        {
            GoalBuilder.AGoal()
                .WithPeriod(GoalPeriods.Monthly)
                .WithRefDate(new DateOnly(2025, 1, 1))
                .WithGoalType(new StackBitcoinGoalType(1_000_000L))
                .Build(),
            GoalBuilder.AGoal()
                .WithPeriod(GoalPeriods.Monthly)
                .WithRefDate(new DateOnly(2025, 1, 1))
                .WithGoalType(new SpendingLimitGoalType(5000m))
                .Build()
        };

        _goalRepository.GetAllAsync()
            .Returns(Task.FromResult<IEnumerable<Goal>>(goals));

        // Act
        var vm = CreateViewModel();
        await Task.Delay(200); // Wait for async initialization

        // Assert
        Assert.That(vm.GoalEntries, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task Should_Filter_Goals_By_Current_Period()
    {
        // Arrange
        var goals = new List<Goal>
        {
            // Goal for January 2025 (should be included)
            GoalBuilder.AGoal()
                .WithPeriod(GoalPeriods.Monthly)
                .WithRefDate(new DateOnly(2025, 1, 1))
                .WithGoalType(new StackBitcoinGoalType(1_000_000L))
                .Build(),
            // Goal for February 2025 (should NOT be included when filter is January)
            GoalBuilder.AGoal()
                .WithPeriod(GoalPeriods.Monthly)
                .WithRefDate(new DateOnly(2025, 2, 1))
                .WithGoalType(new SpendingLimitGoalType(5000m))
                .Build(),
            // Yearly goal for 2025 (should be included)
            GoalBuilder.AGoal()
                .WithPeriod(GoalPeriods.Yearly)
                .WithRefDate(new DateOnly(2025, 1, 1))
                .WithGoalType(new DcaGoalType(12))
                .Build()
        };

        _goalRepository.GetAllAsync()
            .Returns(Task.FromResult<IEnumerable<Goal>>(goals));

        _filterState.MainDate = new DateTime(2025, 1, 15);

        // Act
        var vm = CreateViewModel();
        await Task.Delay(200);

        // Assert - should only include January 2025 monthly goal and 2025 yearly goal
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
        var goals = new List<Goal>
        {
            // Completed goal
            GoalBuilder.AGoal()
                .WithPeriod(GoalPeriods.Monthly)
                .WithRefDate(new DateOnly(2025, 1, 1))
                .WithGoalType(new StackBitcoinGoalType(1_000_000L))
                .WithState(GoalStates.Completed)
                .Build(),
            // Open monthly goal
            GoalBuilder.AGoal()
                .WithPeriod(GoalPeriods.Monthly)
                .WithRefDate(new DateOnly(2025, 1, 1))
                .WithGoalType(new SpendingLimitGoalType(5000m))
                .WithState(GoalStates.Open)
                .Build(),
            // Open yearly goal
            GoalBuilder.AGoal()
                .WithPeriod(GoalPeriods.Yearly)
                .WithRefDate(new DateOnly(2025, 1, 1))
                .WithGoalType(new DcaGoalType(12))
                .WithState(GoalStates.Open)
                .Build()
        };

        _goalRepository.GetAllAsync()
            .Returns(Task.FromResult<IEnumerable<Goal>>(goals));

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
        _goalRepository.GetAllAsync()
            .Returns(callInfo =>
            {
                callCount++;
                return Task.FromResult<IEnumerable<Goal>>(new List<Goal>());
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
        _goalRepository.GetAllAsync()
            .Returns(callInfo =>
            {
                callCount++;
                return Task.FromResult<IEnumerable<Goal>>(new List<Goal>());
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
        var goal = GoalBuilder.AGoal()
            .WithPeriod(GoalPeriods.Monthly)
            .WithRefDate(new DateOnly(2025, 1, 1))
            .WithGoalType(new StackBitcoinGoalType(1_000_000L))
            .WithProgress(25m)
            .Build();

        _goalRepository.GetAllAsync()
            .Returns(Task.FromResult<IEnumerable<Goal>>(new List<Goal> { goal }));

        var vm = CreateViewModel();
        await Task.Delay(200);

        Assert.That(vm.GoalEntries, Has.Count.EqualTo(1));
        Assert.That(vm.GoalEntries[0].Progress, Is.EqualTo(25m));

        // Update the goal progress
        goal.UpdateProgress(50m, goal.GoalType, DateTime.Now);

        // Act
        WeakReferenceMessenger.Default.Send(new GoalProgressUpdated());
        await Task.Delay(100);

        // Assert - progress should be updated (note: the UpdateGoal method updates AnimatedProgressPercentage)
        Assert.That(vm.GoalEntries[0].Progress, Is.EqualTo(50m));
    }

    #endregion

    #region Command Tests

    [Test]
    public async Task RecalculateGoalCommand_Should_Recalculate_And_Mark_Stale()
    {
        // Arrange
        var goal = GoalBuilder.AGoal()
            .WithPeriod(GoalPeriods.Monthly)
            .WithRefDate(new DateOnly(2025, 1, 1))
            .WithGoalType(new StackBitcoinGoalType(1_000_000L))
            .WithState(GoalStates.Completed)
            .Build();

        _goalRepository.GetByIdAsync(Arg.Any<GoalId>())
            .Returns(Task.FromResult<Goal?>(goal));
        _goalRepository.GetAllAsync()
            .Returns(Task.FromResult<IEnumerable<Goal>>(new List<Goal> { goal }));

        var vm = CreateViewModel();
        await Task.Delay(200);

        var entry = new GoalEntryViewModel(goal, _currencySettings.MainFiatCurrency);

        // Act
        await vm.RecalculateGoalCommand.ExecuteAsync(entry);

        // Assert
        await _goalRepository.Received(1).SaveAsync(Arg.Any<Goal>());
        Assert.That(_goalProgressState.HasStaleGoals, Is.True);
    }

    [Test]
    public async Task DeleteGoalCommand_Should_Not_Delete_Without_Entry()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        await vm.DeleteGoalCommand.ExecuteAsync(null);

        // Assert
        await _goalRepository.DidNotReceive().DeleteAsync(Arg.Any<Goal>());
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
    public async Task CopyFromLastMonth_Should_Copy_Monthly_Goals_From_Previous_Month()
    {
        // Arrange - Previous month goal (December 2024)
        var previousMonthGoal = GoalBuilder.AGoal()
            .WithPeriod(GoalPeriods.Monthly)
            .WithRefDate(new DateOnly(2024, 12, 1))
            .WithGoalType(new StackBitcoinGoalType(1_000_000L, 500_000L))
            .Build();

        _goalRepository.GetAllAsync()
            .Returns(Task.FromResult<IEnumerable<Goal>>(new List<Goal> { previousMonthGoal }));

        _filterState.MainDate = new DateTime(2025, 1, 15);

        var vm = CreateViewModel();
        await Task.Delay(200);

        // Act
        await vm.CopyFromLastMonthCommand.ExecuteAsync(null);

        // Assert - Should save a new goal
        await _goalRepository.Received(1).SaveAsync(Arg.Is<Goal>(g =>
            g.Period == GoalPeriods.Monthly &&
            g.RefDate.Year == 2025 &&
            g.RefDate.Month == 1));
    }

    [Test]
    public async Task CopyFromLastMonth_Should_Skip_Yearly_Goals()
    {
        // Arrange - Previous month has yearly goal
        var yearlyGoal = GoalBuilder.AGoal()
            .WithPeriod(GoalPeriods.Yearly)
            .WithRefDate(new DateOnly(2024, 12, 1))
            .WithGoalType(new DcaGoalType(12, 6))
            .Build();

        _goalRepository.GetAllAsync()
            .Returns(Task.FromResult<IEnumerable<Goal>>(new List<Goal> { yearlyGoal }));

        _filterState.MainDate = new DateTime(2025, 1, 15);

        var vm = CreateViewModel();
        await Task.Delay(200);

        // Act
        await vm.CopyFromLastMonthCommand.ExecuteAsync(null);

        // Assert - Should NOT save any goal (yearly goals are skipped)
        await _goalRepository.DidNotReceive().SaveAsync(Arg.Any<Goal>());
    }

    [Test]
    public async Task CopyFromLastMonth_Should_Skip_Duplicate_Goals()
    {
        // Arrange - Same goal exists in both months
        var previousMonthGoal = GoalBuilder.AGoal()
            .WithPeriod(GoalPeriods.Monthly)
            .WithRefDate(new DateOnly(2024, 12, 1))
            .WithGoalType(new StackBitcoinGoalType(1_000_000L, 500_000L))
            .Build();

        var currentMonthGoal = GoalBuilder.AGoal()
            .WithPeriod(GoalPeriods.Monthly)
            .WithRefDate(new DateOnly(2025, 1, 1))
            .WithGoalType(new StackBitcoinGoalType(1_000_000L, 0L))
            .Build();

        _goalRepository.GetAllAsync()
            .Returns(Task.FromResult<IEnumerable<Goal>>(new List<Goal>
            {
                previousMonthGoal,
                currentMonthGoal
            }));

        _filterState.MainDate = new DateTime(2025, 1, 15);

        var vm = CreateViewModel();
        await Task.Delay(200);

        // Act
        await vm.CopyFromLastMonthCommand.ExecuteAsync(null);

        // Assert - Should NOT save any goal (duplicate detected)
        await _goalRepository.DidNotReceive().SaveAsync(Arg.Any<Goal>());
    }

    [Test]
    public async Task CopyFromLastMonth_Should_Allow_Same_Type_With_Different_Target()
    {
        // Arrange - Same type but different target
        var previousMonthGoal = GoalBuilder.AGoal()
            .WithPeriod(GoalPeriods.Monthly)
            .WithRefDate(new DateOnly(2024, 12, 1))
            .WithGoalType(new StackBitcoinGoalType(1_000_000L, 500_000L))
            .Build();

        var currentMonthGoal = GoalBuilder.AGoal()
            .WithPeriod(GoalPeriods.Monthly)
            .WithRefDate(new DateOnly(2025, 1, 1))
            .WithGoalType(new StackBitcoinGoalType(2_000_000L, 0L)) // Different target
            .Build();

        _goalRepository.GetAllAsync()
            .Returns(Task.FromResult<IEnumerable<Goal>>(new List<Goal>
            {
                previousMonthGoal,
                currentMonthGoal
            }));

        _filterState.MainDate = new DateTime(2025, 1, 15);

        var vm = CreateViewModel();
        await Task.Delay(200);

        // Act
        await vm.CopyFromLastMonthCommand.ExecuteAsync(null);

        // Assert - Should save a new goal (different target means not duplicate)
        await _goalRepository.Received(1).SaveAsync(Arg.Is<Goal>(g =>
            g.GoalType.TypeName == GoalTypeNames.StackBitcoin &&
            ((StackBitcoinGoalType)g.GoalType).TargetSats == 1_000_000L));
    }

    [Test]
    public async Task CopyFromLastMonth_Should_Send_GoalListChanged_Message()
    {
        // Arrange
        var previousMonthGoal = GoalBuilder.AGoal()
            .WithPeriod(GoalPeriods.Monthly)
            .WithRefDate(new DateOnly(2024, 12, 1))
            .WithGoalType(new SpendingLimitGoalType(1000m, 500m))
            .Build();

        _goalRepository.GetAllAsync()
            .Returns(Task.FromResult<IEnumerable<Goal>>(new List<Goal> { previousMonthGoal }));

        _filterState.MainDate = new DateTime(2025, 1, 15);

        var vm = CreateViewModel();
        await Task.Delay(200);

        var messageReceived = false;
        WeakReferenceMessenger.Default.Register<GoalListChanged>(this, (_, _) => messageReceived = true);

        // Act
        await vm.CopyFromLastMonthCommand.ExecuteAsync(null);

        // Assert
        Assert.That(messageReceived, Is.True);
    }

    [Test]
    public async Task CopyFromLastMonth_Should_Mark_Progress_State_As_Stale()
    {
        // Arrange
        var previousMonthGoal = GoalBuilder.AGoal()
            .WithPeriod(GoalPeriods.Monthly)
            .WithRefDate(new DateOnly(2024, 12, 1))
            .WithGoalType(new DcaGoalType(10, 5))
            .Build();

        _goalRepository.GetAllAsync()
            .Returns(Task.FromResult<IEnumerable<Goal>>(new List<Goal> { previousMonthGoal }));

        _filterState.MainDate = new DateTime(2025, 1, 15);

        var vm = CreateViewModel();
        await Task.Delay(200);

        // Act
        await vm.CopyFromLastMonthCommand.ExecuteAsync(null);

        // Assert
        Assert.That(_goalProgressState.HasStaleGoals, Is.True);
    }

    [Test]
    public async Task CopyFromLastMonth_Should_Handle_Cross_Year_Boundary()
    {
        // Arrange - December 2024 to January 2025
        var decemberGoal = GoalBuilder.AGoal()
            .WithPeriod(GoalPeriods.Monthly)
            .WithRefDate(new DateOnly(2024, 12, 1))
            .WithGoalType(new IncomeFiatGoalType(5000m, 3000m))
            .Build();

        _goalRepository.GetAllAsync()
            .Returns(Task.FromResult<IEnumerable<Goal>>(new List<Goal> { decemberGoal }));

        _filterState.MainDate = new DateTime(2025, 1, 15);

        var vm = CreateViewModel();
        await Task.Delay(200);

        // Act
        await vm.CopyFromLastMonthCommand.ExecuteAsync(null);

        // Assert - Should save a new goal for January 2025
        await _goalRepository.Received(1).SaveAsync(Arg.Is<Goal>(g =>
            g.RefDate.Year == 2025 &&
            g.RefDate.Month == 1));
    }

    [Test]
    public async Task CopyFromLastMonth_Should_Do_Nothing_When_No_Previous_Month_Goals()
    {
        // Arrange - No goals at all
        _goalRepository.GetAllAsync()
            .Returns(Task.FromResult<IEnumerable<Goal>>(new List<Goal>()));

        _filterState.MainDate = new DateTime(2025, 1, 15);

        var vm = CreateViewModel();
        await Task.Delay(200);

        // Act
        await vm.CopyFromLastMonthCommand.ExecuteAsync(null);

        // Assert
        await _goalRepository.DidNotReceive().SaveAsync(Arg.Any<Goal>());
    }

    [Test]
    public async Task CopyFromLastMonth_Should_Reset_Progress_On_Copied_Goals()
    {
        // Arrange - Goal with progress
        var previousMonthGoal = GoalBuilder.AGoal()
            .WithPeriod(GoalPeriods.Monthly)
            .WithRefDate(new DateOnly(2024, 12, 1))
            .WithGoalType(new StackBitcoinGoalType(1_000_000L, 750_000L)) // 75% progress
            .Build();

        _goalRepository.GetAllAsync()
            .Returns(Task.FromResult<IEnumerable<Goal>>(new List<Goal> { previousMonthGoal }));

        _filterState.MainDate = new DateTime(2025, 1, 15);

        var vm = CreateViewModel();
        await Task.Delay(200);

        // Act
        await vm.CopyFromLastMonthCommand.ExecuteAsync(null);

        // Assert - Copied goal should have reset progress (0)
        await _goalRepository.Received(1).SaveAsync(Arg.Is<Goal>(g =>
            g.GoalType.TypeName == GoalTypeNames.StackBitcoin &&
            ((StackBitcoinGoalType)g.GoalType).CalculatedSats == 0));
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
