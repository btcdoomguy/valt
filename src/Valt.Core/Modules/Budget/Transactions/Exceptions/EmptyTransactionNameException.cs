using Valt.Core.Kernel.Exceptions;

namespace Valt.Core.Modules.Budget.Transactions.Exceptions;

public class EmptyTransactionNameException : DomainException
{
    public EmptyTransactionNameException() : base("Transaction must have a name")
    {
    }
}