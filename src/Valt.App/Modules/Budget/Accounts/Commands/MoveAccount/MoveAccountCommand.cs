using Valt.App.Kernel.Commands;

namespace Valt.App.Modules.Budget.Accounts.Commands.MoveAccount;

/// <summary>
/// Command to reorder an account within its current group.
/// </summary>
public record MoveAccountCommand : ICommand<MoveAccountResult>
{
    public required string AccountId { get; init; }

    /// <summary>
    /// The new display order for the account.
    /// </summary>
    public required int NewDisplayOrder { get; init; }
}

public record MoveAccountResult;
