using Valt.Core.Common;
using Valt.Infra.DataAccess;
using Valt.Infra.Modules.Budget.Categories.Queries.DTOs;

namespace Valt.Infra.Modules.Budget.Categories.Queries;

public class CategoryQueries : ICategoryQueries
{
    private readonly ILocalDatabase _localDatabase;

    public CategoryQueries(ILocalDatabase localDatabase)
    {
        _localDatabase = localDatabase;
    }

    public Task<CategoriesDTO> GetCategoriesAsync()
    {
        var data = _localDatabase.GetCategories().FindAll().ToList();

        return Task.FromResult(new CategoriesDTO(data.Select(category =>
        {
            var icon = category.Icon != null ? Icon.RestoreFromId(category.Icon) : Icon.Empty;

            var name = category.Name;
            if (category.ParentId is not null)
            {
                var parent = data.SingleOrDefault(x => x.Id == category.ParentId);
                if (parent is not null)
                    name = $"{parent.Name} > {name}";
            }
                
            return new CategoryDTO()
            {
                Id = category.Id.ToString(),
                Name = name,
                SimpleName = category.Name,
                Icon = category.Icon,
                Unicode = icon.Unicode,
                Color = icon.Color
            };
        }).ToList()));
    }
}