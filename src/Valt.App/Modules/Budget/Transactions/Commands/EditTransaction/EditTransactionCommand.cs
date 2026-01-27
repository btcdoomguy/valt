using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.App.Modules.Budget.Transactions.DTOs;

namespace Valt.App.Modules.Budget.Transactions.Commands.EditTransaction;

public record EditTransactionCommand : ICommand<Unit>
{
    public required string TransactionId { get; init; }
    public required DateOnly Date { get; init; }
    public required string Name { get; init; }
    public required string CategoryId { get; init; }
    public required TransactionDetailsDto Details { get; init; }
    public string? Notes { get; init; }
    public string? FixedExpenseId { get; init; }
    public DateOnly? FixedExpenseReferenceDate { get; init; }
}
