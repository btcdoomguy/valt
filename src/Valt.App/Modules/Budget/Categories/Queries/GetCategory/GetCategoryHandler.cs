using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.Categories.Contracts;
using Valt.App.Modules.Budget.Categories.DTOs;

namespace Valt.App.Modules.Budget.Categories.Queries.GetCategory;

internal sealed class GetCategoryHandler : IQueryHandler<GetCategoryQuery, CategoryDTO?>
{
    private readonly ICategoryQueries _categoryQueries;

    public GetCategoryHandler(ICategoryQueries categoryQueries)
    {
        _categoryQueries = categoryQueries;
    }

    public Task<CategoryDTO?> HandleAsync(GetCategoryQuery query, CancellationToken ct = default)
    {
        return _categoryQueries.GetCategoryAsync(query.CategoryId);
    }
}
