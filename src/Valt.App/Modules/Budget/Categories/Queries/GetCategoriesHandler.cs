using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.Categories.DTOs;
using Valt.Core.Common;
using Valt.Infra.DataAccess;

namespace Valt.App.Modules.Budget.Categories.Queries;

internal sealed class GetCategoriesHandler : IQueryHandler<GetCategoriesQuery, CategoriesDTO>
{
    private readonly ILocalDatabase _localDatabase;

    public GetCategoriesHandler(ILocalDatabase localDatabase)
    {
        _localDatabase = localDatabase;
    }

    public Task<CategoriesDTO> HandleAsync(GetCategoriesQuery query, CancellationToken ct = default)
    {
        var data = _localDatabase.GetCategories().FindAll().ToList();

        var items = data.Select(category =>
        {
            var icon = category.Icon != null ? Icon.RestoreFromId(category.Icon) : Icon.Empty;

            var name = category.Name;
            if (category.ParentId is not null)
            {
                var parent = data.SingleOrDefault(x => x.Id == category.ParentId);
                if (parent is not null)
                    name = $"{parent.Name} > {name}";
            }

            return new CategoryDTO
            {
                Id = category.Id.ToString(),
                Name = name,
                SimpleName = category.Name,
                IconId = category.Icon,
                Unicode = icon.Unicode,
                Color = icon.Color
            };
        }).ToList();

        return Task.FromResult(new CategoriesDTO(items));
    }
}
