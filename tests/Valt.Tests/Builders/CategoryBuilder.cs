using LiteDB;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Categories;
using Valt.Infra.Modules.Budget.Categories;

namespace Valt.Tests.Builders;

public class CategoryBuilder
{
    private CategoryId _id = null!;
    private CategoryName _name = null!;
    private Icon _icon = Icon.Empty;

    public CategoryBuilder WithId(CategoryId id)
    {
        _id = id;
        return this;
    }

    public CategoryBuilder WithName(CategoryName name)
    {
        _name = name;
        return this;
    }

    public CategoryBuilder WithIcon(Icon icon)
    {
        _icon = icon;
        return this;
    }

    public CategoryEntity Build()
    {
        return new CategoryEntity()
        {
            Id = new ObjectId(_id),
            Icon = _icon.ToString(),
            Name = _name
        };
    }
}