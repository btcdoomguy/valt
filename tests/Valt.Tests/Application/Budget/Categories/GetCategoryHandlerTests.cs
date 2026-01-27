using Valt.App.Modules.Budget.Categories.Queries.GetCategory;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Categories;

namespace Valt.Tests.Application.Budget.Categories;

[TestFixture]
public class GetCategoryHandlerTests : DatabaseTest
{
    private GetCategoryHandler _handler = null!;

    [SetUp]
    public async Task SetUpHandler()
    {
        // Clean up any existing categories from previous tests
        var existingCategories = await _categoryRepository.GetCategoriesAsync();
        foreach (var category in existingCategories)
            await _categoryRepository.DeleteCategoryAsync(category.Id);

        _handler = new GetCategoryHandler(_categoryQueries);
    }

    [Test]
    public async Task HandleAsync_WithExistingCategory_ReturnsDto()
    {
        var category = Category.New(CategoryName.New("Food"), Icon.Empty);
        await _categoryRepository.SaveCategoryAsync(category);

        var query = new GetCategoryQuery { CategoryId = category.Id.Value };

        var result = await _handler.HandleAsync(query);

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.Id, Is.EqualTo(category.Id.Value));
            Assert.That(result.Name, Is.EqualTo("Food"));
            Assert.That(result.SimpleName, Is.EqualTo("Food"));
        });
    }

    [Test]
    public async Task HandleAsync_WithNonExistentCategory_ReturnsNull()
    {
        var query = new GetCategoryQuery { CategoryId = "000000000000000000000001" };

        var result = await _handler.HandleAsync(query);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task HandleAsync_WithChildCategory_ReturnsHierarchicalName()
    {
        var parent = Category.New(CategoryName.New("Expenses"), Icon.Empty);
        await _categoryRepository.SaveCategoryAsync(parent);

        var child = Category.New(CategoryName.New("Food"), Icon.Empty, parent.Id);
        await _categoryRepository.SaveCategoryAsync(child);

        var query = new GetCategoryQuery { CategoryId = child.Id.Value };

        var result = await _handler.HandleAsync(query);

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.Name, Is.EqualTo("Expenses > Food"));
            Assert.That(result.SimpleName, Is.EqualTo("Food"));
        });
    }

    [Test]
    public async Task HandleAsync_WithCategoryIcon_MapsIconProperties()
    {
        // Icon constructor: (string Source, string Name, char Unicode, Color Color)
        var icon = new Icon("phosphor", "shopping-cart", '$', System.Drawing.Color.Orange);
        var category = Category.New(CategoryName.New("Shopping"), icon);
        await _categoryRepository.SaveCategoryAsync(category);

        var query = new GetCategoryQuery { CategoryId = category.Id.Value };

        var result = await _handler.HandleAsync(query);

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.IconId, Is.Not.Null);
            Assert.That(result.Unicode, Is.EqualTo('$'));
        });
    }

    [Test]
    public async Task HandleAsync_WithParentCategory_ReturnsSameNameAndSimpleName()
    {
        var parent = Category.New(CategoryName.New("Investments"), Icon.Empty);
        await _categoryRepository.SaveCategoryAsync(parent);

        var query = new GetCategoryQuery { CategoryId = parent.Id.Value };

        var result = await _handler.HandleAsync(query);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo(result.SimpleName));
    }
}
