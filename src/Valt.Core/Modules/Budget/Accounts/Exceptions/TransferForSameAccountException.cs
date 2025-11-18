using Valt.Core.Kernel.Exceptions;

namespace Valt.Core.Modules.Budget.Accounts.Exceptions;

public class TransferForSameAccountException : DomainException
{
    public TransferForSameAccountException(AccountId accountId) : base(
        $"Cannot transfer to the same account")
    {
    }
}