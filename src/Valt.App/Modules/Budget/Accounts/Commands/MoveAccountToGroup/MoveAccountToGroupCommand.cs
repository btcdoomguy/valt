using Valt.App.Kernel.Commands;

namespace Valt.App.Modules.Budget.Accounts.Commands.MoveAccountToGroup;

/// <summary>
/// Command to move an account to a different group (or no group).
/// </summary>
public record MoveAccountToGroupCommand : ICommand<MoveAccountToGroupResult>
{
    public required string AccountId { get; init; }

    /// <summary>
    /// The target group ID. Null or empty to remove from any group.
    /// </summary>
    public string? TargetGroupId { get; init; }
}

public record MoveAccountToGroupResult;
