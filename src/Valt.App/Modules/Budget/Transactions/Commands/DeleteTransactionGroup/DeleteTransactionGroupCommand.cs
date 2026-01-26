using Valt.App.Kernel.Commands;

namespace Valt.App.Modules.Budget.Transactions.Commands.DeleteTransactionGroup;

/// <summary>
/// Command to delete all transactions in a group (e.g., installment transactions).
/// </summary>
public record DeleteTransactionGroupCommand : ICommand<DeleteTransactionGroupResult>
{
    public required string GroupId { get; init; }
}

public record DeleteTransactionGroupResult
{
    public required int DeletedCount { get; init; }
}
