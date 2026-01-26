using Valt.App.Modules.Budget.Categories.DTOs;

namespace Valt.App.Modules.Budget.Categories.Contracts;

public interface ICategoryQueries
{
    Task<CategoriesDTO> GetCategoriesAsync();
    Task<CategoryDTO?> GetCategoryAsync(string categoryId);
}
