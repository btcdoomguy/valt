using Valt.Core.Kernel.Exceptions;

namespace Valt.Core.Modules.Budget.Accounts.Exceptions;

public class EmptyAccountNameException : DomainException
{
    public EmptyAccountNameException() : base("Account name cannot be empty")
    {
    }
}