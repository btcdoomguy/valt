using Valt.Core.Kernel.Exceptions;

namespace Valt.Core.Modules.Budget.Accounts.Exceptions;

public class AccountNameLengthException : DomainException
{
    public AccountNameLengthException() : base("Account name cannot be bigger than 30 chars")
    {
    }
}