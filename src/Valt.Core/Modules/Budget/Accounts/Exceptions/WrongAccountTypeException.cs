using Valt.Core.Kernel.Exceptions;

namespace Valt.Core.Modules.Budget.Accounts.Exceptions;

public class WrongAccountTypeException : DomainException
{
    public WrongAccountTypeException(AccountId accountId) : base(
        $"The account {accountId} is not a valid account for this operation")
    {
    }
}