using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Valt.Core.Common;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.FixedExpenses;
using Valt.Core.Modules.Budget.FixedExpenses.Contracts;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Infra.Kernel;
using Valt.Infra.Modules.Budget.FixedExpenses;
using Valt.Infra.Settings;
using Valt.UI.Services;
using Valt.UI.State;
using Valt.UI.Views.Main.Tabs.Transactions;
using Valt.UI.Views.Main.Tabs.Transactions.Models;

namespace Valt.Tests.UI.Screens;

[TestFixture]
public class FixedExpensesPanelViewModelTests : DatabaseTest
{
    private IModalFactory _modalFactory = null!;
    private IFixedExpenseProvider _fixedExpenseProvider = null!;
    private IFixedExpenseRecordService _fixedExpenseRecordService = null!;
    private RatesState _ratesState = null!;
    private CurrencySettings _currencySettings = null!;
    private FilterState _filterState = null!;
    private IClock _clock = null!;
    private ILogger<FixedExpensesPanelViewModel> _logger = null!;
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
        _fixedExpenseProvider = Substitute.For<IFixedExpenseProvider>();
        _fixedExpenseRecordService = Substitute.For<IFixedExpenseRecordService>();
        _ratesState = new RatesState();
        _ratesState.FiatRates = new Dictionary<string, decimal> { { "BRL", 5.0m }, { "USD", 1.0m } };
        _currencySettings = new CurrencySettings(_localDatabase);
        _filterState = new FilterState { MainDate = new DateTime(2025, 1, 15) };
        _clock = Substitute.For<IClock>();
        _clock.GetCurrentLocalDate().Returns(new DateOnly(2025, 1, 15));
        _logger = Substitute.For<ILogger<FixedExpensesPanelViewModel>>();
        _secureModeState = new SecureModeState();

        // Default mock for fixed expense provider
        _fixedExpenseProvider.GetFixedExpensesOfMonthAsync(Arg.Any<DateOnly>())
            .Returns(Task.FromResult<IEnumerable<FixedExpenseProviderEntry>>(new List<FixedExpenseProviderEntry>()));
    }

    [TearDown]
    public void TearDown()
    {
        WeakReferenceMessenger.Default.Reset();
        _ratesState?.Dispose();
    }

    private FixedExpensesPanelViewModel CreateViewModel()
    {
        var vm = new FixedExpensesPanelViewModel(
            _modalFactory,
            _fixedExpenseProvider,
            _fixedExpenseRecordService,
            _ratesState,
            _currencySettings,
            _filterState,
            _clock,
            _logger,
            _secureModeState);
        vm.GetUserControlOwnerWindow = () => null!;
        return vm;
    }

    #region Initialization Tests

    [Test]
    public async Task Should_Initialize_With_Empty_FixedExpenseEntries()
    {
        // Arrange & Act
        var vm = CreateViewModel();
        await Task.Delay(100); // Wait for async initialization

        // Assert
        Assert.That(vm.FixedExpenseEntries, Is.Empty);
    }

    [Test]
    public async Task Should_Fetch_FixedExpenses_On_Initialization()
    {
        // Arrange
        var fixedExpenses = new List<FixedExpenseProviderEntry>
        {
            new(new FixedExpenseId().Value, "Rent", new CategoryId(), new DateOnly(2025, 1, 5), null, 1000m, null, null,
                FiatCurrency.Usd.Code, FixedExpenseRecordState.Empty, null),
            new(new FixedExpenseId().Value, "Internet", new CategoryId(), new DateOnly(2025, 1, 10), null, 100m, null,
                null, FiatCurrency.Usd.Code, FixedExpenseRecordState.Paid, new TransactionId())
        };

        _fixedExpenseProvider.GetFixedExpensesOfMonthAsync(Arg.Any<DateOnly>())
            .Returns(Task.FromResult<IEnumerable<FixedExpenseProviderEntry>>(fixedExpenses));

        // Act
        var vm = CreateViewModel();
        await Task.Delay(200); // Wait for async initialization

        // Assert
        Assert.That(vm.FixedExpenseEntries, Has.Count.EqualTo(2));
    }

    [Test]
    public void Should_Return_Correct_Month_Description()
    {
        // Arrange
        _filterState.MainDate = new DateTime(2025, 3, 15);

        // Act
        var vm = CreateViewModel();

        // Assert
        Assert.That(vm.FixedExpenseCurrentMonthDescription, Is.EqualTo("(03/25)"));
    }

    #endregion

    #region Secure Mode Tests

    [Test]
    public void DisplayRemainingFixedExpensesAmount_Should_Return_Masked_When_SecureMode_Enabled()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.RemainingFixedExpensesAmount = "R$ 1.234,56";
        _secureModeState.IsEnabled = true;

        // Act
        var result = vm.DisplayRemainingFixedExpensesAmount;

        // Assert
        Assert.That(result, Is.EqualTo("---"));
    }

    [Test]
    public void DisplayRemainingFixedExpensesAmount_Should_Return_Actual_When_SecureMode_Disabled()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.RemainingFixedExpensesAmount = "R$ 1.234,56";
        _secureModeState.IsEnabled = false;

        // Act
        var result = vm.DisplayRemainingFixedExpensesAmount;

        // Assert
        Assert.That(result, Is.EqualTo("R$ 1.234,56"));
    }

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
    public async Task Should_Refresh_When_TransactionListChanged_Message_Received()
    {
        // Arrange
        var callCount = 0;
        _fixedExpenseProvider.GetFixedExpensesOfMonthAsync(Arg.Any<DateOnly>())
            .Returns(callInfo =>
            {
                callCount++;
                return Task.FromResult<IEnumerable<FixedExpenseProviderEntry>>(
                    new List<FixedExpenseProviderEntry>());
            });

        var vm = CreateViewModel();
        await Task.Delay(100); // Wait for initial fetch
        var initialCallCount = callCount;

        // Act
        WeakReferenceMessenger.Default.Send(new TransactionListChanged());
        await Task.Delay(100);

        // Assert
        Assert.That(callCount, Is.GreaterThan(initialCallCount));
    }

    [Test]
    public async Task Should_Refresh_When_FilterDateRangeChanged_Message_Received()
    {
        // Arrange
        var callCount = 0;
        _fixedExpenseProvider.GetFixedExpensesOfMonthAsync(Arg.Any<DateOnly>())
            .Returns(callInfo =>
            {
                callCount++;
                return Task.FromResult<IEnumerable<FixedExpenseProviderEntry>>(
                    new List<FixedExpenseProviderEntry>());
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
    public void Should_Send_FixedExpenseChanged_When_SelectedFixedExpense_Changes()
    {
        // Arrange
        FixedExpenseChanged? receivedMessage = null;
        WeakReferenceMessenger.Default.Register<FixedExpenseChanged>(this, (_, msg) => receivedMessage = msg);

        var vm = CreateViewModel();
        var fixedExpense = new FixedExpensesEntryViewModel(
            new FixedExpenseProviderEntry(new FixedExpenseId().Value, "Test", new CategoryId(),
                new DateOnly(2025, 1, 10), null, 100m, null, null, FiatCurrency.Usd.Code),
            new DateOnly(2025, 1, 15));

        // Act
        vm.SelectedFixedExpense = fixedExpense;

        // Assert
        Assert.That(receivedMessage, Is.Not.Null);
        Assert.That(receivedMessage!.Value, Is.EqualTo(fixedExpense));
    }

    #endregion

    #region Command Tests

    [Test]
    public async Task IgnoreFixedExpenseCommand_Should_Call_Service()
    {
        // Arrange
        var vm = CreateViewModel();
        var entry = new FixedExpensesEntryViewModel(
            new FixedExpenseProviderEntry(new FixedExpenseId().Value, "Test", new CategoryId(),
                new DateOnly(2025, 1, 10), null, 100m, null, null, FiatCurrency.Usd.Code,
                FixedExpenseRecordState.Empty, null),
            new DateOnly(2025, 1, 15));

        // Act
        await vm.IgnoreFixedExpenseCommand.ExecuteAsync(entry);

        // Assert
        await _fixedExpenseRecordService.Received(1)
            .IgnoreFixedExpenseAsync(Arg.Any<TransactionFixedExpenseReference>());
    }

    [Test]
    public async Task MarkFixedExpenseAsPaidCommand_Should_Call_Service()
    {
        // Arrange
        var vm = CreateViewModel();
        var entry = new FixedExpensesEntryViewModel(
            new FixedExpenseProviderEntry(new FixedExpenseId().Value, "Test", new CategoryId(),
                new DateOnly(2025, 1, 10), null, 100m, null, null, FiatCurrency.Usd.Code,
                FixedExpenseRecordState.Empty, null),
            new DateOnly(2025, 1, 15));

        // Act
        await vm.MarkFixedExpenseAsPaidCommand.ExecuteAsync(entry);

        // Assert
        await _fixedExpenseRecordService.Received(1)
            .MarkFixedExpenseAsPaidAsync(Arg.Any<TransactionFixedExpenseReference>());
    }

    [Test]
    public async Task UndoIgnoreFixedExpenseCommand_Should_Call_Service()
    {
        // Arrange
        var vm = CreateViewModel();
        var entry = new FixedExpensesEntryViewModel(
            new FixedExpenseProviderEntry(new FixedExpenseId().Value, "Test", new CategoryId(),
                new DateOnly(2025, 1, 10), null, 100m, null, null, FiatCurrency.Usd.Code,
                FixedExpenseRecordState.Ignored, null),
            new DateOnly(2025, 1, 15));

        // Act
        await vm.UndoIgnoreFixedExpenseCommand.ExecuteAsync(entry);

        // Assert
        await _fixedExpenseRecordService.Received(1)
            .UndoIgnoreFixedExpenseAsync(Arg.Any<TransactionFixedExpenseReference>());
    }

    [Test]
    public async Task UnmarkFixedExpenseAsPaidCommand_Should_Call_Service()
    {
        // Arrange
        var vm = CreateViewModel();
        var entry = new FixedExpensesEntryViewModel(
            new FixedExpenseProviderEntry(new FixedExpenseId().Value, "Test", new CategoryId(),
                new DateOnly(2025, 1, 10), null, 100m, null, null, FiatCurrency.Usd.Code,
                FixedExpenseRecordState.ManuallyPaid, null),
            new DateOnly(2025, 1, 15));

        // Act
        await vm.UnmarkFixedExpenseAsPaidCommand.ExecuteAsync(entry);

        // Assert
        await _fixedExpenseRecordService.Received(1)
            .UnmarkFixedExpenseAsPaidAsync(Arg.Any<TransactionFixedExpenseReference>());
    }

    #endregion

    #region CanExecute Tests

    [Test]
    public void CanIgnoreFixedExpense_Should_Return_True_For_Empty_State()
    {
        // Arrange
        var vm = CreateViewModel();
        var entry = new FixedExpensesEntryViewModel(
            new FixedExpenseProviderEntry(new FixedExpenseId().Value, "Test", new CategoryId(),
                new DateOnly(2025, 1, 10), null, 100m, null, null, FiatCurrency.Usd.Code,
                FixedExpenseRecordState.Empty, null),
            new DateOnly(2025, 1, 15));

        // Act
        var result = vm.CanIgnoreFixedExpense(entry);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void CanIgnoreFixedExpense_Should_Return_False_For_Paid_State()
    {
        // Arrange
        var vm = CreateViewModel();
        var entry = new FixedExpensesEntryViewModel(
            new FixedExpenseProviderEntry(new FixedExpenseId().Value, "Test", new CategoryId(),
                new DateOnly(2025, 1, 10), null, 100m, null, null, FiatCurrency.Usd.Code,
                FixedExpenseRecordState.Paid, new TransactionId()),
            new DateOnly(2025, 1, 15));

        // Act
        var result = vm.CanIgnoreFixedExpense(entry);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void CanMarkFixedExpenseAsPaid_Should_Return_True_For_Empty_Or_Ignored_State()
    {
        // Arrange
        var vm = CreateViewModel();
        var emptyEntry = new FixedExpensesEntryViewModel(
            new FixedExpenseProviderEntry(new FixedExpenseId().Value, "Test", new CategoryId(),
                new DateOnly(2025, 1, 10), null, 100m, null, null, FiatCurrency.Usd.Code,
                FixedExpenseRecordState.Empty, null),
            new DateOnly(2025, 1, 15));
        var ignoredEntry = new FixedExpensesEntryViewModel(
            new FixedExpenseProviderEntry(new FixedExpenseId().Value, "Test", new CategoryId(),
                new DateOnly(2025, 1, 10), null, 100m, null, null, FiatCurrency.Usd.Code,
                FixedExpenseRecordState.Ignored, null),
            new DateOnly(2025, 1, 15));

        // Act & Assert
        Assert.That(vm.CanMarkFixedExpenseAsPaid(emptyEntry), Is.True);
        Assert.That(vm.CanMarkFixedExpenseAsPaid(ignoredEntry), Is.True);
    }

    [Test]
    public void CanUndoIgnoreFixedExpense_Should_Return_True_Only_For_Ignored_State()
    {
        // Arrange
        var vm = CreateViewModel();
        var ignoredEntry = new FixedExpensesEntryViewModel(
            new FixedExpenseProviderEntry(new FixedExpenseId().Value, "Test", new CategoryId(),
                new DateOnly(2025, 1, 10), null, 100m, null, null, FiatCurrency.Usd.Code,
                FixedExpenseRecordState.Ignored, null),
            new DateOnly(2025, 1, 15));
        var emptyEntry = new FixedExpensesEntryViewModel(
            new FixedExpenseProviderEntry(new FixedExpenseId().Value, "Test", new CategoryId(),
                new DateOnly(2025, 1, 10), null, 100m, null, null, FiatCurrency.Usd.Code,
                FixedExpenseRecordState.Empty, null),
            new DateOnly(2025, 1, 15));

        // Act & Assert
        Assert.That(vm.CanUndoIgnoreFixedExpense(ignoredEntry), Is.True);
        Assert.That(vm.CanUndoIgnoreFixedExpense(emptyEntry), Is.False);
    }

    [Test]
    public void CanUnmarkFixedExpenseAsPaid_Should_Return_True_Only_For_ManuallyPaid_State()
    {
        // Arrange
        var vm = CreateViewModel();
        var manuallyPaidEntry = new FixedExpensesEntryViewModel(
            new FixedExpenseProviderEntry(new FixedExpenseId().Value, "Test", new CategoryId(),
                new DateOnly(2025, 1, 10), null, 100m, null, null, FiatCurrency.Usd.Code,
                FixedExpenseRecordState.ManuallyPaid, null),
            new DateOnly(2025, 1, 15));
        var paidEntry = new FixedExpensesEntryViewModel(
            new FixedExpenseProviderEntry(new FixedExpenseId().Value, "Test", new CategoryId(),
                new DateOnly(2025, 1, 10), null, 100m, null, null, FiatCurrency.Usd.Code,
                FixedExpenseRecordState.Paid, new TransactionId()),
            new DateOnly(2025, 1, 15));

        // Act & Assert
        Assert.That(vm.CanUnmarkFixedExpenseAsPaid(manuallyPaidEntry), Is.True);
        Assert.That(vm.CanUnmarkFixedExpenseAsPaid(paidEntry), Is.False);
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
        WeakReferenceMessenger.Default.Send(new TransactionListChanged());
        WeakReferenceMessenger.Default.Send(new FilterDateRangeChanged());
        Assert.Pass("No exception thrown after dispose");
    }

    #endregion
}
