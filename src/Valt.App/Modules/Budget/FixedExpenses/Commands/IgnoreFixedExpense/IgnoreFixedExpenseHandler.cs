using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.Core.Modules.Budget.FixedExpenses.Contracts;
using Valt.Core.Modules.Budget.Transactions;

namespace Valt.App.Modules.Budget.FixedExpenses.Commands.IgnoreFixedExpense;

internal sealed class IgnoreFixedExpenseHandler(IFixedExpenseRecordService fixedExpenseRecordService)
    : ICommandHandler<IgnoreFixedExpenseCommand, Unit>
{
    public async Task<Result<Unit>> HandleAsync(IgnoreFixedExpenseCommand command, CancellationToken ct = default)
    {
        await fixedExpenseRecordService.IgnoreFixedExpenseAsync(
            new TransactionFixedExpenseReference(command.FixedExpenseId, command.ReferenceDate));
        return Result.Success();
    }
}
