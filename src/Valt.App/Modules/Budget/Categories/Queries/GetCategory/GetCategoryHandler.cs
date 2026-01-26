using LiteDB;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.Categories.DTOs;
using Valt.Core.Common;
using Valt.Infra.DataAccess;

namespace Valt.App.Modules.Budget.Categories.Queries.GetCategory;

internal sealed class GetCategoryHandler : IQueryHandler<GetCategoryQuery, CategoryDTO?>
{
    private readonly ILocalDatabase _localDatabase;

    public GetCategoryHandler(ILocalDatabase localDatabase)
    {
        _localDatabase = localDatabase;
    }

    public Task<CategoryDTO?> HandleAsync(GetCategoryQuery query, CancellationToken ct = default)
    {
        var collection = _localDatabase.GetCategories();
        var category = collection.FindById(new ObjectId(query.CategoryId));

        if (category is null)
            return Task.FromResult<CategoryDTO?>(null);

        var icon = category.Icon != null ? Icon.RestoreFromId(category.Icon) : Icon.Empty;

        var name = category.Name;
        if (category.ParentId is not null)
        {
            var parent = collection.FindById(category.ParentId);
            if (parent is not null)
                name = $"{parent.Name} > {name}";
        }

        var dto = new CategoryDTO
        {
            Id = category.Id.ToString(),
            Name = name,
            SimpleName = category.Name,
            IconId = category.Icon,
            Unicode = icon.Unicode,
            Color = icon.Color
        };

        return Task.FromResult<CategoryDTO?>(dto);
    }
}
