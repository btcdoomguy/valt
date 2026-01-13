using NSubstitute;
using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Infra.Kernel;
using Valt.Infra.Modules.Budget.Categories.Queries;
using Valt.Infra.Modules.Budget.Categories.Queries.DTOs;
using Valt.Infra.TransactionTerms;
using Valt.Tests.Builders;
using Valt.UI.Views.Main.Modals.ChangeCategoryTransactions;

namespace Valt.Tests.UI.Screens;

[TestFixture]
public class ChangeCategoryTransactionsViewModelTests : DatabaseTest
{
    private CategoryQueries _categoryQueries;
    private ITransactionTermService _transactionTermService;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        IdGenerator.Configure(new LiteDbIdProvider());
    }

    private ChangeCategoryTransactionsViewModel CreateInstance()
    {
        _categoryQueries = new CategoryQueries(_localDatabase);
        _transactionTermService = Substitute.For<ITransactionTermService>();

        return new ChangeCategoryTransactionsViewModel(
            _transactionTermService,
            _categoryQueries);
    }

    protected override async Task SeedDatabase()
    {
        await base.SeedDatabase();

        // Seed some categories
        var category1 = CategoryBuilder.ACategory()
            .WithName("Category 1")
            .WithIcon(Icon.Empty)
            .Build();

        var category2 = CategoryBuilder.ACategory()
            .WithName("Category 2")
            .WithIcon(Icon.Empty)
            .Build();

        _localDatabase.GetCategories().Insert(category1);
        _localDatabase.GetCategories().Insert(category2);
    }

    #region Initialization Tests

    [Test]
    public async Task Should_Load_Categories_On_Initialization()
    {
        // Arrange & Act
        var viewModel = CreateInstance();

        // Wait for async initialization
        await Task.Delay(100);

        // Assert
        Assert.That(viewModel.AvailableCategories.Count, Is.EqualTo(2));
    }

    [Test]
    public void Should_Initialize_With_Empty_Name()
    {
        // Arrange & Act
        var viewModel = CreateInstance();

        // Assert
        Assert.That(viewModel.Name, Is.EqualTo(string.Empty));
    }

    [Test]
    public void Should_Initialize_With_RenameEnabled_True()
    {
        // Arrange & Act
        var viewModel = CreateInstance();

        // Assert
        Assert.That(viewModel.RenameEnabled, Is.True);
    }

    [Test]
    public void Should_Initialize_With_ChangeCategoryEnabled_False()
    {
        // Arrange & Act
        var viewModel = CreateInstance();

        // Assert
        Assert.That(viewModel.ChangeCategoryEnabled, Is.False);
    }

    [Test]
    public void Should_Initialize_With_No_SelectedCategory()
    {
        // Arrange & Act
        var viewModel = CreateInstance();

        // Assert
        Assert.That(viewModel.SelectedCategory, Is.Null);
    }

    #endregion

    #region Rename Validation Tests

    [Test]
    public async Task Should_Require_Name_When_RenameEnabled_And_Name_Empty()
    {
        // Arrange
        var viewModel = CreateInstance();
        viewModel.RenameEnabled = true;
        viewModel.Name = string.Empty;

        ChangeCategoryTransactionsViewModel.Response? response = null;
        viewModel.CloseDialog = r => response = r as ChangeCategoryTransactionsViewModel.Response;

        // Act
        await viewModel.OkCommand.ExecuteAsync(null);

        // Assert
        Assert.That(viewModel.HasErrors, Is.True);
        Assert.That(response, Is.Null);
    }

    [Test]
    public async Task Should_Not_Require_Name_When_RenameEnabled_Is_False()
    {
        // Arrange
        var viewModel = CreateInstance();
        await Task.Delay(100); // Wait for categories to load

        viewModel.RenameEnabled = false;
        viewModel.Name = string.Empty;
        viewModel.ChangeCategoryEnabled = true;
        viewModel.SelectedCategory = viewModel.AvailableCategories.First();

        ChangeCategoryTransactionsViewModel.Response? response = null;
        viewModel.CloseDialog = r => response = r as ChangeCategoryTransactionsViewModel.Response;

        // Act
        await viewModel.OkCommand.ExecuteAsync(null);

        // Assert
        Assert.That(viewModel.HasErrors, Is.False);
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.RenameEnabled, Is.False);
    }

    #endregion

    #region Category Validation Tests

    [Test]
    public async Task Should_Not_Require_Category_When_Checkbox_Not_Enabled()
    {
        // Arrange
        var viewModel = CreateInstance();
        viewModel.RenameEnabled = true;
        viewModel.Name = "New Name";
        viewModel.ChangeCategoryEnabled = false;

        ChangeCategoryTransactionsViewModel.Response? response = null;
        viewModel.CloseDialog = r => response = r as ChangeCategoryTransactionsViewModel.Response;

        // Act
        await viewModel.OkCommand.ExecuteAsync(null);

        // Assert
        Assert.That(viewModel.HasErrors, Is.False);
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.ChangeCategoryEnabled, Is.False);
    }

    [Test]
    public async Task Should_Require_Category_When_Checkbox_Is_Enabled()
    {
        // Arrange
        var viewModel = CreateInstance();
        viewModel.RenameEnabled = true;
        viewModel.Name = "New Name";
        viewModel.ChangeCategoryEnabled = true;
        viewModel.SelectedCategory = null;

        ChangeCategoryTransactionsViewModel.Response? response = null;
        viewModel.CloseDialog = r => response = r as ChangeCategoryTransactionsViewModel.Response;

        // Act
        await viewModel.OkCommand.ExecuteAsync(null);

        // Assert
        Assert.That(viewModel.HasErrors, Is.True);
        Assert.That(response, Is.Null);
    }

    [Test]
    public async Task Should_Pass_Validation_When_Category_Checkbox_Enabled_And_Category_Selected()
    {
        // Arrange
        var viewModel = CreateInstance();
        await Task.Delay(100); // Wait for categories to load

        viewModel.RenameEnabled = true;
        viewModel.Name = "New Name";
        viewModel.ChangeCategoryEnabled = true;
        viewModel.SelectedCategory = viewModel.AvailableCategories.First();

        ChangeCategoryTransactionsViewModel.Response? response = null;
        viewModel.CloseDialog = r => response = r as ChangeCategoryTransactionsViewModel.Response;

        // Act
        await viewModel.OkCommand.ExecuteAsync(null);

        // Assert
        Assert.That(viewModel.HasErrors, Is.False);
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.ChangeCategoryEnabled, Is.True);
        Assert.That(response.CategoryId, Is.Not.Null);
    }

    #endregion

    #region Response Tests

    [Test]
    public async Task Should_Return_Response_With_RenameEnabled_True_When_Renaming()
    {
        // Arrange
        var viewModel = CreateInstance();
        viewModel.RenameEnabled = true;
        viewModel.Name = "Test Name";
        viewModel.ChangeCategoryEnabled = false;

        ChangeCategoryTransactionsViewModel.Response? response = null;
        viewModel.CloseDialog = r => response = r as ChangeCategoryTransactionsViewModel.Response;

        // Act
        await viewModel.OkCommand.ExecuteAsync(null);

        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.RenameEnabled, Is.True);
        Assert.That(response.Name, Is.EqualTo("Test Name"));
        Assert.That(response.ChangeCategoryEnabled, Is.False);
        Assert.That(response.CategoryId, Is.Null);
    }

    [Test]
    public async Task Should_Return_Response_With_CategoryId_When_Category_Is_Enabled()
    {
        // Arrange
        var viewModel = CreateInstance();
        await Task.Delay(100); // Wait for categories to load

        viewModel.RenameEnabled = true;
        viewModel.Name = "Test Name";
        viewModel.ChangeCategoryEnabled = true;
        viewModel.SelectedCategory = viewModel.AvailableCategories.First();

        var expectedCategoryId = viewModel.SelectedCategory.Id;

        ChangeCategoryTransactionsViewModel.Response? response = null;
        viewModel.CloseDialog = r => response = r as ChangeCategoryTransactionsViewModel.Response;

        // Act
        await viewModel.OkCommand.ExecuteAsync(null);

        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.RenameEnabled, Is.True);
        Assert.That(response.Name, Is.EqualTo("Test Name"));
        Assert.That(response.ChangeCategoryEnabled, Is.True);
        Assert.That(response.CategoryId, Is.EqualTo(expectedCategoryId));
    }

    [Test]
    public async Task Should_Return_Response_With_Only_Category_When_Rename_Disabled()
    {
        // Arrange
        var viewModel = CreateInstance();
        await Task.Delay(100); // Wait for categories to load

        viewModel.RenameEnabled = false;
        viewModel.Name = string.Empty;
        viewModel.ChangeCategoryEnabled = true;
        viewModel.SelectedCategory = viewModel.AvailableCategories.First();

        var expectedCategoryId = viewModel.SelectedCategory.Id;

        ChangeCategoryTransactionsViewModel.Response? response = null;
        viewModel.CloseDialog = r => response = r as ChangeCategoryTransactionsViewModel.Response;

        // Act
        await viewModel.OkCommand.ExecuteAsync(null);

        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.RenameEnabled, Is.False);
        Assert.That(response.ChangeCategoryEnabled, Is.True);
        Assert.That(response.CategoryId, Is.EqualTo(expectedCategoryId));
    }

    [Test]
    public async Task Should_Return_Response_With_Both_Options_When_Both_Enabled()
    {
        // Arrange
        var viewModel = CreateInstance();
        await Task.Delay(100); // Wait for categories to load

        viewModel.RenameEnabled = true;
        viewModel.Name = "New Name";
        viewModel.ChangeCategoryEnabled = true;
        viewModel.SelectedCategory = viewModel.AvailableCategories.First();

        var expectedCategoryId = viewModel.SelectedCategory.Id;

        ChangeCategoryTransactionsViewModel.Response? response = null;
        viewModel.CloseDialog = r => response = r as ChangeCategoryTransactionsViewModel.Response;

        // Act
        await viewModel.OkCommand.ExecuteAsync(null);

        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.RenameEnabled, Is.True);
        Assert.That(response.Name, Is.EqualTo("New Name"));
        Assert.That(response.ChangeCategoryEnabled, Is.True);
        Assert.That(response.CategoryId, Is.EqualTo(expectedCategoryId));
    }

    #endregion

    #region Cancel Tests

    [Test]
    public void Should_Close_Window_On_Cancel()
    {
        // Arrange
        var viewModel = CreateInstance();
        var closeWindowCalled = false;
        viewModel.CloseWindow = () => closeWindowCalled = true;

        // Act
        viewModel.CancelCommand.Execute(null);

        // Assert
        Assert.That(closeWindowCalled, Is.True);
    }

    #endregion
}
