using Valt.Core.Kernel.Exceptions;

namespace Valt.Core.Common.Exceptions;

public class MaximumFieldLengthException : DomainException
{
    public MaximumFieldLengthException(string fieldName, int maxLength) : base(
        $"Field {fieldName} length cannot be bigger than {maxLength} chars")
    {
    }
}