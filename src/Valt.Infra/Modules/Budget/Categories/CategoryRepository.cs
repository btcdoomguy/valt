using LiteDB;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Categories.Contracts;
using Valt.Infra.DataAccess;

namespace Valt.Infra.Modules.Budget.Categories;

internal class CategoryRepository : ICategoryRepository
{
    private readonly ILocalDatabase _localDatabase;

    public CategoryRepository(ILocalDatabase localDatabase)
    {
        _localDatabase = localDatabase;
    }

    public Task<Category?> GetCategoryByIdAsync(CategoryId categoryId)
    {
        var entity = _localDatabase.GetCategories().FindById(new ObjectId(categoryId));

        return Task.FromResult(entity?.AsDomainObject());
    }

    public Task<IList<Category>> GetCategoriesAsync()
    {
        var entities = _localDatabase.GetCategories().FindAll()!;

        var categories = entities.Select(entity => entity.AsDomainObject()).ToList();

        return Task.FromResult<IList<Category>>(categories);
    }

    public Task SaveCategoryAsync(Category category)
    {
        var entity = category.AsEntity();

        _localDatabase.GetCategories().Upsert(entity);

        return Task.CompletedTask;
    }

    public Task DeleteCategoryAsync(CategoryId categoryId)
    {
        _localDatabase.GetCategories().Delete(new ObjectId(categoryId.Value));

        return Task.CompletedTask;
    }
}