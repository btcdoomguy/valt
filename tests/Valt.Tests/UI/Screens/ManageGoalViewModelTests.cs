using NSubstitute;
using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.Categories.DTOs;
using Valt.App.Modules.Budget.Categories.Queries.GetCategories;
using Valt.App.Modules.Goals.Commands.CreateGoal;
using Valt.App.Modules.Goals.Commands.EditGoal;
using Valt.App.Modules.Goals.DTOs;
using Valt.App.Modules.Goals.Queries.GetGoal;
using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Goals;
using Valt.Infra.Kernel;
using Valt.Infra.Settings;
using Valt.Tests.Builders;
using Valt.UI.Views.Main.Modals.ManageGoal;
using Valt.UI.Views.Main.Modals.ManageGoal.GoalTypeEditors;

namespace Valt.Tests.UI.Screens;

[TestFixture]
public class ManageGoalViewModelTests : DatabaseTest
{
    private ICommandDispatcher _commandDispatcher = null!;
    private IQueryDispatcher _queryDispatcher = null!;
    private CurrencySettings _currencySettings = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        IdGenerator.Configure(new LiteDbIdProvider());
    }

    [SetUp]
    public new void SetUp()
    {
        _commandDispatcher = Substitute.For<ICommandDispatcher>();
        _queryDispatcher = Substitute.For<IQueryDispatcher>();
        _currencySettings = new CurrencySettings(_localDatabase, null!);
        _queryDispatcher.DispatchAsync(Arg.Any<GetCategoriesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new CategoriesDTO(new List<CategoryDTO>()));

        // Default command setup - success results
        _commandDispatcher.DispatchAsync(Arg.Any<CreateGoalCommand>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var cmd = callInfo.Arg<CreateGoalCommand>();
                return Result<CreateGoalResult>.Success(new CreateGoalResult("new-goal-id"));
            });
        _commandDispatcher.DispatchAsync(Arg.Any<EditGoalCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<EditGoalResult>.Success(new EditGoalResult()));
    }

    private ManageGoalViewModel CreateViewModel()
    {
        var vm = new ManageGoalViewModel(_commandDispatcher, _queryDispatcher, _currencySettings);
        vm.GetWindow = () => null!;
        vm.CloseWindow = () => { };
        vm.CloseDialog = _ => { };
        return vm;
    }

    private static GoalDTO CreateGoalDTO(
        string id,
        DateOnly refDate,
        GoalPeriods period,
        GoalTypeOutputDTO goalType)
    {
        return new GoalDTO
        {
            Id = id,
            RefDate = refDate,
            Period = (int)period,
            Progress = 0m,
            IsUpToDate = true,
            LastUpdatedAt = DateTime.UtcNow,
            State = 0,
            GoalType = goalType
        };
    }

    #region Initialization Tests

    [Test]
    public void Should_Initialize_With_Default_Values()
    {
        // Arrange & Act
        var vm = CreateViewModel();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(vm.SelectedPeriod, Is.EqualTo(GoalPeriods.Monthly.ToString()));
            Assert.That(vm.SelectedGoalType, Is.EqualTo(GoalTypeNames.StackBitcoin.ToString()));
            Assert.That(vm.SelectedMonth, Is.EqualTo(DateTime.Today.Month.ToString()));
            Assert.That(vm.SelectedYear, Is.EqualTo(DateTime.Today.Year));
            Assert.That(vm.CurrentGoalTypeEditor, Is.InstanceOf<StackBitcoinGoalTypeEditorViewModel>());
            Assert.That(vm.IsEditMode, Is.False);
        });
    }

    [Test]
    public void ShowMonthSelector_Should_Return_True_When_Period_Is_Monthly()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.SelectedPeriod = GoalPeriods.Monthly.ToString();

        // Assert
        Assert.That(vm.ShowMonthSelector, Is.True);
    }

    [Test]
    public void ShowMonthSelector_Should_Return_False_When_Period_Is_Yearly()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.SelectedPeriod = GoalPeriods.Yearly.ToString();

        // Assert
        Assert.That(vm.ShowMonthSelector, Is.False);
    }

    [Test]
    public void CurrentGoalTypeEditor_Should_Be_StackBitcoinGoalTypeEditorViewModel_When_GoalType_Is_StackBitcoin()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.SelectedGoalType = GoalTypeNames.StackBitcoin.ToString();

        // Assert
        Assert.That(vm.CurrentGoalTypeEditor, Is.InstanceOf<StackBitcoinGoalTypeEditorViewModel>());
    }

    [Test]
    public void CurrentGoalTypeEditor_Should_Be_SpendingLimitGoalTypeEditorViewModel_When_GoalType_Is_SpendingLimit()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.SelectedGoalType = GoalTypeNames.SpendingLimit.ToString();

        // Assert
        Assert.That(vm.CurrentGoalTypeEditor, Is.InstanceOf<SpendingLimitGoalTypeEditorViewModel>());
    }

    [Test]
    public void CurrentGoalTypeEditor_Should_Be_DcaGoalTypeEditorViewModel_When_GoalType_Is_Dca()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.SelectedGoalType = GoalTypeNames.Dca.ToString();

        // Assert
        Assert.That(vm.CurrentGoalTypeEditor, Is.InstanceOf<DcaGoalTypeEditorViewModel>());
    }

    [Test]
    public void AvailablePeriods_Should_Contain_Monthly_And_Yearly()
    {
        // Assert
        var periods = ManageGoalViewModel.AvailablePeriods;
        Assert.Multiple(() =>
        {
            Assert.That(periods.Any(p => p.Value == GoalPeriods.Monthly.ToString()), Is.True);
            Assert.That(periods.Any(p => p.Value == GoalPeriods.Yearly.ToString()), Is.True);
        });
    }

    [Test]
    public void AvailableGoalTypes_Should_Contain_StackBitcoin()
    {
        // Assert
        var goalTypes = ManageGoalViewModel.AvailableGoalTypes;
        Assert.That(goalTypes.Any(g => g.Value == GoalTypeNames.StackBitcoin.ToString()), Is.True);
    }

    [Test]
    public void AvailableMonths_Should_Contain_All_Twelve_Months()
    {
        // Assert
        var months = ManageGoalViewModel.AvailableMonths;
        Assert.That(months.Count, Is.EqualTo(12));
        Assert.That(months.First().Value, Is.EqualTo("1"));
        Assert.That(months.Last().Value, Is.EqualTo("12"));
        // Verify month names are not empty (localized names)
        Assert.That(months.All(m => !string.IsNullOrEmpty(m.Text)), Is.True);
    }

    #endregion

    #region Add Goal Tests

    [Test]
    public async Task OkCommand_Should_Create_New_Monthly_Goal_With_StackBitcoin_Type()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.SelectedPeriod = GoalPeriods.Monthly.ToString();
        vm.SelectedGoalType = GoalTypeNames.StackBitcoin.ToString();
        vm.SelectedYear = 2024;
        vm.SelectedMonth = "6";

        var stackBitcoinEditor = (StackBitcoinGoalTypeEditorViewModel)vm.CurrentGoalTypeEditor!;
        stackBitcoinEditor.TargetBtcAmount = BtcValue.New(1_000_000);

        CreateGoalCommand? capturedCommand = null;
        _commandDispatcher.DispatchAsync(Arg.Do<CreateGoalCommand>(c => capturedCommand = c), Arg.Any<CancellationToken>())
            .Returns(Result<CreateGoalResult>.Success(new CreateGoalResult("new-goal-id")));

        object? dialogResult = null;
        vm.CloseDialog = result => dialogResult = result;

        // Act
        await vm.OkCommand.ExecuteAsync(null);

        // Assert
        Assert.That(capturedCommand, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(capturedCommand!.Period, Is.EqualTo((int)GoalPeriods.Monthly));
            Assert.That(capturedCommand.RefDate.Year, Is.EqualTo(2024));
            Assert.That(capturedCommand.RefDate.Month, Is.EqualTo(6));
            Assert.That(capturedCommand.GoalType, Is.InstanceOf<StackBitcoinGoalTypeDTO>());
            Assert.That(((StackBitcoinGoalTypeDTO)capturedCommand.GoalType).TargetSats, Is.EqualTo(1_000_000));
            Assert.That(dialogResult, Is.InstanceOf<ManageGoalViewModel.Response>());
            Assert.That(((ManageGoalViewModel.Response)dialogResult!).Ok, Is.True);
        });
    }

    [Test]
    public async Task OkCommand_Should_Create_New_Yearly_Goal_With_Month_Set_To_January()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.SelectedPeriod = GoalPeriods.Yearly.ToString();
        vm.SelectedGoalType = GoalTypeNames.StackBitcoin.ToString();
        vm.SelectedYear = 2025;
        vm.SelectedMonth = "8"; // This should be ignored for yearly goals

        var stackBitcoinEditor = (StackBitcoinGoalTypeEditorViewModel)vm.CurrentGoalTypeEditor!;
        stackBitcoinEditor.TargetBtcAmount = BtcValue.New(5_000_000);

        CreateGoalCommand? capturedCommand = null;
        _commandDispatcher.DispatchAsync(Arg.Do<CreateGoalCommand>(c => capturedCommand = c), Arg.Any<CancellationToken>())
            .Returns(Result<CreateGoalResult>.Success(new CreateGoalResult("new-goal-id")));

        vm.CloseDialog = _ => { };

        // Act
        await vm.OkCommand.ExecuteAsync(null);

        // Assert
        Assert.That(capturedCommand, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(capturedCommand!.Period, Is.EqualTo((int)GoalPeriods.Yearly));
            Assert.That(capturedCommand.RefDate.Year, Is.EqualTo(2025));
            Assert.That(capturedCommand.RefDate.Month, Is.EqualTo(1), "Yearly goals should have month set to January");
            Assert.That(capturedCommand.RefDate.Day, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task OkCommand_Should_Dispatch_CreateGoalCommand()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.SelectedPeriod = GoalPeriods.Monthly.ToString();
        vm.SelectedGoalType = GoalTypeNames.StackBitcoin.ToString();

        var stackBitcoinEditor = (StackBitcoinGoalTypeEditorViewModel)vm.CurrentGoalTypeEditor!;
        stackBitcoinEditor.TargetBtcAmount = BtcValue.New(500_000);

        vm.CloseDialog = _ => { };

        // Act
        await vm.OkCommand.ExecuteAsync(null);

        // Assert
        await _commandDispatcher.Received(1).DispatchAsync(Arg.Any<CreateGoalCommand>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Edit Goal Tests

    [Test]
    public async Task OnBindParameterAsync_Should_Load_Goal_When_Valid_Id_Provided()
    {
        // Arrange
        var goalId = "goal-123";
        var goalDto = CreateGoalDTO(
            goalId,
            new DateOnly(2024, 3, 1),
            GoalPeriods.Monthly,
            new StackBitcoinGoalTypeOutputDTO { TargetSats = 2_000_000, CalculatedSats = 0 });

        _queryDispatcher.DispatchAsync(Arg.Any<GetGoalQuery>(), Arg.Any<CancellationToken>())
            .Returns(goalDto);

        var vm = CreateViewModel();
        vm.Parameter = goalId;

        // Act
        await vm.OnBindParameterAsync();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(vm.IsEditMode, Is.True);
            Assert.That(vm.SelectedPeriod, Is.EqualTo(GoalPeriods.Monthly.ToString()));
            Assert.That(vm.SelectedYear, Is.EqualTo(2024));
            Assert.That(vm.SelectedMonth, Is.EqualTo("3"));
            Assert.That(vm.SelectedGoalType, Is.EqualTo(GoalTypeNames.StackBitcoin.ToString()));
            Assert.That(vm.CurrentGoalTypeEditor, Is.InstanceOf<StackBitcoinGoalTypeEditorViewModel>());
            var stackBitcoinEditor = (StackBitcoinGoalTypeEditorViewModel)vm.CurrentGoalTypeEditor!;
            Assert.That(stackBitcoinEditor.TargetBtcAmount.Sats, Is.EqualTo(2_000_000));
        });
    }

    [Test]
    public async Task OnBindParameterAsync_Should_Load_Yearly_Goal()
    {
        // Arrange
        var goalId = "goal-yearly";
        var goalDto = CreateGoalDTO(
            goalId,
            new DateOnly(2025, 1, 1),
            GoalPeriods.Yearly,
            new StackBitcoinGoalTypeOutputDTO { TargetSats = 10_000_000, CalculatedSats = 0 });

        _queryDispatcher.DispatchAsync(Arg.Any<GetGoalQuery>(), Arg.Any<CancellationToken>())
            .Returns(goalDto);

        var vm = CreateViewModel();
        vm.Parameter = goalId;

        // Act
        await vm.OnBindParameterAsync();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(vm.IsEditMode, Is.True);
            Assert.That(vm.SelectedPeriod, Is.EqualTo(GoalPeriods.Yearly.ToString()));
            Assert.That(vm.SelectedYear, Is.EqualTo(2025));
            Assert.That(vm.CurrentGoalTypeEditor, Is.InstanceOf<StackBitcoinGoalTypeEditorViewModel>());
            var stackBitcoinEditor = (StackBitcoinGoalTypeEditorViewModel)vm.CurrentGoalTypeEditor!;
            Assert.That(stackBitcoinEditor.TargetBtcAmount.Sats, Is.EqualTo(10_000_000));
        });
    }

    [Test]
    public async Task OnBindParameterAsync_Should_Do_Nothing_When_Parameter_Is_Null()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.Parameter = null;

        // Act
        await vm.OnBindParameterAsync();

        // Assert
        Assert.That(vm.IsEditMode, Is.False);
        await _queryDispatcher.DidNotReceive().DispatchAsync(Arg.Any<GetGoalQuery>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task OkCommand_Should_Update_Existing_Goal()
    {
        // Arrange
        var goalId = "goal-edit";
        var goalDto = CreateGoalDTO(
            goalId,
            new DateOnly(2024, 3, 1),
            GoalPeriods.Monthly,
            new StackBitcoinGoalTypeOutputDTO { TargetSats = 1_000_000, CalculatedSats = 500_000 });

        _queryDispatcher.DispatchAsync(Arg.Any<GetGoalQuery>(), Arg.Any<CancellationToken>())
            .Returns(goalDto);

        var vm = CreateViewModel();
        vm.Parameter = goalId;
        await vm.OnBindParameterAsync();

        // Modify values
        vm.SelectedYear = 2024;
        vm.SelectedMonth = "5";
        var stackBitcoinEditor = (StackBitcoinGoalTypeEditorViewModel)vm.CurrentGoalTypeEditor!;
        stackBitcoinEditor.TargetBtcAmount = BtcValue.New(3_000_000);

        EditGoalCommand? capturedCommand = null;
        _commandDispatcher.DispatchAsync(Arg.Do<EditGoalCommand>(c => capturedCommand = c), Arg.Any<CancellationToken>())
            .Returns(Result<EditGoalResult>.Success(new EditGoalResult()));

        vm.CloseDialog = _ => { };

        // Act
        await vm.OkCommand.ExecuteAsync(null);

        // Assert
        Assert.That(capturedCommand, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(capturedCommand!.GoalId, Is.EqualTo(goalId));
            Assert.That(capturedCommand.RefDate.Year, Is.EqualTo(2024));
            Assert.That(capturedCommand.RefDate.Month, Is.EqualTo(5));
            Assert.That(((StackBitcoinGoalTypeDTO)capturedCommand.GoalType).TargetSats, Is.EqualTo(3_000_000));
        });
    }

    [Test]
    public async Task OkCommand_In_Edit_Mode_Should_Dispatch_EditGoalCommand()
    {
        // Arrange
        var goalId = "goal-for-edit";
        var goalDto = CreateGoalDTO(
            goalId,
            new DateOnly(2024, 3, 1),
            GoalPeriods.Monthly,
            new StackBitcoinGoalTypeOutputDTO { TargetSats = 1_000_000, CalculatedSats = 750_000 });

        _queryDispatcher.DispatchAsync(Arg.Any<GetGoalQuery>(), Arg.Any<CancellationToken>())
            .Returns(goalDto);

        var vm = CreateViewModel();
        vm.Parameter = goalId;
        await vm.OnBindParameterAsync();

        vm.CloseDialog = _ => { };

        // Act
        await vm.OkCommand.ExecuteAsync(null);

        // Assert
        await _commandDispatcher.Received(1).DispatchAsync(Arg.Any<EditGoalCommand>(), Arg.Any<CancellationToken>());
        await _commandDispatcher.DidNotReceive().DispatchAsync(Arg.Any<CreateGoalCommand>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Cancel and Close Commands Tests

    [Test]
    public void CancelCommand_Should_Close_Window()
    {
        // Arrange
        var vm = CreateViewModel();
        var closeWindowCalled = false;
        vm.CloseWindow = () => closeWindowCalled = true;

        // Act
        vm.CancelCommand.Execute(null);

        // Assert
        Assert.That(closeWindowCalled, Is.True);
    }

    [Test]
    public async Task CloseCommand_Should_Close_Window()
    {
        // Arrange
        var vm = CreateViewModel();
        var closeWindowCalled = false;
        vm.CloseWindow = () => closeWindowCalled = true;

        // Act
        await vm.CloseCommand.ExecuteAsync(null);

        // Assert
        Assert.That(closeWindowCalled, Is.True);
    }

    #endregion

    #region PropertyChanged Notifications Tests

    [Test]
    public void Should_Notify_ShowMonthSelector_When_SelectedPeriod_Changes()
    {
        // Arrange
        var vm = CreateViewModel();
        var changedProperties = new List<string>();
        vm.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName!);

        // Act
        vm.SelectedPeriod = GoalPeriods.Yearly.ToString();

        // Assert
        Assert.That(changedProperties, Contains.Item(nameof(vm.ShowMonthSelector)));
    }

    [Test]
    public void Should_Change_CurrentGoalTypeEditor_When_SelectedGoalType_Changes()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act & Assert for StackBitcoin
        vm.SelectedGoalType = GoalTypeNames.StackBitcoin.ToString();
        Assert.That(vm.CurrentGoalTypeEditor, Is.InstanceOf<StackBitcoinGoalTypeEditorViewModel>());

        // Act & Assert for SpendingLimit
        vm.SelectedGoalType = GoalTypeNames.SpendingLimit.ToString();
        Assert.That(vm.CurrentGoalTypeEditor, Is.InstanceOf<SpendingLimitGoalTypeEditorViewModel>());

        // Act & Assert for Dca
        vm.SelectedGoalType = GoalTypeNames.Dca.ToString();
        Assert.That(vm.CurrentGoalTypeEditor, Is.InstanceOf<DcaGoalTypeEditorViewModel>());
    }

    #endregion

    #region Design-Time Constructor Tests

    [Test]
    public void DesignTime_Constructor_Should_Set_Default_Values()
    {
        // Arrange & Act
        var vm = new ManageGoalViewModel();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(vm.SelectedPeriod, Is.EqualTo(GoalPeriods.Monthly.ToString()));
            Assert.That(vm.SelectedGoalType, Is.EqualTo(GoalTypeNames.StackBitcoin.ToString()));
            Assert.That(vm.CurrentGoalTypeEditor, Is.InstanceOf<StackBitcoinGoalTypeEditorViewModel>());
        });
    }

    #endregion

    #region SpendingLimit Goal Type Tests

    [Test]
    public async Task OkCommand_Should_Create_New_Goal_With_SpendingLimit_Type()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.SelectedPeriod = GoalPeriods.Monthly.ToString();
        vm.SelectedGoalType = GoalTypeNames.SpendingLimit.ToString();
        vm.SelectedYear = 2024;
        vm.SelectedMonth = "6";

        var spendingLimitEditor = (SpendingLimitGoalTypeEditorViewModel)vm.CurrentGoalTypeEditor!;
        spendingLimitEditor.TargetFiatAmount = FiatValue.New(5000m);

        CreateGoalCommand? capturedCommand = null;
        _commandDispatcher.DispatchAsync(Arg.Do<CreateGoalCommand>(c => capturedCommand = c), Arg.Any<CancellationToken>())
            .Returns(Result<CreateGoalResult>.Success(new CreateGoalResult("new-goal-id")));

        vm.CloseDialog = _ => { };

        // Act
        await vm.OkCommand.ExecuteAsync(null);

        // Assert
        Assert.That(capturedCommand, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(capturedCommand!.GoalType, Is.InstanceOf<SpendingLimitGoalTypeDTO>());
            var spendingLimit = (SpendingLimitGoalTypeDTO)capturedCommand.GoalType;
            Assert.That(spendingLimit.TargetAmount, Is.EqualTo(5000m));
        });
    }

    [Test]
    public async Task OnBindParameterAsync_Should_Load_SpendingLimit_Goal()
    {
        // Arrange
        var goalId = "goal-spending";
        var goalDto = CreateGoalDTO(
            goalId,
            new DateOnly(2024, 3, 1),
            GoalPeriods.Monthly,
            new SpendingLimitGoalTypeOutputDTO { TargetAmount = 3000m, CalculatedSpending = 0m });

        _queryDispatcher.DispatchAsync(Arg.Any<GetGoalQuery>(), Arg.Any<CancellationToken>())
            .Returns(goalDto);

        var vm = CreateViewModel();
        vm.Parameter = goalId;

        // Act
        await vm.OnBindParameterAsync();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(vm.CurrentGoalTypeEditor, Is.InstanceOf<SpendingLimitGoalTypeEditorViewModel>());
            var spendingLimitEditor = (SpendingLimitGoalTypeEditorViewModel)vm.CurrentGoalTypeEditor!;
            Assert.That(spendingLimitEditor.TargetFiatAmount.Value, Is.EqualTo(3000m));
        });
    }

    #endregion

    #region DCA Goal Type Tests

    [Test]
    public async Task OkCommand_Should_Create_New_Goal_With_Dca_Type()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.SelectedPeriod = GoalPeriods.Monthly.ToString();
        vm.SelectedGoalType = GoalTypeNames.Dca.ToString();
        vm.SelectedYear = 2024;
        vm.SelectedMonth = "6";

        var dcaEditor = (DcaGoalTypeEditorViewModel)vm.CurrentGoalTypeEditor!;
        dcaEditor.TargetPurchaseCount = 8;

        CreateGoalCommand? capturedCommand = null;
        _commandDispatcher.DispatchAsync(Arg.Do<CreateGoalCommand>(c => capturedCommand = c), Arg.Any<CancellationToken>())
            .Returns(Result<CreateGoalResult>.Success(new CreateGoalResult("new-goal-id")));

        vm.CloseDialog = _ => { };

        // Act
        await vm.OkCommand.ExecuteAsync(null);

        // Assert
        Assert.That(capturedCommand, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(capturedCommand!.GoalType, Is.InstanceOf<DcaGoalTypeDTO>());
            var dca = (DcaGoalTypeDTO)capturedCommand.GoalType;
            Assert.That(dca.TargetPurchaseCount, Is.EqualTo(8));
        });
    }

    [Test]
    public async Task OnBindParameterAsync_Should_Load_Dca_Goal()
    {
        // Arrange
        var goalId = "goal-dca";
        var goalDto = CreateGoalDTO(
            goalId,
            new DateOnly(2024, 3, 1),
            GoalPeriods.Monthly,
            new DcaGoalTypeOutputDTO { TargetPurchaseCount = 12, CalculatedPurchaseCount = 0 });

        _queryDispatcher.DispatchAsync(Arg.Any<GetGoalQuery>(), Arg.Any<CancellationToken>())
            .Returns(goalDto);

        var vm = CreateViewModel();
        vm.Parameter = goalId;

        // Act
        await vm.OnBindParameterAsync();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(vm.CurrentGoalTypeEditor, Is.InstanceOf<DcaGoalTypeEditorViewModel>());
            var dcaEditor = (DcaGoalTypeEditorViewModel)vm.CurrentGoalTypeEditor!;
            Assert.That(dcaEditor.TargetPurchaseCount, Is.EqualTo(12));
        });
    }

    #endregion
}
