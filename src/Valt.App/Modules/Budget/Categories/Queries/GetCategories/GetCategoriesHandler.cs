using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.Categories.Contracts;
using Valt.App.Modules.Budget.Categories.DTOs;

namespace Valt.App.Modules.Budget.Categories.Queries.GetCategories;

internal sealed class GetCategoriesHandler : IQueryHandler<GetCategoriesQuery, CategoriesDTO>
{
    private readonly ICategoryQueries _categoryQueries;

    public GetCategoriesHandler(ICategoryQueries categoryQueries)
    {
        _categoryQueries = categoryQueries;
    }

    public Task<CategoriesDTO> HandleAsync(GetCategoriesQuery query, CancellationToken ct = default)
    {
        return _categoryQueries.GetCategoriesAsync();
    }
}
