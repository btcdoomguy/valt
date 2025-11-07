using Valt.Core.Kernel.Exceptions;

namespace Valt.Core.Modules.Budget.FixedExpenses.Exceptions;

public class EmptyFixedExpenseNameException : DomainException
{
    public EmptyFixedExpenseNameException() : base("Fixed expense must have a name")
    {
    }
}