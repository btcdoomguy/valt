using Valt.Core.Common.Exceptions;
using Valt.Core.Modules.Budget.Categories.Exceptions;

namespace Valt.Core.Modules.Budget.Categories;

public record CategoryName
{
    public string Value { get; }

    private CategoryName(string value)
    {
        Value = value;
    }

    public static CategoryName New(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new EmptyCategoryNameException();

        if (value.Length > 50)
            throw new MaximumFieldLengthException(nameof(CategoryName), 50);

        return new CategoryName(value);
    }

    public static implicit operator string(CategoryName name) => name.Value;

    public static implicit operator CategoryName(string name) => CategoryName.New(name);
}