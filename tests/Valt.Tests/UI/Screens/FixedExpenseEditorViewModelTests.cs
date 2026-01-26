using System.Drawing;
using NSubstitute;
using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.Accounts.DTOs;
using Valt.App.Modules.Budget.Accounts.Queries.GetAccounts;
using Valt.App.Modules.Budget.Categories.DTOs;
using Valt.App.Modules.Budget.Categories.Queries.GetCategories;
using Valt.App.Modules.Budget.FixedExpenses.Commands.CreateFixedExpense;
using Valt.App.Modules.Budget.FixedExpenses.Commands.EditFixedExpense;
using Valt.App.Modules.Budget.FixedExpenses.DTOs;
using Valt.App.Modules.Budget.FixedExpenses.Queries.GetFixedExpense;
using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.FixedExpenses;
using Valt.Infra;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel;
using Valt.Infra.Modules.Configuration;
using Valt.Infra.Settings;
using Valt.Infra.TransactionTerms;
using Valt.UI.Views.Main.Modals.FixedExpenseEditor;

namespace Valt.Tests.UI.Screens;

[TestFixture]
public class FixedExpenseEditorViewModelTests
{
    private ICommandDispatcher _commandDispatcher = null!;
    private IQueryDispatcher _queryDispatcher = null!;
    private DisplaySettings _displaySettings = null!;
    private ITransactionTermService _transactionTermService = null!;
    private IConfigurationManager _configurationManager = null!;

    private string _testAccountId = null!;
    private string _testCategoryId = null!;
    private List<AccountDTO> _testAccounts = null!;
    private List<CategoryDTO> _testCategories = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        IdGenerator.Configure(new LiteDbIdProvider());
    }

    [SetUp]
    public void SetUp()
    {
        _testAccountId = new AccountId().Value;
        _testCategoryId = new CategoryId().Value;

        _testAccounts = new List<AccountDTO>
        {
            new AccountDTO(
                Id: _testAccountId,
                Type: "fiat",
                Name: "Test Account",
                CurrencyNickname: "",
                Visible: true,
                IconId: null,
                Unicode: '\0',
                Color: Color.Black,
                Currency: FiatCurrency.Usd.Code,
                IsBtcAccount: false,
                InitialAmountFiat: null,
                InitialAmountSats: null,
                GroupId: null)
        };

        _testCategories = new List<CategoryDTO>
        {
            new()
            {
                Id = _testCategoryId,
                Name = "Test Category",
                SimpleName = "Test Category",
                Unicode = '\0',
                Color = Color.Black
            }
        };

        _commandDispatcher = Substitute.For<ICommandDispatcher>();
        _queryDispatcher = Substitute.For<IQueryDispatcher>();
        var localDatabase = Substitute.For<ILocalDatabase>();
        _displaySettings = new DisplaySettings(localDatabase);
        _transactionTermService = Substitute.For<ITransactionTermService>();
        _configurationManager = Substitute.For<IConfigurationManager>();

        // Setup default query returns
        _queryDispatcher.DispatchAsync(Arg.Any<GetAccountsQuery>(), Arg.Any<CancellationToken>())
            .Returns(_testAccounts);

        _queryDispatcher.DispatchAsync(Arg.Any<GetCategoriesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new CategoriesDTO(_testCategories));

        // Setup default command returns
        _commandDispatcher.DispatchAsync(Arg.Any<CreateFixedExpenseCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<CreateFixedExpenseResult>.Success(new CreateFixedExpenseResult("new-id")));

        _commandDispatcher.DispatchAsync(Arg.Any<EditFixedExpenseCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<EditFixedExpenseResult>.Success(new EditFixedExpenseResult()));

        // Setup configuration manager
        _configurationManager.GetAvailableFiatCurrencies()
            .Returns(FiatCurrency.GetAll().Select(x => x.Code).ToList());
    }

    private static FixedExpenseDTO CreateFixedExpenseDto(
        string id,
        string name,
        string categoryId,
        string? defaultAccountId,
        string? currency,
        bool enabled,
        int periodId,
        int day,
        DateOnly periodStart,
        decimal? fixedAmount,
        decimal? rangedAmountMin = null,
        decimal? rangedAmountMax = null)
    {
        var ranges = new List<FixedExpenseRangeDTO>
        {
            new()
            {
                PeriodStart = periodStart,
                PeriodId = periodId,
                PeriodDescription = ((FixedExpensePeriods)periodId).ToString(),
                Day = day,
                FixedAmount = fixedAmount,
                RangedAmountMin = rangedAmountMin,
                RangedAmountMax = rangedAmountMax
            }
        };

        return new FixedExpenseDTO
        {
            Id = id,
            Name = name,
            CategoryId = categoryId,
            DefaultAccountId = defaultAccountId,
            Currency = currency,
            DisplayCurrency = currency ?? "USD",
            Enabled = enabled,
            Ranges = ranges
        };
    }

    private FixedExpenseEditorViewModel CreateViewModel()
    {
        return new FixedExpenseEditorViewModel(
            _commandDispatcher,
            _queryDispatcher,
            _displaySettings,
            _transactionTermService,
            _configurationManager);
    }

    private async Task<FixedExpenseEditorViewModel> CreateViewModelWithLoadedDataAsync()
    {
        var vm = CreateViewModel();
        // Wait for initialization to complete
        await Task.Delay(100);
        // Set up required delegates to avoid null reference exceptions
        vm.GetWindow = () => null!;
        vm.CloseWindow = () => { };
        vm.CloseDialog = _ => { };
        return vm;
    }

    #region Initialization Tests

    [Test]
    public async Task Should_Initialize_With_Default_Values()
    {
        // Arrange & Act
        var vm = await CreateViewModelWithLoadedDataAsync();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(vm.Name, Is.EqualTo(string.Empty));
            Assert.That(vm.IsAttachedToDefaultAccount, Is.True);
            Assert.That(vm.IsAttachedToCurrency, Is.False);
            Assert.That(vm.IsFixedAmount, Is.True);
            Assert.That(vm.IsVariableAmount, Is.False);
            Assert.That(vm.Period, Is.EqualTo(FixedExpensePeriods.Monthly.ToString()));
            Assert.That(vm.Day, Is.EqualTo(5));
            Assert.That(vm.Enabled, Is.True);
            Assert.That(vm.IsEditing, Is.False);
            Assert.That(vm.FixedExpenseId, Is.Null);
        });
    }

    [Test]
    public async Task Should_Load_Categories_On_Initialization()
    {
        // Arrange & Act
        var vm = await CreateViewModelWithLoadedDataAsync();

        // Assert
        Assert.That(vm.AvailableCategories.Count, Is.GreaterThan(0));
        Assert.That(vm.AvailableCategories.Any(c => c.Id == _testCategoryId), Is.True);
    }

    [Test]
    public async Task Should_Load_Accounts_On_Initialization()
    {
        // Arrange & Act
        var vm = await CreateViewModelWithLoadedDataAsync();

        // Assert
        Assert.That(vm.AvailableAccounts.Count, Is.GreaterThan(0));
        Assert.That(vm.AvailableAccounts.Any(a => a.Id == _testAccountId), Is.True);
    }

    [Test]
    public async Task Should_Not_Include_Bitcoin_Accounts_In_Available_Accounts()
    {
        // Arrange - Add a BTC account to the test data
        var btcAccountId = new AccountId().Value;
        _testAccounts.Add(new AccountDTO(
            Id: btcAccountId,
            Type: "btc",
            Name: "BTC Account",
            CurrencyNickname: "",
            Visible: true,
            IconId: null,
            Unicode: '\0',
            Color: Color.Black,
            Currency: null,
            IsBtcAccount: true,
            InitialAmountFiat: null,
            InitialAmountSats: null,
            GroupId: null));

        // Act
        var vm = await CreateViewModelWithLoadedDataAsync();

        // Assert
        Assert.That(vm.AvailableAccounts.Any(a => a.Id == btcAccountId), Is.False);
    }

    #endregion

    #region Computed Property Tests

    [Test]
    public async Task IsDefaultAccountSelectorVisible_Should_Return_True_When_AttachedToDefaultAccount()
    {
        // Arrange
        var vm = await CreateViewModelWithLoadedDataAsync();

        // Act
        vm.IsAttachedToDefaultAccount = true;
        vm.IsAttachedToCurrency = false;

        // Assert
        Assert.That(vm.IsDefaultAccountSelectorVisible, Is.True);
    }

    [Test]
    public async Task IsDefaultAccountSelectorVisible_Should_Return_False_When_AttachedToCurrency()
    {
        // Arrange
        var vm = await CreateViewModelWithLoadedDataAsync();

        // Act
        vm.IsAttachedToDefaultAccount = false;
        vm.IsAttachedToCurrency = true;

        // Assert
        Assert.That(vm.IsDefaultAccountSelectorVisible, Is.False);
    }

    [Test]
    public async Task IsFixedSelectorVisible_Should_Return_True_When_FixedAmount()
    {
        // Arrange
        var vm = await CreateViewModelWithLoadedDataAsync();

        // Act
        vm.IsFixedAmount = true;
        vm.IsVariableAmount = false;

        // Assert
        Assert.That(vm.IsFixedSelectorVisible, Is.True);
    }

    [Test]
    public async Task IsFixedSelectorVisible_Should_Return_False_When_VariableAmount()
    {
        // Arrange
        var vm = await CreateViewModelWithLoadedDataAsync();

        // Act
        vm.IsFixedAmount = false;
        vm.IsVariableAmount = true;

        // Assert
        Assert.That(vm.IsFixedSelectorVisible, Is.False);
    }

    [Test]
    public async Task DisplayDaySelector_Should_Return_True_For_Monthly_Period()
    {
        // Arrange
        var vm = await CreateViewModelWithLoadedDataAsync();

        // Act
        vm.Period = FixedExpensePeriods.Monthly.ToString();

        // Assert
        Assert.That(vm.DisplayDaySelector, Is.True);
    }

    [Test]
    public async Task DisplayDaySelector_Should_Return_True_For_Yearly_Period()
    {
        // Arrange
        var vm = await CreateViewModelWithLoadedDataAsync();

        // Act
        vm.Period = FixedExpensePeriods.Yearly.ToString();

        // Assert
        Assert.That(vm.DisplayDaySelector, Is.True);
    }

    [Test]
    public async Task DisplayDaySelector_Should_Return_False_For_Weekly_Period()
    {
        // Arrange
        var vm = await CreateViewModelWithLoadedDataAsync();

        // Act
        vm.Period = FixedExpensePeriods.Weekly.ToString();

        // Assert
        Assert.That(vm.DisplayDaySelector, Is.False);
    }

    [Test]
    public async Task DisplayDaySelector_Should_Return_False_For_Biweekly_Period()
    {
        // Arrange
        var vm = await CreateViewModelWithLoadedDataAsync();

        // Act
        vm.Period = FixedExpensePeriods.Biweekly.ToString();

        // Assert
        Assert.That(vm.DisplayDaySelector, Is.False);
    }

    [Test]
    public async Task IsEditing_Should_Return_True_When_FixedExpenseId_Is_Set()
    {
        // Arrange
        var vm = await CreateViewModelWithLoadedDataAsync();

        // Act
        vm.FixedExpenseId = new FixedExpenseId();

        // Assert
        Assert.That(vm.IsEditing, Is.True);
    }

    [Test]
    public async Task IsRecurrenceInfoLocked_Should_Return_True_When_Editing_And_Not_In_ChangeRecurrenceMode()
    {
        // Arrange
        var vm = await CreateViewModelWithLoadedDataAsync();
        vm.FixedExpenseId = new FixedExpenseId();

        // Assert
        Assert.That(vm.IsRecurrenceInfoLocked, Is.True);
    }

    [Test]
    public async Task IsRecurrenceInfoLocked_Should_Return_False_When_Not_Editing()
    {
        // Arrange
        var vm = await CreateViewModelWithLoadedDataAsync();

        // Assert
        Assert.That(vm.IsRecurrenceInfoLocked, Is.False);
    }

    [Test]
    public async Task ShowChangeRecurrenceButton_Should_Return_True_When_Editing_And_Not_In_ChangeRecurrenceMode()
    {
        // Arrange
        var vm = await CreateViewModelWithLoadedDataAsync();
        vm.FixedExpenseId = new FixedExpenseId();

        // Assert
        Assert.That(vm.ShowChangeRecurrenceButton, Is.True);
    }

    [Test]
    public async Task ShowCancelChangeRecurrenceButton_Should_Return_True_When_Editing_And_In_ChangeRecurrenceMode()
    {
        // Arrange
        var vm = await CreateViewModelWithLoadedDataAsync();
        vm.FixedExpenseId = new FixedExpenseId();

        // Act
        vm.EnterChangeRecurrenceModeCommand.Execute(null);

        // Assert
        Assert.That(vm.ShowCancelChangeRecurrenceButton, Is.True);
        Assert.That(vm.ShowChangeRecurrenceButton, Is.False);
    }

    #endregion

    #region Recurrence Mode Tests

    [Test]
    public async Task EnterChangeRecurrenceMode_Should_Store_Original_Values_And_Set_Flag()
    {
        // Arrange
        var vm = await CreateViewModelWithLoadedDataAsync();
        vm.FixedExpenseId = new FixedExpenseId();
        vm.Period = FixedExpensePeriods.Weekly.ToString();
        vm.Day = 3;
        vm.PeriodStart = new DateTime(2024, 1, 1);

        // Act
        vm.EnterChangeRecurrenceModeCommand.Execute(null);

        // Assert
        Assert.That(vm.IsInChangeRecurrenceMode, Is.True);
    }

    [Test]
    public async Task EnterChangeRecurrenceMode_Should_Set_Minimum_Date_When_Last_Record_Exists()
    {
        // Arrange
        var vm = await CreateViewModelWithLoadedDataAsync();
        vm.FixedExpenseId = new FixedExpenseId();
        vm.LastFixedExpenseRecordReferenceDate = new DateOnly(2024, 6, 15);
        vm.Period = FixedExpensePeriods.Monthly.ToString();
        vm.Day = 5;
        vm.PeriodStart = new DateTime(2024, 1, 1);

        // Act
        vm.EnterChangeRecurrenceModeCommand.Execute(null);

        // Assert
        Assert.That(vm.PeriodStart, Is.EqualTo(new DateOnly(2024, 6, 16).ToValtDateTime()));
    }

    [Test]
    public async Task CancelChangeRecurrenceMode_Should_Restore_Original_Values()
    {
        // Arrange
        var vm = await CreateViewModelWithLoadedDataAsync();
        vm.FixedExpenseId = new FixedExpenseId();
        vm.Period = FixedExpensePeriods.Weekly.ToString();
        vm.Day = 3;
        var originalDate = new DateTime(2024, 1, 1);
        vm.PeriodStart = originalDate;

        vm.EnterChangeRecurrenceModeCommand.Execute(null);

        // Change values
        vm.Period = FixedExpensePeriods.Monthly.ToString();
        vm.Day = 15;
        vm.PeriodStart = new DateTime(2024, 6, 1);

        // Act
        vm.CancelChangeRecurrenceModeCommand.Execute(null);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(vm.IsInChangeRecurrenceMode, Is.False);
            Assert.That(vm.Period, Is.EqualTo(FixedExpensePeriods.Weekly.ToString()));
            Assert.That(vm.Day, Is.EqualTo(3));
            Assert.That(vm.PeriodStart, Is.EqualTo(originalDate));
        });
    }

    #endregion

    #region OnBindParameterAsync Tests

    [Test]
    public async Task OnBindParameterAsync_Should_Do_Nothing_When_Parameter_Is_Null()
    {
        // Arrange
        var vm = await CreateViewModelWithLoadedDataAsync();
        vm.Parameter = null;

        // Act
        await vm.OnBindParameterAsync();

        // Assert
        Assert.That(vm.IsEditing, Is.False);
    }

    [Test]
    public async Task OnBindParameterAsync_Should_Load_FixedExpense_When_Valid_Id_Provided()
    {
        // Arrange
        var fixedExpenseId = new FixedExpenseId();
        var fixedExpenseDto = CreateFixedExpenseDto(
            id: fixedExpenseId.Value,
            name: "Rent Payment",
            categoryId: _testCategoryId,
            defaultAccountId: _testAccountId,
            currency: null,
            enabled: true,
            periodId: (int)FixedExpensePeriods.Monthly,
            day: 5,
            periodStart: new DateOnly(2024, 1, 1),
            fixedAmount: 1500m);

        _queryDispatcher.DispatchAsync(Arg.Any<GetFixedExpenseQuery>(), Arg.Any<CancellationToken>())
            .Returns(fixedExpenseDto);

        var vm = await CreateViewModelWithLoadedDataAsync();
        vm.Parameter = new FixedExpenseEditorViewModel.Request { FixedExpenseId = fixedExpenseId };

        // Act
        await vm.OnBindParameterAsync();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(vm.IsEditing, Is.True);
            Assert.That(vm.FixedExpenseId, Is.EqualTo(fixedExpenseId));
            Assert.That(vm.Name, Is.EqualTo("Rent Payment"));
            Assert.That(vm.IsAttachedToDefaultAccount, Is.True);
            Assert.That(vm.IsFixedAmount, Is.True);
            Assert.That(vm.Period, Is.EqualTo(FixedExpensePeriods.Monthly.ToString()));
            Assert.That(vm.Day, Is.EqualTo(5));
            Assert.That(vm.Enabled, Is.True);
        });
    }

    [Test]
    public async Task OnBindParameterAsync_Should_Load_Ranged_Amount_FixedExpense()
    {
        // Arrange
        var fixedExpenseId = new FixedExpenseId();
        var fixedExpenseDto = CreateFixedExpenseDto(
            id: fixedExpenseId.Value,
            name: "Groceries",
            categoryId: _testCategoryId,
            defaultAccountId: null,
            currency: FiatCurrency.Usd.Code,
            enabled: true,
            periodId: (int)FixedExpensePeriods.Weekly,
            day: 1,
            periodStart: new DateOnly(2024, 1, 1),
            fixedAmount: null,
            rangedAmountMin: 200m,
            rangedAmountMax: 400m);

        _queryDispatcher.DispatchAsync(Arg.Any<GetFixedExpenseQuery>(), Arg.Any<CancellationToken>())
            .Returns(fixedExpenseDto);

        var vm = await CreateViewModelWithLoadedDataAsync();
        vm.Parameter = new FixedExpenseEditorViewModel.Request { FixedExpenseId = fixedExpenseId };

        // Act
        await vm.OnBindParameterAsync();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(vm.IsVariableAmount, Is.True);
            Assert.That(vm.IsFixedAmount, Is.False);
            Assert.That(vm.IsAttachedToCurrency, Is.True);
            Assert.That(vm.IsAttachedToDefaultAccount, Is.False);
            Assert.That(vm.Currency, Is.EqualTo(FiatCurrency.Usd.Code));
            Assert.That(vm.RangedAmountMin?.Value, Is.EqualTo(200m));
            Assert.That(vm.RangedAmountMax?.Value, Is.EqualTo(400m));
            Assert.That(vm.Period, Is.EqualTo(FixedExpensePeriods.Weekly.ToString()));
        });
    }

    #endregion

    #region Cancel and Close Commands Tests

    [Test]
    public async Task CancelCommand_Should_Close_Window()
    {
        // Arrange
        var vm = await CreateViewModelWithLoadedDataAsync();
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
        var vm = await CreateViewModelWithLoadedDataAsync();
        var closeWindowCalled = false;
        vm.CloseWindow = () => closeWindowCalled = true;

        // Act
        vm.CloseCommand.Execute(null);

        // Assert
        Assert.That(closeWindowCalled, Is.True);
    }

    #endregion

    #region Ok Command Tests

    [Test]
    public async Task OkCommand_Should_Not_Dispatch_When_Validation_Fails()
    {
        // Arrange
        var vm = await CreateViewModelWithLoadedDataAsync();
        vm.Name = string.Empty; // Required field is empty

        // Act
        await vm.OkCommand.ExecuteAsync(null);

        // Assert
        await _commandDispatcher.DidNotReceive().DispatchAsync(
            Arg.Any<CreateFixedExpenseCommand>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task OkCommand_Should_Dispatch_CreateFixedExpenseCommand_With_Fixed_Amount()
    {
        // Arrange
        var vm = await CreateViewModelWithLoadedDataAsync();
        vm.Name = "New Expense";
        vm.Category = vm.AvailableCategories.First();
        vm.DefaultAccount = vm.AvailableAccounts.First();
        vm.IsAttachedToDefaultAccount = true;
        vm.IsFixedAmount = true;
        vm.FixedAmount = FiatValue.New(500m);
        vm.Period = FixedExpensePeriods.Monthly.ToString();
        vm.Day = 15;
        vm.PeriodStart = DateTime.Today;
        vm.Enabled = true;

        CreateFixedExpenseCommand? capturedCommand = null;
        _commandDispatcher.DispatchAsync(Arg.Do<CreateFixedExpenseCommand>(c => capturedCommand = c), Arg.Any<CancellationToken>())
            .Returns(Result<CreateFixedExpenseResult>.Success(new CreateFixedExpenseResult("new-id")));

        object? dialogResult = null;
        vm.CloseDialog = result => dialogResult = result;

        // Act
        await vm.OkCommand.ExecuteAsync(null);

        // Assert
        Assert.That(capturedCommand, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(capturedCommand!.Name, Is.EqualTo("New Expense"));
            Assert.That(capturedCommand.DefaultAccountId, Is.Not.Null);
            Assert.That(capturedCommand.Ranges[0].FixedAmount, Is.EqualTo(500m));
            Assert.That(capturedCommand.Ranges[0].PeriodId, Is.EqualTo((int)FixedExpensePeriods.Monthly));
            Assert.That(capturedCommand.Ranges[0].Day, Is.EqualTo(15));
        });
        Assert.That(dialogResult, Is.InstanceOf<FixedExpenseEditorViewModel.Response>());
        Assert.That(((FixedExpenseEditorViewModel.Response)dialogResult!).Ok, Is.True);
    }

    [Test]
    public async Task OkCommand_Should_Dispatch_CreateFixedExpenseCommand_With_Ranged_Amount()
    {
        // Arrange
        var vm = await CreateViewModelWithLoadedDataAsync();
        vm.Name = "Variable Expense";
        vm.Category = vm.AvailableCategories.First();
        vm.IsAttachedToCurrency = true;
        vm.IsAttachedToDefaultAccount = false;
        vm.Currency = FiatCurrency.Usd.Code;
        vm.IsFixedAmount = false;
        vm.IsVariableAmount = true;
        vm.RangedAmountMin = FiatValue.New(100m);
        vm.RangedAmountMax = FiatValue.New(300m);
        vm.Period = FixedExpensePeriods.Biweekly.ToString();
        vm.Day = 2; // Tuesday
        vm.PeriodStart = DateTime.Today;
        vm.Enabled = true;

        CreateFixedExpenseCommand? capturedCommand = null;
        _commandDispatcher.DispatchAsync(Arg.Do<CreateFixedExpenseCommand>(c => capturedCommand = c), Arg.Any<CancellationToken>())
            .Returns(Result<CreateFixedExpenseResult>.Success(new CreateFixedExpenseResult("new-id")));

        vm.CloseDialog = _ => { };

        // Act
        await vm.OkCommand.ExecuteAsync(null);

        // Assert
        Assert.That(capturedCommand, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(capturedCommand!.Currency, Is.EqualTo(FiatCurrency.Usd.Code));
            Assert.That(capturedCommand.DefaultAccountId, Is.Null);
            Assert.That(capturedCommand.Ranges[0].RangedAmountMin, Is.EqualTo(100m));
            Assert.That(capturedCommand.Ranges[0].RangedAmountMax, Is.EqualTo(300m));
            Assert.That(capturedCommand.Ranges[0].PeriodId, Is.EqualTo((int)FixedExpensePeriods.Biweekly));
        });
    }

    [Test]
    public async Task OkCommand_Should_Dispatch_EditFixedExpenseCommand_When_Editing()
    {
        // Arrange
        var fixedExpenseId = new FixedExpenseId();
        var fixedExpenseDto = CreateFixedExpenseDto(
            id: fixedExpenseId.Value,
            name: "Original Name",
            categoryId: _testCategoryId,
            defaultAccountId: _testAccountId,
            currency: null,
            enabled: true,
            periodId: (int)FixedExpensePeriods.Monthly,
            day: 5,
            periodStart: new DateOnly(2024, 1, 1),
            fixedAmount: 100m);

        _queryDispatcher.DispatchAsync(Arg.Any<GetFixedExpenseQuery>(), Arg.Any<CancellationToken>())
            .Returns(fixedExpenseDto);

        var vm = await CreateViewModelWithLoadedDataAsync();
        vm.Parameter = new FixedExpenseEditorViewModel.Request { FixedExpenseId = fixedExpenseId };
        await vm.OnBindParameterAsync();

        // Modify values
        vm.Name = "Updated Name";
        vm.Enabled = false;

        EditFixedExpenseCommand? capturedCommand = null;
        _commandDispatcher.DispatchAsync(Arg.Do<EditFixedExpenseCommand>(c => capturedCommand = c), Arg.Any<CancellationToken>())
            .Returns(Result<EditFixedExpenseResult>.Success(new EditFixedExpenseResult()));

        vm.CloseDialog = _ => { };

        // Act
        await vm.OkCommand.ExecuteAsync(null);

        // Assert
        Assert.That(capturedCommand, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(capturedCommand!.FixedExpenseId, Is.EqualTo(fixedExpenseId.Value));
            Assert.That(capturedCommand.Name, Is.EqualTo("Updated Name"));
            Assert.That(capturedCommand.Enabled, Is.False);
        });
    }

    #endregion

    #region Transaction Term Tests

    [Test]
    public async Task OnTransactionTermResultChanged_Should_Set_Category_When_Name_Matches()
    {
        // Arrange
        var vm = await CreateViewModelWithLoadedDataAsync();
        vm.Name = "Test Term";

        var termResult = new TransactionTermResult(
            "Test Term",
            _testCategoryId,
            "Test Category",
            null,
            100m);

        // Act
        vm.TransactionTermResult = termResult;

        // Assert
        Assert.That(vm.Category?.Id, Is.EqualTo(_testCategoryId));
    }

    [Test]
    public async Task OnTransactionTermResultChanged_Should_Not_Set_Category_When_Name_Does_Not_Match()
    {
        // Arrange
        var vm = await CreateViewModelWithLoadedDataAsync();
        vm.Name = "Different Name";
        var originalCategory = vm.Category;

        var termResult = new TransactionTermResult(
            "Test Term",
            _testCategoryId,
            "Test Category",
            null,
            100m);

        // Act
        vm.TransactionTermResult = termResult;

        // Assert
        Assert.That(vm.Category, Is.EqualTo(originalCategory));
    }

    [Test]
    public async Task GetTransactionTermsAsync_Should_Return_Empty_When_Term_Is_Empty()
    {
        // Arrange
        var vm = await CreateViewModelWithLoadedDataAsync();

        // Act
        var result = await vm.GetTransactionTermsAsync(string.Empty, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetTransactionTermsAsync_Should_Return_Results_From_Service()
    {
        // Arrange
        var vm = await CreateViewModelWithLoadedDataAsync();
        var expectedResults = new List<TransactionTermResult>
        {
            new("Term1", "cat1", "Category1", null, 100m),
            new("Term2", "cat2", "Category2", null, 200m)
        };

        _transactionTermService.Search("test", 5).Returns(expectedResults);

        // Act
        var result = await vm.GetTransactionTermsAsync("test", CancellationToken.None);

        // Assert
        Assert.That(result.Count(), Is.EqualTo(2));
    }

    #endregion

    #region Available Periods and Days Tests

    [Test]
    public void AvailablePeriods_Should_Contain_All_Period_Options()
    {
        // Assert
        Assert.That(FixedExpenseEditorViewModel.AvailablePeriods.Count, Is.EqualTo(4));
        Assert.That(FixedExpenseEditorViewModel.AvailablePeriods.Any(p => p.Value == FixedExpensePeriods.Weekly.ToString()), Is.True);
        Assert.That(FixedExpenseEditorViewModel.AvailablePeriods.Any(p => p.Value == FixedExpensePeriods.Biweekly.ToString()), Is.True);
        Assert.That(FixedExpenseEditorViewModel.AvailablePeriods.Any(p => p.Value == FixedExpensePeriods.Monthly.ToString()), Is.True);
        Assert.That(FixedExpenseEditorViewModel.AvailablePeriods.Any(p => p.Value == FixedExpensePeriods.Yearly.ToString()), Is.True);
    }

    [Test]
    public void AvailableDaysOfWeek_Should_Contain_All_Days()
    {
        // Assert
        Assert.That(FixedExpenseEditorViewModel.AvailableDaysOfWeek.Count, Is.EqualTo(7));
    }

    #endregion

    #region AdaptedDay Property Tests

    [Test]
    public async Task AdaptedDay_Get_Should_Return_Day_As_String()
    {
        // Arrange
        var vm = await CreateViewModelWithLoadedDataAsync();
        vm.Day = 15;

        // Assert
        Assert.That(vm.AdaptedDay, Is.EqualTo("15"));
    }

    [Test]
    public async Task AdaptedDay_Set_Should_Parse_String_To_Day()
    {
        // Arrange
        var vm = await CreateViewModelWithLoadedDataAsync();

        // Act
        vm.AdaptedDay = "20";

        // Assert
        Assert.That(vm.Day, Is.EqualTo(20));
    }

    #endregion

    #region PropertyChanged Notifications Tests

    [Test]
    public async Task Should_Notify_Dependent_Properties_When_IsAttachedToDefaultAccount_Changes()
    {
        // Arrange
        var vm = await CreateViewModelWithLoadedDataAsync();
        var changedProperties = new List<string>();
        vm.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName!);

        // Act
        vm.IsAttachedToDefaultAccount = !vm.IsAttachedToDefaultAccount;

        // Assert
        Assert.That(changedProperties, Contains.Item(nameof(vm.IsDefaultAccountSelectorVisible)));
    }

    [Test]
    public async Task Should_Notify_Dependent_Properties_When_IsFixedAmount_Changes()
    {
        // Arrange
        var vm = await CreateViewModelWithLoadedDataAsync();
        var changedProperties = new List<string>();
        vm.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName!);

        // Act
        vm.IsFixedAmount = !vm.IsFixedAmount;

        // Assert
        Assert.That(changedProperties, Contains.Item(nameof(vm.IsFixedSelectorVisible)));
    }

    [Test]
    public async Task Should_Notify_Dependent_Properties_When_Period_Changes()
    {
        // Arrange
        var vm = await CreateViewModelWithLoadedDataAsync();
        var changedProperties = new List<string>();
        vm.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName!);

        // Act
        vm.Period = FixedExpensePeriods.Weekly.ToString();

        // Assert
        Assert.That(changedProperties, Contains.Item(nameof(vm.DisplayDaySelector)));
    }

    [Test]
    public async Task Should_Notify_Dependent_Properties_When_FixedExpenseId_Changes()
    {
        // Arrange
        var vm = await CreateViewModelWithLoadedDataAsync();
        var changedProperties = new List<string>();
        vm.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName!);

        // Act
        vm.FixedExpenseId = new FixedExpenseId();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(changedProperties, Contains.Item(nameof(vm.IsEditing)));
            Assert.That(changedProperties, Contains.Item(nameof(vm.IsRecurrenceInfoLocked)));
            Assert.That(changedProperties, Contains.Item(nameof(vm.ShowChangeRecurrenceButton)));
            Assert.That(changedProperties, Contains.Item(nameof(vm.ShowCancelChangeRecurrenceButton)));
        });
    }

    [Test]
    public async Task Should_Notify_Dependent_Properties_When_IsInChangeRecurrenceMode_Changes()
    {
        // Arrange
        var vm = await CreateViewModelWithLoadedDataAsync();
        vm.FixedExpenseId = new FixedExpenseId();
        var changedProperties = new List<string>();
        vm.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName!);

        // Act
        vm.EnterChangeRecurrenceModeCommand.Execute(null);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(changedProperties, Contains.Item(nameof(vm.IsRecurrenceInfoLocked)));
            Assert.That(changedProperties, Contains.Item(nameof(vm.ShowChangeRecurrenceButton)));
            Assert.That(changedProperties, Contains.Item(nameof(vm.ShowCancelChangeRecurrenceButton)));
        });
    }

    #endregion

    #region Window Title Tests

    [Test]
    public async Task WindowTitle_Should_Be_AddTitle_By_Default()
    {
        // Arrange & Act
        var vm = await CreateViewModelWithLoadedDataAsync();

        // Assert - Window title should indicate "Add" mode
        Assert.That(vm.WindowTitle, Is.Not.Empty);
    }

    [Test]
    public async Task WindowTitle_Should_Change_When_Editing_Existing_FixedExpense()
    {
        // Arrange
        var fixedExpenseId = new FixedExpenseId();
        var fixedExpenseDto = CreateFixedExpenseDto(
            id: fixedExpenseId.Value,
            name: "Test",
            categoryId: _testCategoryId,
            defaultAccountId: _testAccountId,
            currency: null,
            enabled: true,
            periodId: (int)FixedExpensePeriods.Monthly,
            day: 5,
            periodStart: new DateOnly(2024, 1, 1),
            fixedAmount: 100m);

        _queryDispatcher.DispatchAsync(Arg.Any<GetFixedExpenseQuery>(), Arg.Any<CancellationToken>())
            .Returns(fixedExpenseDto);

        var vm = await CreateViewModelWithLoadedDataAsync();
        var originalTitle = vm.WindowTitle;
        vm.Parameter = new FixedExpenseEditorViewModel.Request { FixedExpenseId = fixedExpenseId };

        // Act
        await vm.OnBindParameterAsync();

        // Assert
        Assert.That(vm.WindowTitle, Is.Not.EqualTo(originalTitle));
    }

    #endregion
}
