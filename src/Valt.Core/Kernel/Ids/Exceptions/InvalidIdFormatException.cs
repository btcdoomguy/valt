using Valt.Core.Kernel.Exceptions;

namespace Valt.Core.Kernel.Ids.Exceptions;

public class InvalidIdFormatException : DomainException
{
    public InvalidIdFormatException(string id) : base($"Id {id} must have a valid format")
    {
    }
}