using Valt.App.Kernel;
using Valt.App.Kernel.Commands;

namespace Valt.App.Modules.Budget.Accounts.Commands.ChangeAccountVisibility;

public record ChangeAccountVisibilityCommand : ICommand<Unit>
{
    public required string AccountId { get; init; }
    public required bool Visible { get; init; }
}
