using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Accounts.Contracts;

namespace Valt.App.Modules.Budget.Accounts.Commands.MoveAccountToGroup;

internal sealed class MoveAccountToGroupHandler : ICommandHandler<MoveAccountToGroupCommand, MoveAccountToGroupResult>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IAccountGroupRepository _accountGroupRepository;

    public MoveAccountToGroupHandler(
        IAccountRepository accountRepository,
        IAccountGroupRepository accountGroupRepository)
    {
        _accountRepository = accountRepository;
        _accountGroupRepository = accountGroupRepository;
    }

    public async Task<Result<MoveAccountToGroupResult>> HandleAsync(
        MoveAccountToGroupCommand command,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.AccountId))
            return Result<MoveAccountToGroupResult>.Failure(
                new Error("VALIDATION_FAILED", "Validation failed", new Dictionary<string, string[]>
                {
                    { nameof(command.AccountId), ["Account ID is required"] }
                }));

        var account = await _accountRepository.GetAccountByIdAsync(new AccountId(command.AccountId));

        if (account is null)
            return Result<MoveAccountToGroupResult>.Failure(
                "ACCOUNT_NOT_FOUND", $"Account with id {command.AccountId} not found");

        AccountGroupId? targetGroupId = null;

        // Validate target group exists if specified
        if (!string.IsNullOrWhiteSpace(command.TargetGroupId))
        {
            var targetGroup = await _accountGroupRepository.GetByIdAsync(new AccountGroupId(command.TargetGroupId));

            if (targetGroup is null)
                return Result<MoveAccountToGroupResult>.Failure(
                    "GROUP_NOT_FOUND", $"Account group with id {command.TargetGroupId} not found");

            targetGroupId = targetGroup.Id;
        }

        account.AssignToGroup(targetGroupId);

        // Set display order to end of the new group
        var allAccounts = await _accountRepository.GetAccountsAsync();
        var accountsInTargetGroup = allAccounts
            .Where(a => a.GroupId == targetGroupId && a.Id != account.Id)
            .ToList();
        var maxDisplayOrder = accountsInTargetGroup.Count > 0
            ? accountsInTargetGroup.Max(a => a.DisplayOrder) + 1
            : 0;
        account.ChangeDisplayOrder(maxDisplayOrder);

        await _accountRepository.SaveAccountAsync(account);

        return Result<MoveAccountToGroupResult>.Success(new MoveAccountToGroupResult());
    }
}
