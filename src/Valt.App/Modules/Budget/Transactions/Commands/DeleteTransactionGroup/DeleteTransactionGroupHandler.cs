using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Core.Modules.Budget.Transactions.Contracts;

namespace Valt.App.Modules.Budget.Transactions.Commands.DeleteTransactionGroup;

internal sealed class DeleteTransactionGroupHandler : ICommandHandler<DeleteTransactionGroupCommand, DeleteTransactionGroupResult>
{
    private readonly ITransactionRepository _transactionRepository;

    public DeleteTransactionGroupHandler(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task<Result<DeleteTransactionGroupResult>> HandleAsync(
        DeleteTransactionGroupCommand command,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.GroupId))
            return Result<DeleteTransactionGroupResult>.Failure(
                new Error("VALIDATION_FAILED", "Validation failed", new Dictionary<string, string[]>
                {
                    { nameof(command.GroupId), ["Group ID is required"] }
                }));

        var groupId = new GroupId(command.GroupId);
        var transactions = await _transactionRepository.GetTransactionsByGroupIdAsync(groupId);
        var transactionList = transactions.ToList();

        if (transactionList.Count == 0)
            return Result<DeleteTransactionGroupResult>.Failure(
                "GROUP_NOT_FOUND", $"No transactions found for group {command.GroupId}");

        await _transactionRepository.DeleteTransactionsByGroupIdAsync(groupId);

        return Result<DeleteTransactionGroupResult>.Success(
            new DeleteTransactionGroupResult { DeletedCount = transactionList.Count });
    }
}
