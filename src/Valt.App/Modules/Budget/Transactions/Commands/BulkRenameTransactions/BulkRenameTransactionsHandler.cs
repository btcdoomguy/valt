using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Core.Modules.Budget.Transactions.Contracts;

namespace Valt.App.Modules.Budget.Transactions.Commands.BulkRenameTransactions;

internal sealed class BulkRenameTransactionsHandler : ICommandHandler<BulkRenameTransactionsCommand, BulkRenameTransactionsResult>
{
    private const int MaxNameLength = 60;

    private readonly ITransactionRepository _transactionRepository;

    public BulkRenameTransactionsHandler(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task<Result<BulkRenameTransactionsResult>> HandleAsync(BulkRenameTransactionsCommand command, CancellationToken ct = default)
    {
        if (command.TransactionIds is null || command.TransactionIds.Length == 0)
        {
            return Result<BulkRenameTransactionsResult>.Failure("VALIDATION_FAILED", "At least one transaction ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.NewName))
        {
            return Result<BulkRenameTransactionsResult>.Failure("VALIDATION_FAILED", "New name is required.");
        }

        if (command.NewName.Length > MaxNameLength)
        {
            return Result<BulkRenameTransactionsResult>.Failure("VALIDATION_FAILED",
                $"Transaction name cannot exceed {MaxNameLength} characters.");
        }

        var newName = TransactionName.New(command.NewName);
        var updatedCount = 0;

        // Batch-load all transactions in a single query to avoid N+1 round trips
        var transactionIds = command.TransactionIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => new TransactionId(id))
            .ToList();

        var transactions = await _transactionRepository.GetTransactionsByIdsAsync(transactionIds);

        foreach (var transaction in transactions)
        {
            transaction.Rename(newName);
            await _transactionRepository.SaveTransactionAsync(transaction);
            updatedCount++;
        }

        return Result<BulkRenameTransactionsResult>.Success(new BulkRenameTransactionsResult(updatedCount));
    }
}
