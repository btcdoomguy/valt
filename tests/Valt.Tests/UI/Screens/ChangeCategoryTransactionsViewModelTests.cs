using System.Drawing;
using NSubstitute;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.Categories.DTOs;
using Valt.App.Modules.Budget.Categories.Queries;
using Valt.Core.Kernel.Factories;
using Valt.Infra.Kernel;
using Valt.Infra.TransactionTerms;
using Valt.UI.Views.Main.Modals.ChangeCategoryTransactions;

namespace Valt.Tests.UI.Screens;

[TestFixture]
public class ChangeCategoryTransactionsViewModelTests
{
    private IQueryDispatcher _queryDispatcher = null!;
    private ITransactionTermService _transactionTermService = null!;
    private List<CategoryDTO> _testCategories = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        IdGenerator.Configure(new LiteDbIdProvider());
    }

    [SetUp]
    public void SetUp()
    {
        _testCategories = new List<CategoryDTO>
        {
            new() { Id = "cat1", Name = "Category 1", SimpleName = "Category 1", Unicode = '\0', Color = Color.Black },
            new() { Id = "cat2", Name = "Category 2", SimpleName = "Category 2", Unicode = '\0', Color = Color.Black }
        };

        _queryDispatcher = Substitute.For<IQueryDispatcher>();
        _queryDispatcher.DispatchAsync(Arg.Any<GetCategoriesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new CategoriesDTO(_testCategories));

        _transactionTermService = Substitute.For<ITransactionTermService>();
    }

    private ChangeCategoryTransactionsViewModel CreateInstance()
    {
        return new ChangeCategoryTransactionsViewModel(
            _transactionTermService,
            _queryDispatcher);
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
