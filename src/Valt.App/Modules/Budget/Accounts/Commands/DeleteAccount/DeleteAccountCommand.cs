using Valt.App.Kernel;
using Valt.App.Kernel.Commands;

namespace Valt.App.Modules.Budget.Accounts.Commands.DeleteAccount;

public record DeleteAccountCommand : ICommand<Unit>
{
    public required string AccountId { get; init; }
}
