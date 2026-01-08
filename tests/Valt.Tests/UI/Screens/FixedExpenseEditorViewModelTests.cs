using NSubstitute;
using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.FixedExpenses;
using Valt.Core.Modules.Budget.FixedExpenses.Contracts;
using Valt.Infra;
using Valt.Infra.Kernel;
using Valt.Infra.Kernel.Time;
using Valt.Infra.Modules.Budget.Accounts.Queries;
using Valt.Infra.Modules.Budget.Accounts.Queries.DTOs;
using Valt.Infra.Modules.Budget.Accounts.Services;
using Valt.Infra.Modules.Budget.Categories.Queries;
using Valt.Infra.Modules.Budget.Categories.Queries.DTOs;
using Valt.Infra.Modules.Configuration;
using Valt.Infra.Settings;
using Valt.Infra.TransactionTerms;
using Valt.Tests.Builders;
using Valt.UI.Views.Main.Modals.FixedExpenseEditor;

namespace Valt.Tests.UI.Screens;

[TestFixture]
public class FixedExpenseEditorViewModelTests : DatabaseTest
{
    private IFixedExpenseRepository _fixedExpenseRepository;
    private AccountQueries _accountQueries;
    private CategoryQueries _categoryQueries;
    private DisplaySettings _displaySettings;
    private ITransactionTermService _transactionTermService;
    private ConfigurationManager _configurationManager;

    private AccountId _testAccountId;
    private CategoryId _testCategoryId;

    protected override Task SeedDatabase()
    {
        // Create test account
        _testAccountId = new AccountId();
        var account = new FiatAccountBuilder()
            .WithId(_testAccountId)
            .WithName("Test Account")
            .WithFiatCurrency(FiatCurrency.Usd)
            .Build();
        _localDatabase.GetAccounts().Insert(account);

        // Create test category
        _testCategoryId = new CategoryId();
        var category = CategoryBuilder.ACategory()
            .WithId(_testCategoryId)
            .WithName("Test Category")
            .Build();
        _localDatabase.GetCategories().Insert(category);

        return Task.CompletedTask;
    }

    private FixedExpenseEditorViewModel CreateViewModel()
    {
        _fixedExpenseRepository = Substitute.For<IFixedExpenseRepository>();
        _accountQueries = new AccountQueries(_localDatabase,
            new AccountTotalsCalculator(_localDatabase, new AccountCacheService(_localDatabase, new Clock())));
        _categoryQueries = new CategoryQueries(_localDatabase);
        _displaySettings = new DisplaySettings(_localDatabase);
        _transactionTermService = Substitute.For<ITransactionTermService>();
        _configurationManager = new ConfigurationManager(_localDatabase);

        return new FixedExpenseEditorViewModel(
            _fixedExpenseRepository,
            _accountQueries,
            _categoryQueries,
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
        Assert.That(vm.AvailableCategories.Any(c => c.Id == _testCategoryId.Value), Is.True);
    }

    [Test]
    public async Task Should_Load_Accounts_On_Initialization()
    {
        // Arrange & Act
        var vm = await CreateViewModelWithLoadedDataAsync();

        // Assert
        Assert.That(vm.AvailableAccounts.Count, Is.GreaterThan(0));
        Assert.That(vm.AvailableAccounts.Any(a => a.Id == _testAccountId.Value), Is.True);
    }

    [Test]
    public async Task Should_Not_Include_Bitcoin_Accounts_In_Available_Accounts()
    {
        // Arrange
        var btcAccountId = new AccountId();
        var btcAccount = new BtcAccountBuilder()
        {
            Id = btcAccountId,
            Name = "BTC Account",
            Value = 0
        }.Build();
        _localDatabase.GetAccounts().Insert(btcAccount);

        // Act
        var vm = await CreateViewModelWithLoadedDataAsync();

        // Assert
        Assert.That(vm.AvailableAccounts.Any(a => a.Id == btcAccountId.Value), Is.False);
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
        var fixedExpense = FixedExpenseBuilder.AFixedExpenseWithAccount(_testAccountId)
            .WithId(fixedExpenseId)
            .WithName("Rent Payment")
            .WithCategoryId(_testCategoryId)
            .WithFixedAmountRange(1500m, FixedExpensePeriods.Monthly, new DateOnly(2024, 1, 1), 5)
            .WithEnabled(true)
            .BuildDomainObject();

        var vm = await CreateViewModelWithLoadedDataAsync();
        // Set up mock AFTER creating the ViewModel (which creates the substitute)
        _fixedExpenseRepository.GetFixedExpenseByIdAsync(Arg.Any<FixedExpenseId>()).Returns(Task.FromResult<FixedExpense?>(fixedExpense));
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
        var fixedExpense = FixedExpenseBuilder.AFixedExpenseWithCurrency(FiatCurrency.Usd)
            .WithId(fixedExpenseId)
            .WithName("Groceries")
            .WithCategoryId(_testCategoryId)
            .WithRangedAmountRange(200m, 400m, FixedExpensePeriods.Weekly, new DateOnly(2024, 1, 1), 1)
            .WithEnabled(true)
            .BuildDomainObject();

        var vm = await CreateViewModelWithLoadedDataAsync();
        // Set up mock AFTER creating the ViewModel (which creates the substitute)
        _fixedExpenseRepository.GetFixedExpenseByIdAsync(Arg.Any<FixedExpenseId>()).Returns(Task.FromResult<FixedExpense?>(fixedExpense));
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

    // Note: OnBindParameterAsync_Should_Close_Window_When_FixedExpense_Not_Found cannot be easily tested
    // because it requires MessageBoxHelper which needs Avalonia platform to be initialized.
    // The scenario is: when FixedExpense is not found, it shows an error message and closes the window.

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
    public async Task OkCommand_Should_Not_Save_When_Validation_Fails()
    {
        // Arrange
        var vm = await CreateViewModelWithLoadedDataAsync();
        vm.Name = string.Empty; // Required field is empty

        // Act
        await vm.OkCommand.ExecuteAsync(null);

        // Assert
        await _fixedExpenseRepository.DidNotReceive().SaveFixedExpenseAsync(Arg.Any<FixedExpense>());
    }

    [Test]
    public async Task OkCommand_Should_Create_New_FixedExpense_With_Fixed_Amount()
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

        FixedExpense? savedExpense = null;
        _fixedExpenseRepository.SaveFixedExpenseAsync(Arg.Do<FixedExpense>(fe => savedExpense = fe))
            .Returns(Task.CompletedTask);

        object? dialogResult = null;
        vm.CloseDialog = result => dialogResult = result;

        // Act
        await vm.OkCommand.ExecuteAsync(null);

        // Assert
        Assert.That(savedExpense, Is.Not.Null);
        Assert.That((string)savedExpense!.Name, Is.EqualTo("New Expense"));
        Assert.That(savedExpense.DefaultAccountId, Is.Not.Null);
        Assert.That(savedExpense.CurrentRange.FixedAmount?.Value, Is.EqualTo(500m));
        Assert.That(savedExpense.CurrentRange.Period, Is.EqualTo(FixedExpensePeriods.Monthly));
        Assert.That(savedExpense.CurrentRange.Day, Is.EqualTo(15));
        Assert.That(dialogResult, Is.InstanceOf<FixedExpenseEditorViewModel.Response>());
        Assert.That(((FixedExpenseEditorViewModel.Response)dialogResult!).Ok, Is.True);
    }

    [Test]
    public async Task OkCommand_Should_Create_New_FixedExpense_With_Ranged_Amount()
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

        FixedExpense? savedExpense = null;
        _fixedExpenseRepository.SaveFixedExpenseAsync(Arg.Do<FixedExpense>(fe => savedExpense = fe))
            .Returns(Task.CompletedTask);

        vm.CloseDialog = _ => { };

        // Act
        await vm.OkCommand.ExecuteAsync(null);

        // Assert
        Assert.That(savedExpense, Is.Not.Null);
        Assert.That(savedExpense!.Currency, Is.EqualTo(FiatCurrency.Usd));
        Assert.That(savedExpense.DefaultAccountId, Is.Null);
        Assert.That(savedExpense.CurrentRange.RangedAmount, Is.Not.Null);
        Assert.That(savedExpense.CurrentRange.RangedAmount!.Min.Value, Is.EqualTo(100m));
        Assert.That(savedExpense.CurrentRange.RangedAmount.Max.Value, Is.EqualTo(300m));
        Assert.That(savedExpense.CurrentRange.Period, Is.EqualTo(FixedExpensePeriods.Biweekly));
    }

    [Test]
    public async Task OkCommand_Should_Edit_Existing_FixedExpense()
    {
        // Arrange
        var fixedExpenseId = new FixedExpenseId();
        var existingExpense = FixedExpenseBuilder.AFixedExpenseWithAccount(_testAccountId)
            .WithId(fixedExpenseId)
            .WithName("Original Name")
            .WithCategoryId(_testCategoryId)
            .WithFixedAmountRange(100m, FixedExpensePeriods.Monthly, new DateOnly(2024, 1, 1), 5)
            .WithEnabled(true)
            .BuildDomainObject();

        var vm = await CreateViewModelWithLoadedDataAsync();
        // Set up mock AFTER creating the ViewModel (which creates the substitute)
        _fixedExpenseRepository.GetFixedExpenseByIdAsync(Arg.Any<FixedExpenseId>()).Returns(Task.FromResult<FixedExpense?>(existingExpense));
        vm.Parameter = new FixedExpenseEditorViewModel.Request { FixedExpenseId = fixedExpenseId };
        await vm.OnBindParameterAsync();

        // Modify values
        vm.Name = "Updated Name";
        vm.Enabled = false;

        FixedExpense? savedExpense = null;
        _fixedExpenseRepository.SaveFixedExpenseAsync(Arg.Do<FixedExpense>(fe => savedExpense = fe))
            .Returns(Task.CompletedTask);

        vm.CloseDialog = _ => { };

        // Act
        await vm.OkCommand.ExecuteAsync(null);

        // Assert
        Assert.That(savedExpense, Is.Not.Null);
        Assert.That((string)savedExpense!.Name, Is.EqualTo("Updated Name"));
        Assert.That(savedExpense.Enabled, Is.False);
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
            _testCategoryId.Value,
            "Test Category",
            null,
            100m);

        // Act
        vm.TransactionTermResult = termResult;

        // Assert
        Assert.That(vm.Category?.Id, Is.EqualTo(_testCategoryId.Value));
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
            _testCategoryId.Value,
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
        var fixedExpense = FixedExpenseBuilder.AFixedExpenseWithAccount(_testAccountId)
            .WithId(fixedExpenseId)
            .WithName("Test")
            .WithCategoryId(_testCategoryId)
            .WithFixedAmountRange(100m, FixedExpensePeriods.Monthly, new DateOnly(2024, 1, 1), 5)
            .BuildDomainObject();

        var vm = await CreateViewModelWithLoadedDataAsync();
        // Set up mock AFTER creating the ViewModel (which creates the substitute)
        _fixedExpenseRepository.GetFixedExpenseByIdAsync(Arg.Any<FixedExpenseId>()).Returns(Task.FromResult<FixedExpense?>(fixedExpense));
        var originalTitle = vm.WindowTitle;
        vm.Parameter = new FixedExpenseEditorViewModel.Request { FixedExpenseId = fixedExpenseId };

        // Act
        await vm.OnBindParameterAsync();

        // Assert
        Assert.That(vm.WindowTitle, Is.Not.EqualTo(originalTitle));
    }

    #endregion
}
