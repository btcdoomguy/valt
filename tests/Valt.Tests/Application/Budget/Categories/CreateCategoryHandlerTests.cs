using NSubstitute;
using Valt.App.Modules.Budget.Categories.Commands.CreateCategory;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Categories.Contracts;

namespace Valt.Tests.Application.Budget.Categories;

[TestFixture]
public class CreateCategoryHandlerTests : DatabaseTest
{
    private CreateCategoryHandler _handler = null!;

    [SetUp]
    public void SetUpHandler()
    {
        _handler = new CreateCategoryHandler(_categoryRepository, new CreateCategoryValidator());
    }

    [Test]
    public async Task HandleAsync_WithValidCommand_CreatesCategory()
    {
        var command = new CreateCategoryCommand
        {
            Name = "Food",
            IconId = Icon.Empty.ToString()
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.Not.Null);
            Assert.That(result.Value!.CategoryId, Is.Not.Empty);
        });

        var savedCategory = await _categoryRepository.GetCategoryByIdAsync(new CategoryId(result.Value!.CategoryId));
        Assert.That(savedCategory, Is.Not.Null);
        Assert.That(savedCategory!.Name.Value, Is.EqualTo("Food"));
    }

    [Test]
    public async Task HandleAsync_WithValidParent_CreatesChildCategory()
    {
        // Create parent first
        var parent = Category.New(CategoryName.New("Parent"), Icon.Empty);
        await _categoryRepository.SaveCategoryAsync(parent);

        var command = new CreateCategoryCommand
        {
            Name = "Child",
            IconId = Icon.Empty.ToString(),
            ParentId = parent.Id.Value
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        var savedCategory = await _categoryRepository.GetCategoryByIdAsync(new CategoryId(result.Value!.CategoryId));
        Assert.That(savedCategory!.ParentId?.Value, Is.EqualTo(parent.Id.Value));
    }

    [Test]
    public async Task HandleAsync_WithNonExistentParent_ReturnsNotFound()
    {
        var command = new CreateCategoryCommand
        {
            Name = "Orphan",
            IconId = Icon.Empty.ToString(),
            ParentId = "000000000000000000000001"
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("CATEGORY_NOT_FOUND"));
        });
    }

    [Test]
    public async Task HandleAsync_WithGrandparentParent_ReturnsInvalidParent()
    {
        // Create parent and child first
        var grandparent = Category.New(CategoryName.New("Grandparent"), Icon.Empty);
        await _categoryRepository.SaveCategoryAsync(grandparent);

        var parent = Category.New(CategoryName.New("Parent"), Icon.Empty, grandparent.Id);
        await _categoryRepository.SaveCategoryAsync(parent);

        // Try to create a grandchild (3rd level)
        var command = new CreateCategoryCommand
        {
            Name = "Grandchild",
            IconId = Icon.Empty.ToString(),
            ParentId = parent.Id.Value
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("INVALID_PARENT"));
        });
    }

    [Test]
    public async Task HandleAsync_WithEmptyName_ReturnsValidationError()
    {
        var command = new CreateCategoryCommand
        {
            Name = "",
            IconId = Icon.Empty.ToString()
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
            Assert.That(result.Error.HasValidationErrors, Is.True);
        });
    }

    [Test]
    public async Task HandleAsync_WithEmptyIcon_ReturnsValidationError()
    {
        var command = new CreateCategoryCommand
        {
            Name = "Valid Name",
            IconId = ""
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }
}
