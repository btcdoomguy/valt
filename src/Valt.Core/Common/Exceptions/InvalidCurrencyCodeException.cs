using Valt.Core.Kernel.Exceptions;

namespace Valt.Core.Common.Exceptions;

public class InvalidCurrencyCodeException : DomainException
{
    public InvalidCurrencyCodeException(string code) : base($"Cannot parse the currency code {code}")
    {
    }
}