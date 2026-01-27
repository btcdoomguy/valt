using Valt.App.Modules.Budget.Categories.Commands.EditCategory;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Categories;

namespace Valt.Tests.Application.Budget.Categories;

[TestFixture]
public class EditCategoryHandlerTests : DatabaseTest
{
    private EditCategoryHandler _handler = null!;
    private Category _existingCategory = null!;

    protected override async Task SeedDatabase()
    {
        _existingCategory = Category.New(CategoryName.New("Original"), Icon.Empty);
        await _categoryRepository.SaveCategoryAsync(_existingCategory);
    }

    [SetUp]
    public void SetUpHandler()
    {
        _handler = new EditCategoryHandler(_categoryRepository, new EditCategoryValidator());
    }

    [Test]
    public async Task HandleAsync_WithValidCommand_UpdatesCategory()
    {
        var newIcon = new Icon("test", "test-icon", 'X', System.Drawing.Color.Red);
        var command = new EditCategoryCommand
        {
            CategoryId = _existingCategory.Id.Value,
            Name = "Updated Name",
            IconId = newIcon.ToString()
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        var updatedCategory = await _categoryRepository.GetCategoryByIdAsync(_existingCategory.Id);
        Assert.Multiple(() =>
        {
            Assert.That(updatedCategory!.Name.Value, Is.EqualTo("Updated Name"));
            Assert.That(updatedCategory.Icon.Name, Is.EqualTo("test-icon"));
        });
    }

    [Test]
    public async Task HandleAsync_WithNonExistentCategory_ReturnsNotFound()
    {
        var command = new EditCategoryCommand
        {
            CategoryId = "000000000000000000000001",
            Name = "Updated",
            IconId = Icon.Empty.ToString()
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("CATEGORY_NOT_FOUND"));
        });
    }

    [Test]
    public async Task HandleAsync_WithEmptyName_ReturnsValidationError()
    {
        var command = new EditCategoryCommand
        {
            CategoryId = _existingCategory.Id.Value,
            Name = "",
            IconId = Icon.Empty.ToString()
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }

    [Test]
    public async Task HandleAsync_WithEmptyCategoryId_ReturnsValidationError()
    {
        var command = new EditCategoryCommand
        {
            CategoryId = "",
            Name = "Valid Name",
            IconId = Icon.Empty.ToString()
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }
}
