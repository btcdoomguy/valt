using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Accounts.Contracts;

namespace Valt.App.Modules.Budget.Accounts.Commands.MoveAccountGroup;

internal sealed class MoveAccountGroupHandler : ICommandHandler<MoveAccountGroupCommand, MoveAccountGroupResult>
{
    private readonly IAccountGroupRepository _accountGroupRepository;

    public MoveAccountGroupHandler(IAccountGroupRepository accountGroupRepository)
    {
        _accountGroupRepository = accountGroupRepository;
    }

    public async Task<Result<MoveAccountGroupResult>> HandleAsync(
        MoveAccountGroupCommand command,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.GroupId))
            return Result<MoveAccountGroupResult>.Failure(
                new Error("VALIDATION_FAILED", "Validation failed", new Dictionary<string, string[]>
                {
                    { nameof(command.GroupId), ["Group ID is required"] }
                }));

        var group = await _accountGroupRepository.GetByIdAsync(new AccountGroupId(command.GroupId));

        if (group is null)
            return Result<MoveAccountGroupResult>.Failure(
                "GROUP_NOT_FOUND", $"Account group with id {command.GroupId} not found");

        // Get all groups to renumber
        var allGroups = await _accountGroupRepository.GetAllAsync();
        var orderedGroups = allGroups.OrderBy(g => g.DisplayOrder).ToList();

        // Remove the group from its current position
        orderedGroups.Remove(group);

        // Insert at the new position
        var targetIndex = Math.Min(command.NewDisplayOrder, orderedGroups.Count);
        orderedGroups.Insert(targetIndex, group);

        // Update display orders for all affected groups
        for (var i = 0; i < orderedGroups.Count; i++)
        {
            var g = orderedGroups[i];
            if (g.DisplayOrder != i)
            {
                g.ChangeDisplayOrder(i);
                await _accountGroupRepository.SaveAsync(g);
            }
        }

        return Result<MoveAccountGroupResult>.Success(new MoveAccountGroupResult());
    }
}
