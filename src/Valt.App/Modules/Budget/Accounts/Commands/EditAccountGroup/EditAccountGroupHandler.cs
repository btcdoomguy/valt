using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Accounts.Contracts;

namespace Valt.App.Modules.Budget.Accounts.Commands.EditAccountGroup;

internal sealed class EditAccountGroupHandler : ICommandHandler<EditAccountGroupCommand, EditAccountGroupResult>
{
    private readonly IAccountGroupRepository _accountGroupRepository;

    public EditAccountGroupHandler(IAccountGroupRepository accountGroupRepository)
    {
        _accountGroupRepository = accountGroupRepository;
    }

    public async Task<Result<EditAccountGroupResult>> HandleAsync(
        EditAccountGroupCommand command,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.GroupId))
            return Result<EditAccountGroupResult>.Failure(
                new Error("VALIDATION_FAILED", "Validation failed", new Dictionary<string, string[]>
                {
                    { nameof(command.GroupId), ["Group ID is required"] }
                }));

        if (string.IsNullOrWhiteSpace(command.Name))
            return Result<EditAccountGroupResult>.Failure(
                new Error("VALIDATION_FAILED", "Validation failed", new Dictionary<string, string[]>
                {
                    { nameof(command.Name), ["Name is required"] }
                }));

        var group = await _accountGroupRepository.GetByIdAsync(new AccountGroupId(command.GroupId));

        if (group is null)
            return Result<EditAccountGroupResult>.Failure(
                "GROUP_NOT_FOUND", $"Account group with id {command.GroupId} not found");

        group.Rename(AccountGroupName.New(command.Name));
        await _accountGroupRepository.SaveAsync(group);

        return Result<EditAccountGroupResult>.Success(new EditAccountGroupResult());
    }
}
