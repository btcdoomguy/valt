using LiteDB;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Categories;
using Valt.Infra.Kernel.Exceptions;

namespace Valt.Infra.Modules.Budget.Categories;

public static class Extensions
{
    public static Category AsDomainObject(this CategoryEntity entity)
    {
        try
        {
            return Category.Create(entity.Id.ToString(), entity.Name, Icon.RestoreFromId(entity.Icon!), entity.ParentId is not null ? new CategoryId(entity.ParentId.ToString()!) : null);
        }
        catch (Exception ex)
        {
            throw new BrokenConversionFromDbException(nameof(CategoryEntity), entity.Id.ToString(), ex);
        }
    }

    public static CategoryEntity AsEntity(this Category category)
    {
        return new CategoryEntity()
        {
            Id = new ObjectId(category.Id),
            Icon = category.Icon.ToString(),
            Name = category.Name,
            ParentId = category.ParentId != null ? new ObjectId(category.ParentId) : null
        };
    }
}