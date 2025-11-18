using Valt.Core.Kernel.Exceptions;

namespace Valt.Core.Common.Exceptions;

public class InvalidFiatValueException : DomainException
{
    public InvalidFiatValueException() : base("Invalid fiat currency value")
    {
    }
}