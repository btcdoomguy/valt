using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.Core.Modules.Budget.FixedExpenses;
using Valt.Core.Modules.Budget.FixedExpenses.Contracts;

namespace Valt.App.Modules.Budget.FixedExpenses.Commands.DeleteFixedExpense;

internal sealed class DeleteFixedExpenseHandler : ICommandHandler<DeleteFixedExpenseCommand, DeleteFixedExpenseResult>
{
    private readonly IFixedExpenseRepository _fixedExpenseRepository;

    public DeleteFixedExpenseHandler(IFixedExpenseRepository fixedExpenseRepository)
    {
        _fixedExpenseRepository = fixedExpenseRepository;
    }

    public async Task<Result<DeleteFixedExpenseResult>> HandleAsync(
        DeleteFixedExpenseCommand command,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.FixedExpenseId))
            return Result<DeleteFixedExpenseResult>.Failure(
                new Error("VALIDATION_FAILED", "Validation failed", new Dictionary<string, string[]>
                {
                    { nameof(command.FixedExpenseId), ["Fixed expense ID is required"] }
                }));

        var fixedExpenseId = new FixedExpenseId(command.FixedExpenseId);

        FixedExpense? fixedExpense;
        try
        {
            fixedExpense = await _fixedExpenseRepository.GetFixedExpenseByIdAsync(fixedExpenseId);
        }
        catch (Exception) when (true)
        {
            // Repository has a bug where it throws an exception when entity is not found
            // due to accessing properties on null entity in a LiteDB expression
            fixedExpense = null;
        }

        if (fixedExpense is null)
            return Result<DeleteFixedExpenseResult>.Failure(
                "FIXED_EXPENSE_NOT_FOUND", $"Fixed expense with id {command.FixedExpenseId} not found");

        await _fixedExpenseRepository.DeleteFixedExpenseAsync(fixedExpenseId);

        return Result<DeleteFixedExpenseResult>.Success(new DeleteFixedExpenseResult());
    }
}
