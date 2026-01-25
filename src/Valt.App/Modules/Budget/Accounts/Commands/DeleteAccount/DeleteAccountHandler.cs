using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Accounts.Contracts;
using Valt.Core.Modules.Budget.Accounts.Exceptions;

namespace Valt.App.Modules.Budget.Accounts.Commands.DeleteAccount;

internal sealed class DeleteAccountHandler : ICommandHandler<DeleteAccountCommand, Unit>
{
    private readonly IAccountRepository _accountRepository;

    public DeleteAccountHandler(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<Result<Unit>> HandleAsync(DeleteAccountCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.AccountId))
        {
            return Result<Unit>.Failure("VALIDATION_FAILED", "Account ID is required.");
        }

        var accountId = new AccountId(command.AccountId);
        var account = await _accountRepository.GetAccountByIdAsync(accountId);

        if (account is null)
        {
            return Result<Unit>.NotFound("Account", command.AccountId);
        }

        try
        {
            await _accountRepository.DeleteAccountAsync(account);
        }
        catch (AccountHasTransactionsException)
        {
            return Result<Unit>.Failure(
                "ACCOUNT_HAS_TRANSACTIONS",
                "Cannot delete account because it has transactions.");
        }

        return Result.Success();
    }
}
