using Valt.App.Kernel.Commands;

namespace Valt.App.Modules.Budget.Accounts.Commands.CreateAccountGroup;

public record CreateAccountGroupCommand : ICommand<CreateAccountGroupResult>
{
    public required string Name { get; init; }
}

public record CreateAccountGroupResult(string GroupId);
