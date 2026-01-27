using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Accounts.Contracts;

namespace Valt.App.Modules.Budget.Accounts.Commands.ChangeAccountVisibility;

internal sealed class ChangeAccountVisibilityHandler : ICommandHandler<ChangeAccountVisibilityCommand, Unit>
{
    private readonly IAccountRepository _accountRepository;

    public ChangeAccountVisibilityHandler(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<Result<Unit>> HandleAsync(
        ChangeAccountVisibilityCommand command,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.AccountId))
            return Result<Unit>.Failure(
                new Error("VALIDATION_FAILED", "Validation failed", new Dictionary<string, string[]>
                {
                    { nameof(command.AccountId), ["Account ID is required"] }
                }));

        var account = await _accountRepository.GetAccountByIdAsync(new AccountId(command.AccountId));

        if (account is null)
            return Result<Unit>.Failure(
                "ACCOUNT_NOT_FOUND", $"Account with id {command.AccountId} not found");

        account.ChangeVisibility(command.Visible);
        await _accountRepository.SaveAccountAsync(account);

        return Result<Unit>.Success(Unit.Value);
    }
}
