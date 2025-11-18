using Valt.Infra.Modules.Budget.Categories.Queries.DTOs;

namespace Valt.Infra.Modules.Budget.Categories.Queries;

public interface ICategoryQueries
{
    Task<CategoriesDTO> GetCategoriesAsync();
}