using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Validation;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Core.Modules.Budget.Transactions.Contracts;

namespace Valt.App.Modules.Budget.Transactions.Commands.SetTransactionExclusion;

internal sealed class SetTransactionExclusionHandler : ICommandHandler<SetTransactionExclusionCommand, Unit>
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IValidator<SetTransactionExclusionCommand> _validator;

    public SetTransactionExclusionHandler(
        ITransactionRepository transactionRepository,
        IValidator<SetTransactionExclusionCommand> validator)
    {
        _transactionRepository = transactionRepository;
        _validator = validator;
    }

    public async Task<Result<Unit>> HandleAsync(SetTransactionExclusionCommand command, CancellationToken ct = default)
    {
        var validation = _validator.Validate(command);
        if (!validation.IsValid)
        {
            return Result<Unit>.ValidationFailure(
                new Dictionary<string, string[]>(validation.Errors));
        }

        var transactionId = new TransactionId(command.TransactionId);
        var transaction = await _transactionRepository.GetTransactionByIdAsync(transactionId);
        if (transaction is null)
        {
            return Result<Unit>.NotFound("Transaction", command.TransactionId);
        }

        transaction.SetExcluded(command.IsExcluded);

        await _transactionRepository.SaveTransactionAsync(transaction);

        return Result.Success();
    }
}
