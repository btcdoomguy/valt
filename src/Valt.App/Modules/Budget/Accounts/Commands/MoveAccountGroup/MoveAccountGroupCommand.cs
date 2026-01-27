using Valt.App.Kernel.Commands;

namespace Valt.App.Modules.Budget.Accounts.Commands.MoveAccountGroup;

/// <summary>
/// Command to reorder account groups.
/// </summary>
public record MoveAccountGroupCommand : ICommand<MoveAccountGroupResult>
{
    public required string GroupId { get; init; }

    /// <summary>
    /// The new display order for the group.
    /// </summary>
    public required int NewDisplayOrder { get; init; }
}

public record MoveAccountGroupResult;
