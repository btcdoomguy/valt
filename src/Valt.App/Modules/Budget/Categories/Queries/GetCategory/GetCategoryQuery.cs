using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.Categories.DTOs;

namespace Valt.App.Modules.Budget.Categories.Queries.GetCategory;

/// <summary>
/// Query to get a single category by ID.
/// </summary>
public record GetCategoryQuery : IQuery<CategoryDTO?>
{
    public required string CategoryId { get; init; }
}
