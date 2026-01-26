using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Accounts.Contracts;

namespace Valt.App.Modules.Budget.Accounts.Commands.MoveAccount;

internal sealed class MoveAccountHandler : ICommandHandler<MoveAccountCommand, MoveAccountResult>
{
    private readonly IAccountRepository _accountRepository;

    public MoveAccountHandler(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<Result<MoveAccountResult>> HandleAsync(
        MoveAccountCommand command,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.AccountId))
            return Result<MoveAccountResult>.Failure(
                new Error("VALIDATION_FAILED", "Validation failed", new Dictionary<string, string[]>
                {
                    { nameof(command.AccountId), ["Account ID is required"] }
                }));

        var account = await _accountRepository.GetAccountByIdAsync(new AccountId(command.AccountId));

        if (account is null)
            return Result<MoveAccountResult>.Failure(
                "ACCOUNT_NOT_FOUND", $"Account with id {command.AccountId} not found");

        // Get all accounts in the same group to renumber
        var allAccounts = await _accountRepository.GetAccountsAsync();
        var accountsInGroup = allAccounts
            .Where(a => a.GroupId == account.GroupId)
            .OrderBy(a => a.DisplayOrder)
            .ToList();

        // Remove the account from its current position
        accountsInGroup.Remove(account);

        // Insert at the new position
        var targetIndex = Math.Min(command.NewDisplayOrder, accountsInGroup.Count);
        accountsInGroup.Insert(targetIndex, account);

        // Update display orders for all affected accounts
        for (var i = 0; i < accountsInGroup.Count; i++)
        {
            var a = accountsInGroup[i];
            if (a.DisplayOrder != i)
            {
                a.ChangeDisplayOrder(i);
                await _accountRepository.SaveAccountAsync(a);
            }
        }

        return Result<MoveAccountResult>.Success(new MoveAccountResult());
    }
}
