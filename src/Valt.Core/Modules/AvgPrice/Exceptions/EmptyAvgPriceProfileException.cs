using Valt.Core.Kernel.Exceptions;

namespace Valt.Core.Modules.AvgPrice.Exceptions;

public class EmptyAvgPriceProfileException : DomainException
{
    public EmptyAvgPriceProfileException() : base("Avg price profile name cannot be empty")
    {
    }
}