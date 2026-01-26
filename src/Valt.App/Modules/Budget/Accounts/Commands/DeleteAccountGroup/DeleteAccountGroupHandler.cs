using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Accounts.Contracts;

namespace Valt.App.Modules.Budget.Accounts.Commands.DeleteAccountGroup;

internal sealed class DeleteAccountGroupHandler : ICommandHandler<DeleteAccountGroupCommand, DeleteAccountGroupResult>
{
    private readonly IAccountGroupRepository _accountGroupRepository;

    public DeleteAccountGroupHandler(IAccountGroupRepository accountGroupRepository)
    {
        _accountGroupRepository = accountGroupRepository;
    }

    public async Task<Result<DeleteAccountGroupResult>> HandleAsync(
        DeleteAccountGroupCommand command,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.GroupId))
            return Result<DeleteAccountGroupResult>.Failure(
                new Error("VALIDATION_FAILED", "Validation failed", new Dictionary<string, string[]>
                {
                    { nameof(command.GroupId), ["Group ID is required"] }
                }));

        var group = await _accountGroupRepository.GetByIdAsync(new AccountGroupId(command.GroupId));

        if (group is null)
            return Result<DeleteAccountGroupResult>.Failure(
                "GROUP_NOT_FOUND", $"Account group with id {command.GroupId} not found");

        await _accountGroupRepository.DeleteAsync(group.Id);

        return Result<DeleteAccountGroupResult>.Success(new DeleteAccountGroupResult());
    }
}
