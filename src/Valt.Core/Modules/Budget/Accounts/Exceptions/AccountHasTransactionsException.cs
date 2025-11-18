using Valt.Core.Kernel.Exceptions;

namespace Valt.Core.Modules.Budget.Accounts.Exceptions;

public class AccountHasTransactionsException : DomainException
{
    public AccountHasTransactionsException() : base("Cannot delete account with transactions")
    {
    }
}