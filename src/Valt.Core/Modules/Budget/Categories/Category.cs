using Valt.Core.Common;
using Valt.Core.Kernel;

namespace Valt.Core.Modules.Budget.Categories;

public sealed class Category : Entity<CategoryId>
{
    public CategoryId? ParentId { get; private set; }
    public CategoryName Name { get; private set; }
    public Icon Icon { get; private set; }

    private Category(CategoryId id, CategoryName name, Icon icon, CategoryId? parentId)
    {
        Id = id;
        Name = name;
        Icon = icon;
        ParentId = parentId;

        if (Id == parentId)
            throw new ArgumentException(
                "Parent category cannot be the same as the category itself", nameof(parentId)
            );
    }

    public static Category New(CategoryName name, Icon icon, CategoryId? parentId = null) =>
        new(new CategoryId(), name, icon, parentId);

    public static Category Create(CategoryId id, CategoryName name, Icon icon, CategoryId? parentId = null) =>
        new(id, name, icon, parentId);

    public void Rename(CategoryName categoryName)
    {
        if (Name == categoryName)
            return;
        
        Name = categoryName;
    }

    public void ChangeIcon(Icon icon)
    {
        if (Icon == icon)
            return;
        
        Icon = icon;
    }

    public void ChangeParent(CategoryId? parentId)
    {
        if (Id == parentId)
        {
            ParentId = null;
            return;
        }

        ParentId = parentId;
    }
}