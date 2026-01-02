using Valt.Core.Kernel.Exceptions;

namespace Valt.Core.Modules.AvgPrice.Exceptions;

public class MixedCurrencyException : DomainException
{
    public MixedCurrencyException() : base("Cannot calculate totals for profiles with different currencies")
    {
    }
}
