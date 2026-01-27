using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.Core.Modules.Budget.FixedExpenses;
using Valt.Core.Modules.Budget.FixedExpenses.Contracts;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Core.Modules.Budget.Transactions.Contracts;

namespace Valt.App.Modules.Budget.Transactions.Commands.BindTransactionToFixedExpense;

internal sealed class BindTransactionToFixedExpenseHandler : ICommandHandler<BindTransactionToFixedExpenseCommand, Unit>
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IFixedExpenseRepository _fixedExpenseRepository;

    public BindTransactionToFixedExpenseHandler(
        ITransactionRepository transactionRepository,
        IFixedExpenseRepository fixedExpenseRepository)
    {
        _transactionRepository = transactionRepository;
        _fixedExpenseRepository = fixedExpenseRepository;
    }

    public async Task<Result<Unit>> HandleAsync(BindTransactionToFixedExpenseCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.TransactionId))
        {
            return Result<Unit>.Failure("VALIDATION_FAILED", "Transaction ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.FixedExpenseId))
        {
            return Result<Unit>.Failure("VALIDATION_FAILED", "Fixed expense ID is required.");
        }

        var transactionId = new TransactionId(command.TransactionId);
        var transaction = await _transactionRepository.GetTransactionByIdAsync(transactionId);

        if (transaction is null)
        {
            return Result<Unit>.NotFound("Transaction", command.TransactionId);
        }

        var fixedExpenseId = new FixedExpenseId(command.FixedExpenseId);
        var fixedExpense = await _fixedExpenseRepository.GetFixedExpenseByIdAsync(fixedExpenseId);

        if (fixedExpense is null)
        {
            return Result<Unit>.NotFound("FixedExpense", command.FixedExpenseId);
        }

        var reference = new TransactionFixedExpenseReference(fixedExpenseId, command.ReferenceDate);
        transaction.SetFixedExpense(reference);

        await _transactionRepository.SaveTransactionAsync(transaction);

        return Result.Success();
    }
}
