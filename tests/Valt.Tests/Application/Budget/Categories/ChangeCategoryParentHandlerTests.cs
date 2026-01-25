using Valt.App.Modules.Budget.Categories.Commands.ChangeCategoryParent;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Categories;

namespace Valt.Tests.Application.Budget.Categories;

[TestFixture]
public class ChangeCategoryParentHandlerTests : DatabaseTest
{
    private ChangeCategoryParentHandler _handler = null!;
    private Category _parentCategory = null!;
    private Category _childCategory = null!;
    private Category _orphanCategory = null!;

    protected override async Task SeedDatabase()
    {
        _parentCategory = Category.New(CategoryName.New("Parent"), Icon.Empty);
        await _categoryRepository.SaveCategoryAsync(_parentCategory);

        _childCategory = Category.New(CategoryName.New("Child"), Icon.Empty, _parentCategory.Id);
        await _categoryRepository.SaveCategoryAsync(_childCategory);

        _orphanCategory = Category.New(CategoryName.New("Orphan"), Icon.Empty);
        await _categoryRepository.SaveCategoryAsync(_orphanCategory);
    }

    [SetUp]
    public void SetUpHandler()
    {
        _handler = new ChangeCategoryParentHandler(_categoryRepository);
    }

    [Test]
    public async Task HandleAsync_SetNewParent_UpdatesCategoryParent()
    {
        var command = new ChangeCategoryParentCommand
        {
            CategoryId = _orphanCategory.Id.Value,
            NewParentId = _parentCategory.Id.Value
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        var updated = await _categoryRepository.GetCategoryByIdAsync(_orphanCategory.Id);
        Assert.That(updated!.ParentId?.Value, Is.EqualTo(_parentCategory.Id.Value));
    }

    [Test]
    public async Task HandleAsync_RemoveParent_MakesCategoryRoot()
    {
        var command = new ChangeCategoryParentCommand
        {
            CategoryId = _childCategory.Id.Value,
            NewParentId = null
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        var updated = await _categoryRepository.GetCategoryByIdAsync(_childCategory.Id);
        Assert.That(updated!.ParentId, Is.Null);
    }

    [Test]
    public async Task HandleAsync_WithNonExistentCategory_ReturnsNotFound()
    {
        var command = new ChangeCategoryParentCommand
        {
            CategoryId = "000000000000000000000001",
            NewParentId = _parentCategory.Id.Value
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("CATEGORY_NOT_FOUND"));
        });
    }

    [Test]
    public async Task HandleAsync_WithNonExistentParent_ReturnsNotFound()
    {
        var command = new ChangeCategoryParentCommand
        {
            CategoryId = _orphanCategory.Id.Value,
            NewParentId = "000000000000000000000001"
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("CATEGORY_NOT_FOUND"));
        });
    }

    [Test]
    public async Task HandleAsync_SetParentToSelf_ReturnsError()
    {
        var command = new ChangeCategoryParentCommand
        {
            CategoryId = _orphanCategory.Id.Value,
            NewParentId = _orphanCategory.Id.Value
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("INVALID_PARENT"));
        });
    }

    [Test]
    public async Task HandleAsync_SetParentToChildCategory_ReturnsError()
    {
        // Create a fresh child category with a parent to ensure test isolation
        var testParent = Category.New(CategoryName.New("TestParent"), Icon.Empty);
        await _categoryRepository.SaveCategoryAsync(testParent);

        var testChild = Category.New(CategoryName.New("TestChild"), Icon.Empty, testParent.Id);
        await _categoryRepository.SaveCategoryAsync(testChild);

        // Trying to set _orphanCategory's parent to testChild should fail
        // because testChild already has a parent (would create 3 levels)
        var command = new ChangeCategoryParentCommand
        {
            CategoryId = _orphanCategory.Id.Value,
            NewParentId = testChild.Id.Value // testChild already has a parent
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("INVALID_PARENT"));
        });
    }

    [Test]
    public async Task HandleAsync_MoveCategoryWithChildrenUnderParent_ReturnsError()
    {
        // Create a new parent to test moving a category with children
        var newParent = Category.New(CategoryName.New("NewParent"), Icon.Empty);
        await _categoryRepository.SaveCategoryAsync(newParent);

        // Try to move _parentCategory (which has _childCategory as a child) under newParent
        var command = new ChangeCategoryParentCommand
        {
            CategoryId = _parentCategory.Id.Value,
            NewParentId = newParent.Id.Value
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("INVALID_PARENT"));
        });
    }

    [Test]
    public async Task HandleAsync_WithEmptyCategoryId_ReturnsValidationError()
    {
        var command = new ChangeCategoryParentCommand
        {
            CategoryId = "",
            NewParentId = _parentCategory.Id.Value
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }
}
