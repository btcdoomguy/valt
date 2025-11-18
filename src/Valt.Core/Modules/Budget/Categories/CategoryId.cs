using Valt.Core.Kernel.Factories;
using Valt.Core.Kernel.Ids;

namespace Valt.Core.Modules.Budget.Categories;

public class CategoryId : CommonId
{
    public CategoryId() : base(IdGenerator.Generate())
    {
    }

    public CategoryId(string value) : base(value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));
    }

    public static implicit operator string(CategoryId id) => id.Value;

    public static implicit operator CategoryId(string id) => new(id);
}