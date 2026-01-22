using Valt.Core.Kernel.Exceptions;

namespace Valt.Core.Modules.Budget.Accounts.Exceptions;

public class EmptyAccountGroupNameException : DomainException
{
    public EmptyAccountGroupNameException() : base("Account group name cannot be empty")
    {
    }
}

public class AccountGroupNameLengthException : DomainException
{
    public AccountGroupNameLengthException() : base("Account group name cannot be longer than 50 characters")
    {
    }
}
