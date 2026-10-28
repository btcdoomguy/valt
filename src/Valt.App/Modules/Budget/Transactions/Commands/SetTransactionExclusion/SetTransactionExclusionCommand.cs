using Valt.App.Kernel;
using Valt.App.Kernel.Commands;

namespace Valt.App.Modules.Budget.Transactions.Commands.SetTransactionExclusion;

public record SetTransactionExclusionCommand : ICommand<Unit>
{
    public required string TransactionId { get; init; }
    public bool IsExcluded { get; init; }
}
