using Valt.Core.Kernel.Abstractions;

namespace Valt.Core.Modules.Budget.Categories.Contracts;

public interface ICategoryRepository : IRepository
{
    Task<Category?> GetCategoryByIdAsync(CategoryId categoryId);
    Task<IList<Category>> GetCategoriesAsync();

    Task SaveCategoryAsync(Category category);
    Task DeleteCategoryAsync(CategoryId categoryId);
}