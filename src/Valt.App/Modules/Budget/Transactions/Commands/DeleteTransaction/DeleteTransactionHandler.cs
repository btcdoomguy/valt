using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Core.Modules.Budget.Transactions.Contracts;

namespace Valt.App.Modules.Budget.Transactions.Commands.DeleteTransaction;

internal sealed class DeleteTransactionHandler : ICommandHandler<DeleteTransactionCommand, Unit>
{
    private readonly ITransactionRepository _transactionRepository;

    public DeleteTransactionHandler(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task<Result<Unit>> HandleAsync(DeleteTransactionCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.TransactionId))
        {
            return Result<Unit>.Failure("VALIDATION_FAILED", "Transaction ID is required.");
        }

        var transactionId = new TransactionId(command.TransactionId);
        var transaction = await _transactionRepository.GetTransactionByIdAsync(transactionId);

        if (transaction is null)
        {
            return Result<Unit>.NotFound("Transaction", command.TransactionId);
        }

        await _transactionRepository.DeleteTransactionAsync(transactionId);

        return Result.Success();
    }
}
