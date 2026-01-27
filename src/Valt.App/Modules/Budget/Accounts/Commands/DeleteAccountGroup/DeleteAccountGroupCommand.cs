using Valt.App.Kernel.Commands;

namespace Valt.App.Modules.Budget.Accounts.Commands.DeleteAccountGroup;

public record DeleteAccountGroupCommand : ICommand<DeleteAccountGroupResult>
{
    public required string GroupId { get; init; }
}

public record DeleteAccountGroupResult;
