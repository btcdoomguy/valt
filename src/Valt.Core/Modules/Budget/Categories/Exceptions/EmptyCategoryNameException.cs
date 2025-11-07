using Valt.Core.Kernel.Exceptions;

namespace Valt.Core.Modules.Budget.Categories.Exceptions;

public class EmptyCategoryNameException : DomainException
{
    public EmptyCategoryNameException() : base("Category name cannot be empty")
    {
    }
}

public class CategoryNameLengthException : DomainException
{
    public CategoryNameLengthException() : base("Category name cannot be bigger than 50 chars")
    {
    }
}