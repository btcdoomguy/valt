using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.Core.Modules.Budget.FixedExpenses.Contracts;
using Valt.Core.Modules.Budget.Transactions;

namespace Valt.App.Modules.Budget.FixedExpenses.Commands.UndoIgnoreFixedExpense;

internal sealed class UndoIgnoreFixedExpenseHandler(IFixedExpenseRecordService fixedExpenseRecordService)
    : ICommandHandler<UndoIgnoreFixedExpenseCommand, Unit>
{
    public async Task<Result<Unit>> HandleAsync(UndoIgnoreFixedExpenseCommand command, CancellationToken ct = default)
    {
        await fixedExpenseRecordService.UndoIgnoreFixedExpenseAsync(
            new TransactionFixedExpenseReference(command.FixedExpenseId, command.ReferenceDate));
        return Result.Success();
    }
}
