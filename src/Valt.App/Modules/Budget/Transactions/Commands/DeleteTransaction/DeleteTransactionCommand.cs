using Valt.App.Kernel;
using Valt.App.Kernel.Commands;

namespace Valt.App.Modules.Budget.Transactions.Commands.DeleteTransaction;

public record DeleteTransactionCommand : ICommand<Unit>
{
    public required string TransactionId { get; init; }
}
