using Valt.App.Kernel.Commands;
using Valt.App.Modules.Budget.Transactions.DTOs;

namespace Valt.App.Modules.Budget.Transactions.Commands.AddTransaction;

public record AddTransactionCommand : ICommand<AddTransactionResult>
{
    public required DateOnly Date { get; init; }
    public required string Name { get; init; }
    public required string CategoryId { get; init; }
    public required TransactionDetailsDto Details { get; init; }
    public string? Notes { get; init; }
    public string? FixedExpenseId { get; init; }
    public DateOnly? FixedExpenseReferenceDate { get; init; }
    public string? GroupId { get; init; }
}

public record AddTransactionResult(string TransactionId, DateOnly Date);
