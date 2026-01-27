using Valt.App.Modules.Budget.Categories.Queries.GetCategories;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Categories;

namespace Valt.Tests.Application.Budget.Categories;

[TestFixture]
public class GetCategoriesHandlerTests : DatabaseTest
{
    private GetCategoriesHandler _handler = null!;
    private Category _parentCategory = null!;
    private Category _childCategory = null!;

    protected override async Task SeedDatabase()
    {
        _parentCategory = Category.New(CategoryName.New("Parent"), Icon.Empty);
        await _categoryRepository.SaveCategoryAsync(_parentCategory);

        _childCategory = Category.New(CategoryName.New("Child"), Icon.Empty, _parentCategory.Id);
        await _categoryRepository.SaveCategoryAsync(_childCategory);
    }

    [SetUp]
    public void SetUpHandler()
    {
        _handler = new GetCategoriesHandler(_categoryQueries);
    }

    [Test]
    public async Task HandleAsync_ReturnsAllCategories()
    {
        var query = new GetCategoriesQuery();

        var result = await _handler.HandleAsync(query);

        Assert.That(result.Items, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task HandleAsync_ChildCategoryHasHierarchicalName()
    {
        var query = new GetCategoriesQuery();

        var result = await _handler.HandleAsync(query);

        var child = result.Items.First(c => c.Id == _childCategory.Id.Value);
        Assert.Multiple(() =>
        {
            Assert.That(child.Name, Is.EqualTo("Parent > Child"));
            Assert.That(child.SimpleName, Is.EqualTo("Child"));
        });
    }

    [Test]
    public async Task HandleAsync_ParentCategoryHasSimpleName()
    {
        var query = new GetCategoriesQuery();

        var result = await _handler.HandleAsync(query);

        var parent = result.Items.First(c => c.Id == _parentCategory.Id.Value);
        Assert.Multiple(() =>
        {
            Assert.That(parent.Name, Is.EqualTo("Parent"));
            Assert.That(parent.SimpleName, Is.EqualTo("Parent"));
        });
    }

    [Test]
    public async Task HandleAsync_WithEmptyDatabase_ReturnsEmptyList()
    {
        // Clear the categories
        var allCategories = await _categoryRepository.GetCategoriesAsync();
        foreach (var cat in allCategories)
        {
            await _categoryRepository.DeleteCategoryAsync(cat.Id);
        }

        var query = new GetCategoriesQuery();

        var result = await _handler.HandleAsync(query);

        Assert.That(result.Items, Is.Empty);
    }
}
