using Valt.App.Kernel.Commands;

namespace Valt.App.Modules.Budget.Accounts.Commands.EditAccountGroup;

public record EditAccountGroupCommand : ICommand<EditAccountGroupResult>
{
    public required string GroupId { get; init; }
    public required string Name { get; init; }
}

public record EditAccountGroupResult;
