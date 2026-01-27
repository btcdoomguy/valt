using Valt.App.Modules.Budget.Categories.Contracts;
using Valt.App.Modules.Budget.Categories.DTOs;
using Valt.Core.Common;
using Valt.Infra.DataAccess;

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
                IconId = category.Icon,
                Unicode = icon.Unicode,
                Color = icon.Color
            };
        }).ToList()));
    }

    public Task<CategoryDTO?> GetCategoryAsync(string categoryId)
    {
        var data = _localDatabase.GetCategories().FindAll().ToList();
        var category = data.FirstOrDefault(c => c.Id.ToString() == categoryId);

        if (category is null)
            return Task.FromResult<CategoryDTO?>(null);

        var icon = category.Icon != null ? Icon.RestoreFromId(category.Icon) : Icon.Empty;

        var name = category.Name;
        if (category.ParentId is not null)
        {
            var parent = data.SingleOrDefault(x => x.Id == category.ParentId);
            if (parent is not null)
                name = $"{parent.Name} > {name}";
        }

        return Task.FromResult<CategoryDTO?>(new CategoryDTO()
        {
            Id = category.Id.ToString(),
            Name = name,
            SimpleName = category.Name,
            IconId = category.Icon,
            Unicode = icon.Unicode,
            Color = icon.Color
        });
    }
}