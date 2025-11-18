using Valt.Core.Kernel.Exceptions;

namespace Valt.Core.Modules.Budget.FixedExpenses.Exceptions;

public class InvalidFixedExpenseRangeException : DomainException
{
    public DateOnly MinimumDate { get; }

    public InvalidFixedExpenseRangeException(DateOnly minimumDate) : base("Cannot add range with period start date before last fixed expense record date")
    {
        MinimumDate = minimumDate;
    }
}