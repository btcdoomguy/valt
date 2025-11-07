using Valt.Core.Kernel.Exceptions;

namespace Valt.Core.Common.Exceptions;

public class InvalidBtcValueException : DomainException
{
    public InvalidBtcValueException() : base("Invalid bitcoin value")
    {
    }
}