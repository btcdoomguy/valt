using Valt.Core.Kernel.Exceptions;

namespace Valt.Core.Kernel.Ids.Exceptions;

public class EmptyIdException : DomainException
{
    public EmptyIdException() : base("Id cannot be null or empty")
    {
    }
}